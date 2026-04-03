namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var baseType = GetBaseType(attributeConstraint);
        builder.AppendLine("public sealed class " + className + " : " + baseType);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatType = GetAssemblyFormatType(attributeConstraint);
        if (assemblyFormatType != null)
        {
            builder.Append(", new " + assemblyFormatType + "()");
        }

        builder.AppendLine(", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        var primitiveBaseConstructor = GetPrimitiveBaseConstructor(attributeConstraint);
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

        var valueConstructorParam = GetValueConstructorParameter(attributeConstraint);
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

    private static string GetBaseType(AttributeConstraintModel attributeConstraint)
    {
        return attributeConstraint.Kind switch
        {
            AttributeConstraintKind.BooleanLiteral => "BooleanAttributeValue",
            AttributeConstraintKind.IntegerLiteral => "IntegerAttributeValue",
            AttributeConstraintKind.FloatingPointLiteral => GetFloatingPointBaseType(attributeConstraint.RecordName),
            AttributeConstraintKind.StringLiteral => "StringAttributeValue",
            AttributeConstraintKind.DenseBooleanArrayAttribute => "DenseBooleanArrayAttributeValue",
            AttributeConstraintKind.DenseIntegerArrayAttribute => "DenseIntegerArrayAttributeValue",
            AttributeConstraintKind.DenseF32ArrayAttribute => "DenseF32ArrayAttributeValue",
            AttributeConstraintKind.DenseF64ArrayAttribute => "DenseF64ArrayAttributeValue",
            AttributeConstraintKind.ElementsAttribute => "ElementsAttributeValue",
            AttributeConstraintKind.DictionaryAttribute => "DictionaryAttributeValue",
            AttributeConstraintKind.OpaqueAttribute => "OpaqueAttributeValue",
            AttributeConstraintKind.TypeAttribute => "TypeAttributeValue",
            AttributeConstraintKind.UnitAttribute => "UnitAttributeValue",
            _ => "AttributeValue",
        };
    }

    private static string GetFloatingPointBaseType(string recordName)
    {
        return recordName switch
        {
            "F32Attr" => "F32AttributeValue",
            "F64Attr" => "F64AttributeValue",
            _ => "FloatingPointAttributeValue",
        };
    }

    private static string? GetAssemblyFormatType(AttributeConstraintModel attributeConstraint)
    {
        return attributeConstraint.Kind switch
        {
            AttributeConstraintKind.BooleanLiteral => "BooleanLiteralAttributeAssemblyFormat",
            AttributeConstraintKind.IntegerLiteral => "IntegerLiteralAttributeAssemblyFormat",
            AttributeConstraintKind.FloatingPointLiteral => GetFloatingPointAssemblyFormatType(attributeConstraint.RecordName),
            AttributeConstraintKind.StringLiteral => "StringLiteralAttributeAssemblyFormat",
            AttributeConstraintKind.DenseBooleanArrayAttribute => "DenseBooleanArrayAttributeAssemblyFormat",
            AttributeConstraintKind.DenseIntegerArrayAttribute => "DenseIntegerArrayAttributeAssemblyFormat",
            AttributeConstraintKind.DenseF32ArrayAttribute => "DenseF32ArrayAttributeAssemblyFormat",
            AttributeConstraintKind.DenseF64ArrayAttribute => "DenseF64ArrayAttributeAssemblyFormat",
            AttributeConstraintKind.ElementsAttribute => "ElementsAttributeAssemblyFormat",
            AttributeConstraintKind.DictionaryAttribute => "DictionaryAttributeAssemblyFormat",
            AttributeConstraintKind.TypeAttribute => "TypeAttributeAssemblyFormat",
            AttributeConstraintKind.UnitAttribute => "UnitAttributeAssemblyFormat",
            _ => null,
        };
    }

    private static string GetFloatingPointAssemblyFormatType(string recordName)
    {
        return recordName switch
        {
            "F32Attr" => "F32AttributeAssemblyFormat",
            "F64Attr" => "F64AttributeAssemblyFormat",
            _ => "FloatingPointLiteralAttributeAssemblyFormat",
        };
    }

    private static string? GetPrimitiveBaseConstructor(AttributeConstraintModel attributeConstraint)
    {
        return attributeConstraint.Kind switch
        {
            AttributeConstraintKind.BooleanLiteral => "context, ((BooleanAttributeValueSyntax)context.Syntax).Value",
            AttributeConstraintKind.IntegerLiteral => "context, ((IntegerAttributeValueSyntax)context.Syntax).Value",
            AttributeConstraintKind.FloatingPointLiteral => GetFloatingPointBaseConstructor(attributeConstraint.RecordName),
            AttributeConstraintKind.StringLiteral => "context, ((StringAttributeValueSyntax)context.Syntax).Value",
            AttributeConstraintKind.DenseBooleanArrayAttribute => "context, StructuredAttributeSemanticDecoder.DecodeBooleanItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)",
            AttributeConstraintKind.DenseIntegerArrayAttribute => "context, StructuredAttributeSemanticDecoder.DecodeIntegerItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)",
            AttributeConstraintKind.DenseF32ArrayAttribute => "context, StructuredAttributeSemanticDecoder.DecodeSinglePrecisionItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)",
            AttributeConstraintKind.DenseF64ArrayAttribute => "context, StructuredAttributeSemanticDecoder.DecodeDoublePrecisionItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)",
            AttributeConstraintKind.ElementsAttribute => "context, StructuredAttributeSemanticDecoder.DecodeValue(((ElementsAttributeValueSyntax)context.Syntax).Payload), ((ElementsAttributeValueSyntax)context.Syntax).TypeSyntax",
            AttributeConstraintKind.DictionaryAttribute => "context, StructuredAttributeSemanticDecoder.DecodeAttributes(((DictionaryAttributeValueSyntax)context.Syntax).Attributes.Items)",
            AttributeConstraintKind.OpaqueAttribute => "context",
            AttributeConstraintKind.TypeAttribute => "context, ((TypeAttributeValueSyntax)context.Syntax).TypeSyntax",
            AttributeConstraintKind.UnitAttribute => "context",
            _ => null,
        };
    }

    private static string GetFloatingPointBaseConstructor(string recordName)
    {
        return recordName switch
        {
            "F32Attr" => "context, global::System.Single.Parse(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText, global::System.Globalization.CultureInfo.InvariantCulture)",
            "F64Attr" => "context, global::System.Double.Parse(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText, global::System.Globalization.CultureInfo.InvariantCulture)",
            _ => "context, ((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText",
        };
    }

    private static string? GetValueConstructorParameter(AttributeConstraintModel attributeConstraint)
    {
        return attributeConstraint.Kind switch
        {
            AttributeConstraintKind.IntegerLiteral => "global::System.Numerics.BigInteger",
            AttributeConstraintKind.BooleanLiteral => "bool",
            AttributeConstraintKind.StringLiteral => "string",
            AttributeConstraintKind.FloatingPointLiteral => GetFloatingPointValueConstructorParameter(attributeConstraint.RecordName),
            AttributeConstraintKind.DenseBooleanArrayAttribute => "global::System.Collections.Generic.IReadOnlyList<bool>",
            AttributeConstraintKind.DenseIntegerArrayAttribute => "global::System.Collections.Generic.IReadOnlyList<global::System.Numerics.BigInteger>",
            AttributeConstraintKind.DenseF32ArrayAttribute => "global::System.Collections.Generic.IReadOnlyList<float>",
            AttributeConstraintKind.DenseF64ArrayAttribute => "global::System.Collections.Generic.IReadOnlyList<double>",
            _ => null,
        };
    }

    private static string GetFloatingPointValueConstructorParameter(string recordName)
    {
        return recordName switch
        {
            "F32Attr" => "float",
            "F64Attr" => "double",
            _ => "string",
        };
    }
}
