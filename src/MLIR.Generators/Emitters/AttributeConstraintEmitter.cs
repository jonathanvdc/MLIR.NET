namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        if (attributeConstraint.Kind == AttributeConstraintKind.EnumAttribute
            && attributeConstraint.EnumModel != null)
        {
            EmitEnumConstraint(builder, attributeConstraint, attributeConstraint.EnumModel);
        }
        else if (attributeConstraint.Kind == AttributeConstraintKind.TypedArrayAttribute)
        {
            EmitTypedArrayConstraint(builder, attributeConstraint, resolver);
        }
        else
        {
            EmitStandardConstraint(builder, attributeConstraint);
        }
    }

    private static void EmitEnumConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, EnumModel enumModel)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        // Constraint class
        var assemblyFormatType = className + "AssemblyFormat";
        builder.AppendLine("public sealed class " + className + " : IntegerAttributeValue");
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.AppendLine("        new AttributeConstraintDefinition(");
        builder.Append("            " + EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        builder.AppendLine(", new " + assemblyFormatType + "(), factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context, ParseValue(context.Syntax))");
        builder.AppendLine("    {");
        builder.AppendLine("        EnumValue = ParseEnumValue(context.Syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(" + enumTypeName + " value)");
        builder.AppendLine("        : base((global::System.Numerics.BigInteger)(ulong)value)");
        builder.AppendLine("    {");
        builder.AppendLine("        EnumValue = value;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + enumTypeName + " EnumValue { get; }");
        builder.AppendLine("    public " + enumTypeName + " TypedValue => EnumValue;");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine();

        // Enum value parser
        builder.AppendLine("    private static " + enumTypeName + " ParseEnumValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax == null) return default;");
        builder.AppendLine("        if (syntax is MLIR.Syntax.Attributes.Primitives.IntegerAttributeValueSyntax integerSyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            return " + EnumEmitter.GetEnumInfoClassName(enumModel) + ".TryFromInteger(integerSyntax.Value, out var integerValue) ? integerValue : default;");
        builder.AppendLine("        }");
        builder.AppendLine("        var raw = syntax.ToString();");
        EnumEmitter.EmitParseExpression(builder, enumModel, enumTypeName, "raw", "        ");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Integer value parser (to feed IntegerAttributeValue base constructor)
        builder.AppendLine("    private static global::System.Numerics.BigInteger ParseValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return (global::System.Numerics.BigInteger)global::System.Convert.ToUInt64(ParseEnumValue(syntax));");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Print helper
        builder.AppendLine("    internal string PrintEnumValue(" + enumTypeName + " value)");
        builder.AppendLine("    {");
        EnumEmitter.EmitFormatExpression(builder, enumModel, "value", "        ");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        // Assembly format class for enum constraint
        builder.AppendLine("internal sealed class " + assemblyFormatType + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var firstToken)");
        builder.AppendLine("            && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out firstToken))");
        builder.AppendLine("        {");
        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var rawText = firstToken.Text;");
        if (enumModel.IsBitEnum)
        {
            var sepKind = EnumEmitter.GetSeparatorTokenKind(enumModel);
            builder.AppendLine("        while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
            builder.AppendLine("        {");
            builder.AppendLine("            if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
            builder.AppendLine("                && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
            builder.AppendLine("            {");
            builder.AppendLine("                break;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + " + nextToken.Text;");
            builder.AppendLine("        }");
        }

        builder.AppendLine();
        builder.AppendLine("        return ParseResult<AttributeValueSyntax>.Success(new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(rawText)));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return definition.Factory(new AttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        var enumAttr = (" + className + ")attribute;");
        builder.AppendLine("        var text = enumAttr.PrintEnumValue(enumAttr.EnumValue);");
        builder.AppendLine("        return new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(text));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitTypedArrayConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var elementRecordName = attributeConstraint.ElementConstraintRecordName;
        var elementKind = string.IsNullOrEmpty(elementRecordName)
            ? AttributeConstraintKind.None
            : resolver.TryResolveAttributeConstraintKind(elementRecordName!);
        var elementTypeName = GetTypedArrayElementTypeName(elementRecordName, resolver);
        var elementUsesPayload = UsesTypedArrayElementPayload(elementKind);
        var assemblyFormatType = className + "AssemblyFormat";

        builder.AppendLine("public sealed class " + className + " : TypedArrayAttributeValue<" + elementTypeName + ">");
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.AppendLine("        new AttributeConstraintDefinition(");
        builder.Append("            " + EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        builder.AppendLine(", new " + assemblyFormatType + "(), factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context, DecodeItems(context.Syntax))");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(global::System.Collections.Generic.IReadOnlyList<" + elementTypeName + "> items)");
        builder.AppendLine("        : base(items)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine();
        builder.AppendLine("    private static global::System.Collections.Generic.IReadOnlyList<" + elementTypeName + "> DecodeItems(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax is not MLIR.Syntax.Attributes.Collections.ArrayAttributeValueSyntax arraySyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            return global::System.Array.Empty<" + elementTypeName + ">();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var items = new global::System.Collections.Generic.List<" + elementTypeName + ">(arraySyntax.Items.Count);");
        builder.AppendLine("        for (var i = 0; i < arraySyntax.Items.Count; i++)");
        builder.AppendLine("        {");
        builder.AppendLine("            var itemSyntax = arraySyntax.Items[i];");
        if (elementUsesPayload)
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            var elementPayloadProperty = GetTypedArrayElementPayloadPropertyName(elementRecordName, resolver);
            builder.AppendLine("            var itemValue = new " + elementClassName + "(new MLIR.Semantics.AttributeValueConstructionContext(itemSyntax, " + elementClassName + ".AttributeConstraintDefinition.Name, " + elementClassName + ".AttributeConstraintDefinition, itemSyntax.Location));");
            builder.AppendLine("            items.Add(itemValue." + elementPayloadProperty + ");");
        }
        else
        {
            builder.AppendLine("            items.Add(MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue(itemSyntax));");
        }
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return items;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine("internal sealed class " + assemblyFormatType + " : TypedArrayAttributeAssemblyFormat<" + elementTypeName + ">");
        builder.AppendLine("{");
        builder.AppendLine("    protected override MLIR.Syntax.AttributeValueSyntax ElementToSyntax(" + elementTypeName + " element, MLIR.Transforms.ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        if (elementUsesPayload)
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            builder.AppendLine("        return context.BuildAttributeValueSyntax(new " + elementClassName + "(element));");
        }
        else
        {
            builder.AppendLine("        return context.BuildAttributeValueSyntax(element);");
        }
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static string GetTypedArrayElementTypeName(string? elementRecordName, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "global::MLIR.Semantics.AttributeValue";
        }

        var kind = resolver.TryResolveAttributeConstraintKind(elementRecordName!);
        if (IsGenericTypedArrayElementKind(kind))
        {
            return "global::MLIR.Semantics.AttributeValue";
        }

        return AttributeTypeResolver.GetAttributeValueTypeName(elementRecordName, resolver)
            ?? "global::MLIR.Semantics.AttributeValue";
    }

    private static string GetTypedArrayElementClassName(string? elementRecordName, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "global::MLIR.Semantics.AttributeValue";
        }

        return resolver.TryResolveAttributeConstraintClassName(elementRecordName!)
            ?? "global::MLIR.Semantics.AttributeValue";
    }

    private static string GetTypedArrayElementPayloadPropertyName(string? elementRecordName, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "Syntax";
        }

        return resolver.TryResolveAttributeConstraintKind(elementRecordName!) switch
        {
            AttributeConstraintKind.BooleanLiteral => "Value",
            AttributeConstraintKind.IntegerLiteral => "Value",
            AttributeConstraintKind.FloatingPointLiteral => "Value",
            AttributeConstraintKind.StringLiteral => "Value",
            AttributeConstraintKind.EnumAttribute => "TypedValue",
            AttributeConstraintKind.TypeAttribute => "TypeSyntax",
            AttributeConstraintKind.DictionaryAttribute => "Attributes",
            AttributeConstraintKind.DenseBooleanArrayAttribute => "Items",
            AttributeConstraintKind.DenseIntegerArrayAttribute => "Items",
            AttributeConstraintKind.DenseF32ArrayAttribute => "Items",
            AttributeConstraintKind.DenseF64ArrayAttribute => "Items",
            AttributeConstraintKind.TypedArrayAttribute => "Items",
            AttributeConstraintKind.ElementsAttribute => "Payload",
            _ => "Value",
        };
    }

    private static bool IsGenericTypedArrayElementKind(AttributeConstraintKind kind)
    {
        return kind is AttributeConstraintKind.OpaqueAttribute
            or AttributeConstraintKind.ElementsAttribute
            or AttributeConstraintKind.UnitAttribute
            or AttributeConstraintKind.None;
    }

    private static bool UsesTypedArrayElementPayload(AttributeConstraintKind kind)
    {
        return kind is AttributeConstraintKind.BooleanLiteral
            or AttributeConstraintKind.IntegerLiteral
            or AttributeConstraintKind.FloatingPointLiteral
            or AttributeConstraintKind.StringLiteral
            or AttributeConstraintKind.EnumAttribute
            or AttributeConstraintKind.TypeAttribute
            or AttributeConstraintKind.DictionaryAttribute
            or AttributeConstraintKind.DenseBooleanArrayAttribute
            or AttributeConstraintKind.DenseIntegerArrayAttribute
            or AttributeConstraintKind.DenseF32ArrayAttribute
            or AttributeConstraintKind.DenseF64ArrayAttribute
            or AttributeConstraintKind.TypedArrayAttribute;
    }

    private static void EmitStandardConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint)
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

        if (attributeConstraint.Kind == AttributeConstraintKind.TypeAttribute)
        {
            builder.AppendLine();
            builder.AppendLine("    private static global::MLIR.Syntax.TypeSyntax DecodeTypeSyntax(MLIR.Syntax.AttributeValueSyntax? syntax)");
            builder.AppendLine("    {");
            builder.AppendLine("        return syntax is global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax typeSyntax");
            builder.AppendLine("            ? typeSyntax.TypeSyntax");
            builder.AppendLine("            : new global::MLIR.Syntax.RawTypeSyntax(syntax?.GetRawText() ?? new global::MLIR.Syntax.RawSyntaxText(string.Empty));");
            builder.AppendLine("    }");
        }
        else if (attributeConstraint.Kind == AttributeConstraintKind.DictionaryAttribute)
        {
            builder.AppendLine();
            builder.AppendLine("    private static global::MLIR.Semantics.NamedAttributeCollection DecodeAttributes(MLIR.Syntax.AttributeValueSyntax? syntax)");
            builder.AppendLine("    {");
            builder.AppendLine("        return syntax is global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax dictionarySyntax");
            builder.AppendLine("            ? global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes(dictionarySyntax.Attributes.Items)");
            builder.AppendLine("            : global::MLIR.Semantics.NamedAttributeCollection.Empty;");
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
            AttributeConstraintKind.DictionaryAttribute => "context, DecodeAttributes(context.Syntax)",
            AttributeConstraintKind.OpaqueAttribute => "context",
            AttributeConstraintKind.TypeAttribute => "context, DecodeTypeSyntax(context.Syntax)",
            AttributeConstraintKind.UnitAttribute => "context",
            _ => null,
        };
    }

    private static string GetFloatingPointBaseConstructor(string recordName)
    {
        return recordName switch
        {
            "F32Attr" => "context, global::MLIR.Semantics.Attributes.Primitives.FloatingPointLiteralParser.ParseSingle(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText)",
            "F64Attr" => "context, global::MLIR.Semantics.Attributes.Primitives.FloatingPointLiteralParser.ParseDouble(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText)",
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
            AttributeConstraintKind.DictionaryAttribute => "global::MLIR.Semantics.NamedAttributeCollection",
            AttributeConstraintKind.TypeAttribute => "global::MLIR.Syntax.TypeSyntax",
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
