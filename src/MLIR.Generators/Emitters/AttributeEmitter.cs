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
        builder.AppendLine("    public static AttributeDefinition AttributeDefinition { get; } =");
        builder.AppendLine("        new AttributeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Location)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeDefinition.Name;");
        builder.AppendLine("    public override AttributeDefinition? Definition => AttributeDefinition;");
        builder.AppendLine("}");
    }
}
