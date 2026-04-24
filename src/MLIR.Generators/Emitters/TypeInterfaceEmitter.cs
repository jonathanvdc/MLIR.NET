namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

/// <summary>
/// Emits a partial C# marker interface declaration for a <c>TypeInterface</c> record.
/// </summary>
/// <remarks>
/// Generated interfaces are partial so that hand-written runtime code can add helper
/// members later without conflicting with generated source. No C++ interface method
/// signatures are translated to C# members in this foundational layer.
/// </remarks>
internal static class TypeInterfaceEmitter
{
    /// <summary>
    /// Emits a <c>public partial interface</c> declaration for the given type interface model.
    /// </summary>
    public static void Emit(StringBuilder builder, InterfaceModel interfaceModel)
    {
        var interfaceName = DialectGeneratorNaming.GetTypeInterfaceName(interfaceModel);
        EmitterHelpers.AppendXmlDocComment(
            builder,
            interfaceModel.CsharpSummary,
            interfaceModel.CsharpDescription ?? interfaceModel.Description);
        builder.AppendLine("public partial interface " + interfaceName);
        builder.AppendLine("{");
        foreach (var member in interfaceModel.CsharpMembers)
        {
            if (member.Kind != InterfaceCSharpMemberKind.Property)
            {
                throw new System.InvalidOperationException(
                    "Unsupported mapped type interface member kind '" + member.Kind + "' for interface '"
                    + interfaceModel.RecordName + "'.");
            }

            var upstreamMethod = FindUpstreamMethod(interfaceModel, member.UpstreamName);
            EmitterHelpers.AppendXmlDocComment(
                builder,
                member.CsharpSummary,
                member.CsharpDescription ?? upstreamMethod?.Description,
                "    ");
            builder.AppendLine("    " + member.CsharpType + " " + member.CsharpName + " { get; }");
        }
        builder.AppendLine("}");
    }

    private static InterfaceMethodModel? FindUpstreamMethod(InterfaceModel interfaceModel, string upstreamName)
    {
        foreach (var method in interfaceModel.Methods)
        {
            if (string.Equals(method.Name, upstreamName, System.StringComparison.Ordinal))
            {
                return method;
            }
        }

        return null;
    }
}
