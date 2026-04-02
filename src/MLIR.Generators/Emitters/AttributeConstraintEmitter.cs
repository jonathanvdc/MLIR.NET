namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var baseType = GetBaseType(attributeConstraint.Kind);
        builder.AppendLine("public sealed class " + className + " : " + baseType);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatType = GetAssemblyFormatType(attributeConstraint.Kind);
        if (assemblyFormatType != null)
        {
            builder.Append(", new " + assemblyFormatType + "()");
        }

        builder.AppendLine(", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        var primitiveBaseConstructor = GetPrimitiveBaseConstructor(attributeConstraint.Kind);
        if (primitiveBaseConstructor != null)
        {
            builder.AppendLine("        : base(" + primitiveBaseConstructor + ")");
        }
        else
        {
            builder.AppendLine("        : base(context.Syntax, context.Location)");
        }
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        if (primitiveBaseConstructor == null)
        {
            builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
            builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        }
        builder.AppendLine("}");
    }

    private static string GetBaseType(AttributeConstraintKind kind)
    {
        return kind switch
        {
            AttributeConstraintKind.BooleanLiteral => "BooleanAttributeValue",
            AttributeConstraintKind.IntegerLiteral => "IntegerAttributeValue",
            AttributeConstraintKind.FloatingPointLiteral => "FloatingPointAttributeValue",
            AttributeConstraintKind.StringLiteral => "StringAttributeValue",
            _ => "AttributeValue",
        };
    }

    private static string? GetAssemblyFormatType(AttributeConstraintKind kind)
    {
        return kind switch
        {
            AttributeConstraintKind.BooleanLiteral => "BooleanLiteralAttributeAssemblyFormat",
            AttributeConstraintKind.IntegerLiteral => "IntegerLiteralAttributeAssemblyFormat",
            AttributeConstraintKind.FloatingPointLiteral => "FloatingPointLiteralAttributeAssemblyFormat",
            AttributeConstraintKind.StringLiteral => "StringLiteralAttributeAssemblyFormat",
            _ => null,
        };
    }

    private static string? GetPrimitiveBaseConstructor(AttributeConstraintKind kind)
    {
        return kind switch
        {
            AttributeConstraintKind.BooleanLiteral => "context, ((BooleanAttributeValueSyntax)context.Syntax).Value",
            AttributeConstraintKind.IntegerLiteral => "context, ((IntegerAttributeValueSyntax)context.Syntax).Value",
            AttributeConstraintKind.FloatingPointLiteral => "context, ((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText",
            AttributeConstraintKind.StringLiteral => "context, ((StringAttributeValueSyntax)context.Syntax).Value",
            _ => null,
        };
    }
}
