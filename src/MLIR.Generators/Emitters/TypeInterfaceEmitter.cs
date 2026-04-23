namespace MLIR.Generators.Emitters;

using System.Text;
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

            builder.AppendLine("    " + member.CsharpType + " " + member.CsharpName + " { get; }");
        }
        builder.AppendLine("}");
    }
}
