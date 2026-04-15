namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class EnumEmitter
{
    public static string GetEnumConstraintAssemblyFormatTypeName(string constraintRecordName)
    {
        return DialectGeneratorNaming.ToPascalCase(constraintRecordName.Replace('.', '_')) + "ConstraintAttributeValueAssemblyFormat";
    }

    public static string GetIntegerTypeFactoryExpression(int bitwidth) => bitwidth switch
    {
        1 => "global::MLIR.Semantics.TypeFactory.I1",
        8 => "global::MLIR.Semantics.TypeFactory.I8",
        16 => "global::MLIR.Semantics.TypeFactory.I16",
        32 => "global::MLIR.Semantics.TypeFactory.I32",
        64 => "global::MLIR.Semantics.TypeFactory.I64",
        _ => $"global::MLIR.Semantics.TypeFactory.I({bitwidth})",
    };

    public static string GetEnumToIntegerAttrExpression(EnumModel enumModel, string enumValueExpression, string syntaxExpression)
    {
        return "new global::MLIR.IntegerAttr("
            + GetIntegerTypeFactoryExpression(enumModel.Bitwidth)
            + ", global::MLIR.Numerics.ApInt.FromUInt64("
            + enumModel.Bitwidth
            + ", (ulong)"
            + enumValueExpression
            + "), "
            + syntaxExpression
            + ")";
    }

    public static string GetIntegerToEnumExpression(EnumModel enumModel, string apIntExpression, string fallbackExpression)
    {
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);
        return GetEnumInfoClassName(enumModel)
            + ".TryFromInteger("
            + apIntExpression
            + ", out "
            + enumTypeName
            + " enumValue) ? enumValue : "
            + fallbackExpression;
    }

    public static void EmitSharedDefinitions(StringBuilder builder, EnumModel enumModel)
    {
        EmitEnumType(builder, enumModel);
        builder.AppendLine();
        EmitEnumInfo(builder, enumModel);
    }

    public static string GetEnumInfoClassName(EnumModel enumModel)
    {
        return EnumHelpers.GetCSharpEnumTypeName(enumModel) + "Info";
    }

    public static string GetUnderlyingCSharpType(int bitwidth) => bitwidth switch
    {
        8 => "byte",
        16 => "ushort",
        32 => "uint",
        64 => "ulong",
        _ => "ulong",
    };

    public static string GetSeparatorTokenKind(EnumModel enumModel)
    {
        return enumModel.Separator.TrimStart().StartsWith(",", System.StringComparison.Ordinal)
            ? "TokenKind.Comma"
            : "TokenKind.Pipe";
    }

    public static void EmitParseExpression(StringBuilder builder, EnumModel enumModel, string enumTypeName, string rawExpression, string indent)
    {
        var infoClassName = GetEnumInfoClassName(enumModel);
        builder.AppendLine(indent + "return " + infoClassName + ".TryParse(" + rawExpression + ", out var value) ? value : default;");
    }

    public static void EmitFormatExpression(StringBuilder builder, EnumModel enumModel, string valueExpression, string indent)
    {
        var infoClassName = GetEnumInfoClassName(enumModel);
        builder.AppendLine(indent + "return " + infoClassName + ".Format(" + valueExpression + ");");
    }

    public static void EmitParseEnumValueHelperMethod(
        StringBuilder builder,
        EnumModel enumModel,
        string enumTypeName,
        string indent,
        string accessibility,
        bool includeIntegerLiteralSyntaxFallback,
        bool allowBitEnumAngleBrackets)
    {
        builder.AppendLine(indent + accessibility + " " + enumTypeName + " ParseEnumValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    if (syntax == null) return default;");
        if (includeIntegerLiteralSyntaxFallback)
        {
            builder.AppendLine(indent + "    if (syntax is MLIR.Syntax.Attributes.Primitives.IntegerAttributeValueSyntax integerSyntax)");
            builder.AppendLine(indent + "    {");
            builder.AppendLine(indent + "        return " + GetIntegerToEnumExpression(enumModel, "integerSyntax.Value", "default") + ";");
            builder.AppendLine(indent + "    }");
        }

        builder.AppendLine(indent + "    var raw = syntax.ToString();");
        if (allowBitEnumAngleBrackets && enumModel.IsBitEnum)
        {
            builder.AppendLine(indent + "    if (raw.Length >= 2 && raw[0] == '<' && raw[raw.Length - 1] == '>')");
            builder.AppendLine(indent + "    {");
            builder.AppendLine(indent + "        raw = raw.Substring(1, raw.Length - 2).Trim();");
            builder.AppendLine(indent + "    }");
        }

        EmitParseExpression(builder, enumModel, enumTypeName, "raw", indent + "    ");
        builder.AppendLine(indent + "}");
        builder.AppendLine();
    }

    public static void EmitPrintEnumValueHelperMethod(
        StringBuilder builder,
        EnumModel enumModel,
        string enumTypeName,
        string indent,
        string accessibility)
    {
        builder.AppendLine(indent + accessibility + " string PrintEnumValue(" + enumTypeName + " value)");
        builder.AppendLine(indent + "{");
        EmitFormatExpression(builder, enumModel, "value", indent + "    ");
        builder.AppendLine(indent + "}");
        builder.AppendLine();
    }

    public static void EmitAssemblyFormatTryParseMethod(
        StringBuilder builder,
        EnumModel enumModel,
        string indent,
        bool allowBitEnumAngleBrackets)
    {
        builder.AppendLine(indent + "public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)");
        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var firstToken)");
        builder.AppendLine(indent + "        && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out firstToken)");
        if (allowBitEnumAngleBrackets && enumModel.IsBitEnum)
        {
            builder.AppendLine(indent + "        && !context.TryMatch(MLIR.Text.TokenKind.LessThan, out firstToken))");
        }
        else
        {
            builder.AppendLine(indent + "        )");
        }

        builder.AppendLine(indent + "    {");
        builder.AppendLine(indent + "        return ParseResult<AttributeValueSyntax>.NoMatch();");
        builder.AppendLine(indent + "    }");
        builder.AppendLine();
        builder.AppendLine(indent + "    var rawText = firstToken.Text;");

        if (enumModel.IsBitEnum)
        {
            var sepKind = GetSeparatorTokenKind(enumModel);
            if (allowBitEnumAngleBrackets)
            {
                builder.AppendLine(indent + "    if (firstToken.Text == \"<\")");
                builder.AppendLine(indent + "    {");
                builder.AppendLine(indent + "        if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
                builder.AppendLine(indent + "            && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
                builder.AppendLine(indent + "        {");
                builder.AppendLine(indent + "            return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic(\"Expected an enum element.\", firstToken.Location.Line, firstToken.Location.Column));");
                builder.AppendLine(indent + "        }");
                builder.AppendLine();
                builder.AppendLine(indent + "        rawText += nextToken.Text;");
                builder.AppendLine(indent + "        while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
                builder.AppendLine(indent + "        {");
                builder.AppendLine(indent + "            rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + ";");
                builder.AppendLine(indent + "            if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out nextToken)");
                builder.AppendLine(indent + "                && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
                builder.AppendLine(indent + "            {");
                builder.AppendLine(indent + "                return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic(\"Expected an enum element.\", firstToken.Location.Line, firstToken.Location.Column));");
                builder.AppendLine(indent + "            }");
                builder.AppendLine();
                builder.AppendLine(indent + "            rawText += nextToken.Text;");
                builder.AppendLine(indent + "        }");
                builder.AppendLine();
                builder.AppendLine(indent + "        var greaterThanResult = context.Expect(MLIR.Text.TokenKind.GreaterThan, \"Expected '>' to close the enum attribute.\");");
                builder.AppendLine(indent + "        if (!greaterThanResult.IsSuccess)");
                builder.AppendLine(indent + "        {");
                builder.AppendLine(indent + "            return ParseResult<AttributeValueSyntax>.Failure(greaterThanResult.Diagnostic!);");
                builder.AppendLine(indent + "        }");
                builder.AppendLine(indent + "        rawText += greaterThanResult.Value.Text;");
                builder.AppendLine(indent + "    }");
                builder.AppendLine(indent + "    else");
                builder.AppendLine(indent + "    {");
                builder.AppendLine(indent + "        while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
                builder.AppendLine(indent + "        {");
                builder.AppendLine(indent + "            if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
                builder.AppendLine(indent + "                && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
                builder.AppendLine(indent + "            {");
                builder.AppendLine(indent + "                break;");
                builder.AppendLine(indent + "            }");
                builder.AppendLine();
                builder.AppendLine(indent + "            rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + " + nextToken.Text;");
                builder.AppendLine(indent + "        }");
                builder.AppendLine(indent + "    }");
            }
            else
            {
                builder.AppendLine(indent + "    while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
                builder.AppendLine(indent + "    {");
                builder.AppendLine(indent + "        if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
                builder.AppendLine(indent + "            && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
                builder.AppendLine(indent + "        {");
                builder.AppendLine(indent + "            break;");
                builder.AppendLine(indent + "        }");
                builder.AppendLine();
                builder.AppendLine(indent + "        rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + " + nextToken.Text;");
                builder.AppendLine(indent + "    }");
            }
        }

        builder.AppendLine();
        builder.AppendLine(indent + "    return ParseResult<AttributeValueSyntax>.Success(new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(rawText)));");
        builder.AppendLine(indent + "}");
        builder.AppendLine();
    }

    private static void EmitEnumType(StringBuilder builder, EnumModel enumModel)
    {
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("[global::System.Flags]");
        }

        builder.AppendLine("public enum " + enumTypeName + " : " + GetUnderlyingCSharpType(enumModel.Bitwidth));
        builder.AppendLine("{");
        foreach (var enumCase in enumModel.Cases)
        {
            var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
            builder.AppendLine("    " + memberName + " = " + enumCase.Value + ",");
        }

        builder.AppendLine("}");
    }

    private static void EmitEnumInfo(StringBuilder builder, EnumModel enumModel)
    {
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);
        var infoClassName = GetEnumInfoClassName(enumModel);

        builder.AppendLine("internal static class " + infoClassName);
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

        builder.AppendLine("    internal static readonly global::System.Collections.Generic.Dictionary<" + enumTypeName + ", string> ExactValueToSymbol =");
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
        builder.AppendLine();

        builder.AppendLine("    internal static readonly global::System.Collections.Generic.Dictionary<ulong, " + enumTypeName + "> IntegerToEnum =");
        builder.AppendLine("        new global::System.Collections.Generic.Dictionary<ulong, " + enumTypeName + ">()");
        builder.AppendLine("        {");
        foreach (var enumCase in enumModel.Cases)
        {
            var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
            builder.AppendLine("            { " + unchecked((ulong)enumCase.Value).ToString() + "UL, " + enumTypeName + "." + memberName + " },");
        }

        builder.AppendLine("        };");
        builder.AppendLine();

        if (enumModel.IsBitEnum)
        {
            EmitBitEnumCases(builder, enumModel, enumTypeName);
            builder.AppendLine();
        }

        builder.AppendLine("    internal static bool TryParse(string raw, out " + enumTypeName + " value)");
        builder.AppendLine("    {");
        builder.AppendLine("        raw = raw.Trim();");
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("        if (raw.Length == 0)");
            builder.AppendLine("        {");
            builder.AppendLine("            value = default;");
            builder.AppendLine("            return false;");
            builder.AppendLine("        }");
            builder.AppendLine();
            var separator = enumModel.Separator.Trim();
            builder.AppendLine("        var parts = raw.Split(new[] { " + EmitterHelpers.ToCSharpCharLiteral(separator[0]) + " }, global::System.StringSplitOptions.None);");
            builder.AppendLine("        var accumulator = (" + enumTypeName + ")0;");
            builder.AppendLine("        foreach (var part in parts)");
            builder.AppendLine("        {");
            builder.AppendLine("            var trimmed = part.Trim();");
            builder.AppendLine("            if (!SymbolToEnum.TryGetValue(trimmed, out var flag))");
            builder.AppendLine("            {");
            builder.AppendLine("                value = default;");
            builder.AppendLine("                return false;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            accumulator |= flag;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        value = accumulator;");
            builder.AppendLine("        return true;");
        }
        else
        {
            builder.AppendLine("        return SymbolToEnum.TryGetValue(raw, out value);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        builder.AppendLine("    internal static string Format(" + enumTypeName + " value)");
        builder.AppendLine("    {");
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("        if (ExactValueToSymbol.TryGetValue(value, out var exactSymbol))");
            builder.AppendLine("        {");
            builder.AppendLine("            return exactSymbol;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        var remaining = (ulong)value;");
            builder.AppendLine("        var parts = new global::System.Collections.Generic.List<string>();");
            builder.AppendLine("        foreach (var pair in PrimaryFlags)");
            builder.AppendLine("        {");
            builder.AppendLine("            var flagValue = (ulong)pair.Key;");
            builder.AppendLine("            if (flagValue != 0 && (remaining & flagValue) == flagValue)");
            builder.AppendLine("            {");
            builder.AppendLine("                parts.Add(pair.Value);");
            builder.AppendLine("                remaining &= ~flagValue;");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        if (parts.Count == 0 || remaining != 0)");
            builder.AppendLine("        {");
            builder.AppendLine("            return value.ToString();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        return string.Join(" + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + ", parts);");
        }
        else
        {
            builder.AppendLine("        return ExactValueToSymbol.TryGetValue(value, out var symbol) ? symbol : value.ToString();");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    internal static bool TryFromInteger(global::MLIR.Numerics.ApInt raw, out " + enumTypeName + " value)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (raw.IsNegative)");
        builder.AppendLine("        {");
        builder.AppendLine("            value = default;");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return IntegerToEnum.TryGetValue(raw.ToUInt64(), out value);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitBitEnumCases(StringBuilder builder, EnumModel enumModel, string enumTypeName)
    {
        builder.AppendLine("    internal static readonly global::System.Collections.Generic.KeyValuePair<" + enumTypeName + ", string>[] PrimaryFlags =");
        builder.AppendLine("    [");

        foreach (var enumCase in enumModel.Cases.Where(static c => IsSingleBitFlag(c.Value)))
        {
            var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
            builder.AppendLine("        new global::System.Collections.Generic.KeyValuePair<" + enumTypeName + ", string>(" + enumTypeName + "." + memberName + ", " + EmitterHelpers.ToCSharpStringLiteral(enumCase.Str) + "),");
        }

        builder.AppendLine("    ];");
    }

    private static bool IsSingleBitFlag(long value)
    {
        var unsignedValue = unchecked((ulong)value);
        return unsignedValue != 0 && (unsignedValue & (unsignedValue - 1)) == 0;
    }
}
