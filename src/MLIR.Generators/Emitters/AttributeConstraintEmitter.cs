namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var baseType = attributeConstraint.Kind == AttributeConstraintKind.IntegerLiteral
            ? "IntegerAttributeValue"
            : "AttributeValue";
        builder.AppendLine("public sealed class " + className + " : " + baseType);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        if (attributeConstraint.Kind == AttributeConstraintKind.IntegerLiteral)
        {
            builder.Append(", new IntegerLiteralAttributeAssemblyFormat()");
        }

        builder.AppendLine(", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        if (attributeConstraint.Kind == AttributeConstraintKind.IntegerLiteral)
        {
            builder.AppendLine("        : base(context, ((IntegerAttributeValueSyntax)context.Syntax).Value)");
        }
        else
        {
            builder.AppendLine("        : base(context.Syntax, context.Location)");
        }
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        if (attributeConstraint.Kind != AttributeConstraintKind.IntegerLiteral)
        {
            builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
            builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        }
        builder.AppendLine("}");
    }
}
