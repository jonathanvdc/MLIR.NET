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

        var valueConstructorParam = GetValueConstructorParameter(attributeConstraint.Kind);
        if (valueConstructorParam != null)
        {
            builder.AppendLine();
            builder.AppendLine("    public " + className + "(" + valueConstructorParam + " value)");
            builder.AppendLine("        : base(value)");
            builder.AppendLine("    {");
            builder.AppendLine("    }");
        }

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
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
            AttributeConstraintKind.DenseArrayAttribute => "ArrayAttributeValue",
            AttributeConstraintKind.ElementsAttribute => "ElementsAttributeValue",
            AttributeConstraintKind.DictionaryAttribute => "DictionaryAttributeValue",
            AttributeConstraintKind.OpaqueAttribute => "OpaqueAttributeValue",
            AttributeConstraintKind.TypeAttribute => "TypeAttributeValue",
            AttributeConstraintKind.UnitAttribute => "UnitAttributeValue",
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
            AttributeConstraintKind.DenseArrayAttribute => "DenseArrayAttributeAssemblyFormat",
            AttributeConstraintKind.ElementsAttribute => "ElementsAttributeAssemblyFormat",
            AttributeConstraintKind.DictionaryAttribute => "DictionaryAttributeAssemblyFormat",
            AttributeConstraintKind.TypeAttribute => "TypeAttributeAssemblyFormat",
            AttributeConstraintKind.UnitAttribute => "UnitAttributeAssemblyFormat",
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
            AttributeConstraintKind.DenseArrayAttribute => "context, StructuredAttributeSemanticDecoder.DecodeItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)",
            AttributeConstraintKind.ElementsAttribute => "context, StructuredAttributeSemanticDecoder.DecodeValue(((ElementsAttributeValueSyntax)context.Syntax).Payload), ((ElementsAttributeValueSyntax)context.Syntax).TypeSyntax",
            AttributeConstraintKind.DictionaryAttribute => "context, StructuredAttributeSemanticDecoder.DecodeAttributes(((DictionaryAttributeValueSyntax)context.Syntax).Attributes.Items)",
            AttributeConstraintKind.OpaqueAttribute => "context",
            AttributeConstraintKind.TypeAttribute => "context, ((TypeAttributeValueSyntax)context.Syntax).TypeSyntax",
            AttributeConstraintKind.UnitAttribute => "context",
            _ => null,
        };
    }

    private static string? GetValueConstructorParameter(AttributeConstraintKind kind)
    {
        return kind switch
        {
            AttributeConstraintKind.IntegerLiteral => "global::System.Numerics.BigInteger",
            AttributeConstraintKind.BooleanLiteral => "bool",
            AttributeConstraintKind.StringLiteral => "string",
            AttributeConstraintKind.FloatingPointLiteral => "string",
            _ => null,
        };
    }
}
