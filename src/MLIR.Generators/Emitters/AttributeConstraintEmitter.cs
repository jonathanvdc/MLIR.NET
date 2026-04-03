namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint)
    {
        if (attributeConstraint.Kind == AttributeConstraintKind.EnumAttribute
            && attributeConstraint.EnumModel != null)
        {
            EmitEnumConstraint(builder, attributeConstraint, attributeConstraint.EnumModel);
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

        // Emit the C# enum type
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("[global::System.Flags]");
        }

        var underlyingType = GetUnderlyingCSharpType(enumModel.Bitwidth);
        builder.AppendLine("public enum " + enumTypeName + " : " + underlyingType);
        builder.AppendLine("{");
        foreach (var enumCase in enumModel.Cases)
        {
            var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
            builder.AppendLine("    " + memberName + " = " + enumCase.Value + ",");
        }

        builder.AppendLine("}");
        builder.AppendLine();

        // Symbol-to-enum lookup dictionary (file-scoped to avoid redundancy in the generated source)
        builder.AppendLine("internal static class " + enumTypeName + "EnumParser");
        builder.AppendLine("{");
        builder.AppendLine("    internal static readonly global::System.Collections.Generic.Dictionary<string, " + enumTypeName + "> SymbolToEnum =");
        builder.AppendLine("        new global::System.Collections.Generic.Dictionary<string, " + enumTypeName + ">(global::System.StringComparer.Ordinal)");
        builder.AppendLine("        {");
        foreach (var enumCase in enumModel.Cases)
        {
            var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
            builder.AppendLine("            { " + EmitterHelpers.ToCSharpStringLiteral(enumCase.Str) + ", " + enumTypeName + "." + memberName + " },");
        }

        builder.AppendLine("        };");
        builder.AppendLine();
        builder.AppendLine("    internal static readonly global::System.Collections.Generic.Dictionary<" + enumTypeName + ", string> EnumToSymbol =");
        builder.AppendLine("        new global::System.Collections.Generic.Dictionary<" + enumTypeName + ", string>()");
        builder.AppendLine("        {");
        var seenValues = new HashSet<long>();
        foreach (var enumCase in enumModel.Cases)
        {
            if (seenValues.Add(enumCase.Value))
            {
                var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
                builder.AppendLine("            { " + enumTypeName + "." + memberName + ", " + EmitterHelpers.ToCSharpStringLiteral(enumCase.Str) + " },");
            }
        }

        builder.AppendLine("        };");
        builder.AppendLine("}");
        builder.AppendLine();

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
        builder.AppendLine("    public " + enumTypeName + " EnumValue { get; }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine();

        // Enum value parser
        builder.AppendLine("    private static " + enumTypeName + " ParseEnumValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax == null) return default;");
        builder.AppendLine("        var raw = syntax.GetRawText().Text.Trim();");
        if (enumModel.IsBitEnum)
        {
            var sep = enumModel.Separator.Contains(",") ? "," : "|";
            builder.AppendLine("        var parts = raw.Split('" + sep + "');");
            builder.AppendLine("        var result = (" + enumTypeName + ")0;");
            builder.AppendLine("        foreach (var part in parts)");
            builder.AppendLine("        {");
            builder.AppendLine("            var trimmed = part.Trim();");
            builder.AppendLine("            if (" + enumTypeName + "EnumParser.SymbolToEnum.TryGetValue(trimmed, out var flag)) result |= flag;");
            builder.AppendLine("        }");
            builder.AppendLine("        return result;");
        }
        else
        {
            builder.AppendLine("        return " + enumTypeName + "EnumParser.SymbolToEnum.TryGetValue(raw, out var v) ? v : default;");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        // Integer value parser (to feed IntegerAttributeValue base constructor)
        builder.AppendLine("    private static global::System.Numerics.BigInteger ParseValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return (long)(object)ParseEnumValue(syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Print helper
        builder.AppendLine("    internal string PrintEnumValue(" + enumTypeName + " value)");
        builder.AppendLine("    {");
        if (enumModel.IsBitEnum)
        {
            var sep = enumModel.Separator.Contains(",") ? "\", \"" : "\" | \"";
            builder.AppendLine("        if (" + enumTypeName + "EnumParser.EnumToSymbol.TryGetValue(value, out var directStr)) return directStr;");
            builder.AppendLine("        var parts = new global::System.Collections.Generic.List<string>();");
            builder.AppendLine("        foreach (var pair in " + enumTypeName + "EnumParser.EnumToSymbol)");
            builder.AppendLine("        {");
            builder.AppendLine("            var flag = pair.Key;");
            builder.AppendLine("            if ((long)(object)flag != 0 && ((long)(object)(value & flag) == (long)(object)flag))");
            builder.AppendLine("            {");
            builder.AppendLine("                parts.Add(pair.Value);");
            builder.AppendLine("                value &= ~flag;");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine("        return string.Join(" + sep + ", parts);");
        }
        else
        {
            builder.AppendLine("        return " + enumTypeName + "EnumParser.EnumToSymbol.TryGetValue(value, out var s) ? s : value.ToString();");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        // Assembly format class for enum constraint
        builder.AppendLine("internal sealed class " + assemblyFormatType + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        syntax = null;");
        builder.AppendLine("        if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var firstToken)");
        builder.AppendLine("            && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out firstToken))");
        builder.AppendLine("        {");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var rawText = firstToken.Text;");
        if (enumModel.IsBitEnum)
        {
            var sepKind = enumModel.Separator.Contains(",") ? "TokenKind.Comma" : "TokenKind.Pipe";
            builder.AppendLine("        while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
            builder.AppendLine("        {");
            builder.AppendLine("            if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
            builder.AppendLine("                && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
            builder.AppendLine("            {");
            builder.AppendLine("                break;");
            builder.AppendLine("            }");
            builder.AppendLine();
            var sep = enumModel.Separator.Contains(",") ? "\", \"" : "\" | \"";
            builder.AppendLine("            rawText += " + sep + " + nextToken.Text;");
            builder.AppendLine("        }");
        }

        builder.AppendLine();
        builder.AppendLine("        syntax = new MLIR.Syntax.Attributes.Primitives.StringAttributeValueSyntax(");
        builder.AppendLine("            new MLIR.Text.SyntaxToken(rawText, firstToken.LeadingTrivia, firstToken.Location.Line, firstToken.Location.Column),");
        builder.AppendLine("            rawText);");
        builder.AppendLine("        return true;");
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
        builder.AppendLine("        return new MLIR.Syntax.Attributes.Primitives.StringAttributeValueSyntax(");
        builder.AppendLine("            new MLIR.Text.SyntaxToken(text), text);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
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

    private static string GetUnderlyingCSharpType(int bitwidth) => bitwidth switch
    {
        8 => "byte",
        16 => "ushort",
        32 => "uint",
        64 => "ulong",
        _ => "ulong",
    };
}
