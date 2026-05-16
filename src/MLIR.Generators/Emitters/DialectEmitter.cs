namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;

internal sealed class DialectEmitter
{
    private readonly DialectSymbolResolver resolver;
    private readonly StringBuilder builder = new();
    private readonly List<Diagnostic> diagnostics = [];

    public DialectEmitter(DialectSymbolResolver resolver)
    {
        this.resolver = resolver;
    }

    public GeneratedDialectSourceResult Generate(DialectModel dialect)
    {
        DialectFileEmitter.EmitHeader(builder, dialect);

        foreach (var enumModel in dialect.Attributes
            .Select(static attribute => attribute.EnumModel)
            .Concat(dialect.AttributeConstraints.Select(static constraint => constraint.EnumModel))
            .Where(static enumModel => enumModel != null)
            .Cast<EnumModel>()
            .GroupBy(static enumModel => enumModel.ClassName, System.StringComparer.Ordinal)
            .Select(static group => group.First()))
        {
            EnumEmitter.EmitSharedDefinitions(builder, enumModel);
            builder.AppendLine();
        }

        foreach (var operation in dialect.Operations)
        {
            if (!TryEmit(dialect, "operation", operation.ClassName ?? operation.Name, () =>
            {
                OperationEmitter.Emit(builder, operation, resolver);
                builder.AppendLine();

                if (operation.AssemblyFormat != null)
                {
                    UnifiedAssemblyFormatEmitter.EmitOperation(
                        builder,
                        operation,
                        DialectGeneratorNaming.GetOperationClassName(operation),
                        resolver,
                        diagnostics);
                    builder.AppendLine();
                }
            }))
            {
                return new GeneratedDialectSourceResult(builder.ToString(), diagnostics);
            }
        }

        foreach (var attribute in dialect.Attributes)
        {
            if (!TryEmit(dialect, "attribute", attribute.RecordName, () =>
            {
                AttributeEmitter.Emit(builder, attribute, diagnostics);
                builder.AppendLine();
            }))
            {
                return new GeneratedDialectSourceResult(builder.ToString(), diagnostics);
            }
        }

        foreach (var attributeConstraint in dialect.AttributeConstraints)
        {
            if (!TryEmit(dialect, "attribute constraint", attributeConstraint.RecordName, () =>
            {
                AttributeConstraintEmitter.Emit(builder, attributeConstraint, resolver);
                builder.AppendLine();
            }))
            {
                return new GeneratedDialectSourceResult(builder.ToString(), diagnostics);
            }
        }

        foreach (var typeConstraint in dialect.TypeConstraints)
        {
            if (!TryEmit(dialect, "type constraint", typeConstraint.RecordName, () =>
            {
                TypeConstraintEmitter.Emit(builder, typeConstraint);
                builder.AppendLine();
            }))
            {
                return new GeneratedDialectSourceResult(builder.ToString(), diagnostics);
            }
        }

        // Emit C# marker interfaces for each TypeInterface defined in this dialect.
        foreach (var interfaceModel in dialect.Interfaces)
        {
            if (interfaceModel.Kind != InterfaceKind.Type)
            {
                continue;
            }

            if (!TryEmit(dialect, "type interface", interfaceModel.RecordName, () =>
            {
                TypeInterfaceEmitter.Emit(builder, interfaceModel);
                builder.AppendLine();
            }))
            {
                return new GeneratedDialectSourceResult(builder.ToString(), diagnostics);
            }
        }

        foreach (var type in dialect.Types)
        {
            if (!TryEmit(dialect, "type", type.RecordName, () =>
            {
                var typeInterfaces = ResolveTypeInterfaces(type);
                TypeEmitter.Emit(builder, type, typeInterfaces, diagnostics);
                builder.AppendLine();
            }))
            {
                return new GeneratedDialectSourceResult(builder.ToString(), diagnostics);
            }
        }

        DialectRegistrationEmitter.Emit(builder, dialect);

        return new GeneratedDialectSourceResult(builder.ToString(), diagnostics);
    }

    /// <summary>
    /// Computes the ordered list of fully qualified C# marker interface names for the given
    /// type based on its ODS trait list. Only interface-backed traits of kind
    /// <see cref="InterfaceKind.Type"/> that have a resolvable C# interface name are included.
    /// The order is deterministic (same as the trait list order).
    /// </summary>
    private IReadOnlyList<ResolvedTypeInterfaceModel> ResolveTypeInterfaces(TypeModel type)
    {
        List<ResolvedTypeInterfaceModel>? resolved = null;

        foreach (var trait in type.Traits)
        {
            if (trait is not InterfaceTraitModel interfaceTrait
                || interfaceTrait.Kind != InterfaceKind.Type
                || interfaceTrait.CppInterfaceName == null)
            {
                continue;
            }

            var name = resolver.TryResolveTypeInterfaceName(interfaceTrait.CppNamespace, interfaceTrait.CppInterfaceName);
            var model = resolver.TryResolveTypeInterfaceModel(interfaceTrait.CppNamespace, interfaceTrait.CppInterfaceName);
            if (name == null || model == null)
            {
                continue;
            }

            resolved ??= new List<ResolvedTypeInterfaceModel>();
            if (!resolved.Any(existing => existing.QualifiedName == name))
            {
                resolved.Add(new ResolvedTypeInterfaceModel(name, model));
            }
        }

        return resolved ?? (IReadOnlyList<ResolvedTypeInterfaceModel>)System.Array.Empty<ResolvedTypeInterfaceModel>();
    }

    private bool TryEmit(DialectModel dialect, string entityKind, string entityName, System.Action emit)
    {
        try
        {
            emit();
            return true;
        }
        catch (System.Exception exception)
        {
            diagnostics.Add(Diagnostic.Create(
                DialectGeneratorDiagnostics.DialectDefinitionEmissionFailed,
                Location.None,
                entityKind,
                entityName,
                dialect.Name,
                FormatExceptionMessage(exception)));
            return false;
        }
    }

    private static string FormatExceptionMessage(System.Exception exception)
    {
        var parts = new List<string>();
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                parts.Add(current.Message);
            }
        }

        return parts.Count == 0
            ? exception.GetType().FullName ?? exception.GetType().Name
            : string.Join(" --> ", parts);
    }
}
