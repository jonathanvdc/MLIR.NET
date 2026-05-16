namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Text;
using MLIR.Generators.Emitters.Common;

/// <summary>
/// Shared statement emitters used by subject-specific bind/build implementations.
/// </summary>
internal static class AssemblyFormatEmitterHelpers
{
    public static void EmitUnsupportedThrow(StringBuilder builder, AssemblyFormatPlan plan, string action)
    {
        var message = "Unsupported declarative assembly format construct during " + action + ": " + plan.UnsupportedFeatures[0] + ".";
        builder.AppendLine("        throw new global::System.NotSupportedException(" + EmitterHelpers.ToCSharpStringLiteral(message) + ");");
    }

    public static void EmitAttrOrTypeBuildBody(StringBuilder builder, AssemblyFormatPlan plan, string valueParameterName, string typedLocalName, string prefixExpression)
    {
        builder.AppendLine("    {");
        builder.AppendLine("        var " + typedLocalName + " = (" + plan.Subject.ClassName + ")" + valueParameterName + ";");
        builder.AppendLine("        if (" + typedLocalName + ".Syntax is " + plan.Subject.SyntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");
        foreach (var slot in plan.Slots)
        {
            builder.AppendLine("        var " + slot.ParameterName + " = " + slot.BuildExpression(typedLocalName) + ";");
        }

        builder.Append("        return new " + plan.Subject.SyntaxClassName + "(" + prefixExpression);
        foreach (var slot in plan.Slots)
        {
            builder.Append(", " + slot.ParameterName);
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
    }
}
