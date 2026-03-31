namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal static class AttributeEmitter
{
    public static void Emit(StringBuilder builder, AttributeModel attribute)
    {
        var className = DialectGeneratorNaming.GetAttributeClassName(attribute);
        builder.AppendLine("public sealed class " + className + " : AttributeValue");
        builder.AppendLine("{");
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Name, context.Definition, context.Location)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
