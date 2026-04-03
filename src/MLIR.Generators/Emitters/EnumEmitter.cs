namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.ODS.Model;

internal static class EnumEmitter
{
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
            builder.AppendLine("        var parts = raw.Split(" + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator.Trim()) + ", global::System.StringSplitOptions.None);");
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
        builder.AppendLine("    internal static bool TryFromInteger(global::System.Numerics.BigInteger raw, out " + enumTypeName + " value)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (raw < 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            value = default;");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return IntegerToEnum.TryGetValue((ulong)raw, out value);");
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
