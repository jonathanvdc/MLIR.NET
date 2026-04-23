namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;

internal sealed class DialectEmitter
{
    private readonly DialectSymbolResolver resolver;
    private readonly StringBuilder builder = new();

    public DialectEmitter(DialectSymbolResolver resolver)
    {
        this.resolver = resolver;
    }

    public string Generate(DialectModel dialect)
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
            try
            {
                OperationEmitter.Emit(builder, operation, resolver);
                builder.AppendLine();

                if (operation.AssemblyFormat != null)
                {
                    var metadata = OperationBodySyntaxEmitter.Emit(builder, operation);
                    builder.AppendLine();

                    OperationAssemblyFormatEmitter.Emit(builder, operation, metadata, resolver);
                    builder.AppendLine();
                }
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate operation '" + (operation.ClassName ?? operation.Name) + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        foreach (var attribute in dialect.Attributes)
        {
            try
            {
                AttributeEmitter.Emit(builder, attribute);
                builder.AppendLine();
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate attribute '" + attribute.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

            foreach (var attributeConstraint in dialect.AttributeConstraints)
            {
                try
                {
                    AttributeConstraintEmitter.Emit(builder, attributeConstraint, resolver);
                    builder.AppendLine();
                }
                catch (System.Exception exception)
                {
                throw new System.InvalidOperationException(
                    "Failed to generate attribute constraint '" + attributeConstraint.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        foreach (var typeConstraint in dialect.TypeConstraints)
        {
            try
            {
                TypeConstraintEmitter.Emit(builder, typeConstraint);
                builder.AppendLine();
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate type constraint '" + typeConstraint.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        // Emit C# marker interfaces for each TypeInterface defined in this dialect.
        foreach (var interfaceModel in dialect.Interfaces)
        {
            if (interfaceModel.Kind != InterfaceKind.Type)
            {
                continue;
            }

            try
            {
                TypeInterfaceEmitter.Emit(builder, interfaceModel);
                builder.AppendLine();
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate type interface '" + interfaceModel.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        foreach (var type in dialect.Types)
        {
            try
            {
                var markerInterfaces = ResolveMarkerInterfaces(type);
                TypeEmitter.Emit(builder, type, markerInterfaces);
                builder.AppendLine();
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate type '" + type.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        DialectRegistrationEmitter.Emit(builder, dialect);

        return builder.ToString();
    }

    /// <summary>
    /// Computes the ordered list of fully qualified C# marker interface names for the given
    /// type based on its ODS trait list. Only interface-backed traits of kind
    /// <see cref="InterfaceKind.Type"/> that have a resolvable C# interface name are included.
    /// The order is deterministic (same as the trait list order).
    /// </summary>
    private IReadOnlyList<string> ResolveMarkerInterfaces(TypeModel type)
    {
        List<string>? names = null;

        foreach (var trait in type.Traits)
        {
            if (trait is not InterfaceTraitModel interfaceTrait
                || interfaceTrait.Kind != InterfaceKind.Type
                || interfaceTrait.CppInterfaceName == null)
            {
                continue;
            }

            var name = resolver.TryResolveTypeInterfaceName(interfaceTrait.CppNamespace, interfaceTrait.CppInterfaceName);
            if (name == null)
            {
                continue;
            }

            names ??= new List<string>();
            if (!names.Contains(name))
            {
                names.Add(name);
            }
        }

        return names ?? (IReadOnlyList<string>)System.Array.Empty<string>();
    }
}
