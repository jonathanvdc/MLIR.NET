namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Text;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class TypeInterfaceImplementationEmitter
{
    public static void Emit(StringBuilder builder, TypeModel type, IReadOnlyList<ResolvedTypeInterfaceModel> interfaces)
    {
        foreach (var resolvedInterface in interfaces)
        {
            foreach (var member in resolvedInterface.InterfaceModel.CsharpMembers)
            {
                if (HasCompatibleGeneratedProperty(type, member))
                {
                    continue;
                }

                if (!TryFindExplicitImplementation(type, resolvedInterface.InterfaceModel.RecordName, member.CsharpName, out var implementation))
                {
                    throw new InvalidOperationException(
                        "Type '" + type.RecordName + "' implements mapped type interface '"
                        + resolvedInterface.InterfaceModel.RecordName + "' but does not provide required property '"
                        + member.CsharpName + "' of type '" + member.CsharpType
                        + "'. Add a matching generated property or a csharpInterfaceImplementations entry.");
                }

                if (member.Kind != InterfaceCSharpMemberKind.Property)
                {
                    throw new InvalidOperationException(
                        "Type interface member kind '" + member.Kind + "' is not supported for type '"
                        + type.RecordName + "'.");
                }

                var expressionTemplate = implementation!.CsharpExpressionTemplate;
                expressionTemplate?.RequireOnly();
                var renderedExpression = expressionTemplate?.Render(new Dictionary<string, string>()) ?? implementation.CsharpExpression;

                var upstreamMethod = FindUpstreamMethod(resolvedInterface.InterfaceModel, member.UpstreamName);
                EmitterHelpers.AppendXmlDocComment(
                    builder,
                    member.CsharpSummary,
                    member.CsharpDescription ?? upstreamMethod?.Description,
                    "    ");
                builder.AppendLine("    public " + member.CsharpType + " " + member.CsharpName + " => " + renderedExpression + ";");
            }
        }
    }

    private static bool HasCompatibleGeneratedProperty(TypeModel type, InterfaceCSharpMemberModel member)
    {
        foreach (var parameter in type.Parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(parameter.Name);
            var propertyType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(parameter);
            if (string.Equals(propertyName, member.CsharpName, StringComparison.Ordinal))
            {
                if (!string.Equals(propertyType, member.CsharpType, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Type '" + type.RecordName + "' has generated property '" + propertyName
                        + "' with C# type '" + propertyType + "', but mapped interface member '"
                        + member.CsharpName + "' requires type '" + member.CsharpType + "'.");
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryFindExplicitImplementation(
        TypeModel type,
        string interfaceRecordName,
        string csharpMemberName,
        out InterfaceMemberImplementationModel? implementation)
    {
        foreach (var candidate in type.InterfaceMemberImplementations)
        {
            if (string.Equals(candidate.InterfaceRecordName, interfaceRecordName, StringComparison.Ordinal)
                && string.Equals(candidate.CsharpMemberName, csharpMemberName, StringComparison.Ordinal))
            {
                implementation = candidate;
                return true;
            }
        }

        implementation = null;
        return false;
    }

    private static InterfaceMethodModel? FindUpstreamMethod(InterfaceModel interfaceModel, string upstreamName)
    {
        foreach (var method in interfaceModel.Methods)
        {
            if (string.Equals(method.Name, upstreamName, StringComparison.Ordinal))
            {
                return method;
            }
        }

        return null;
    }
}
