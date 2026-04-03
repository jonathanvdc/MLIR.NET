namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal static class AttributeEmitter
{
    public static void Emit(StringBuilder builder, AttributeModel attribute)
    {
        var className = DialectGeneratorNaming.GetAttributeClassName(attribute);

        if (attribute.EnumModel != null)
        {
            EmitEnumType(builder, attribute.EnumModel);
            builder.AppendLine();
            EmitEnumAttributeClass(builder, attribute, className);
        }
        else
        {
            EmitPlainAttributeClass(builder, attribute, className);
        }
    }

    private static void EmitPlainAttributeClass(StringBuilder builder, AttributeModel attribute, string className)
    {
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

    private static void EmitEnumType(StringBuilder builder, EnumModel enumModel)
    {
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);
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
    }

    private static void EmitEnumAttributeClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var enumModel = attribute.EnumModel!;
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        builder.AppendLine("public sealed class " + className + " : AttributeValue");
        builder.AppendLine("{");

        // Symbol-to-enum mapping table
        builder.AppendLine("    private static readonly global::System.Collections.Generic.Dictionary<string, " + enumTypeName + "> SymbolToEnum =");
        builder.AppendLine("        new global::System.Collections.Generic.Dictionary<string, " + enumTypeName + ">(global::System.StringComparer.Ordinal)");
        builder.AppendLine("        {");
        foreach (var enumCase in enumModel.Cases)
        {
            var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
            builder.AppendLine("            { " + EmitterHelpers.ToCSharpStringLiteral(enumCase.Str) + ", " + enumTypeName + "." + memberName + " },");
        }

        builder.AppendLine("        };");
        builder.AppendLine();

        // Enum-to-symbol mapping table (only primary cases — distinct values in declaration order)
        builder.AppendLine("    private static readonly global::System.Collections.Generic.Dictionary<" + enumTypeName + ", string> EnumToSymbol =");
        builder.AppendLine("        new global::System.Collections.Generic.Dictionary<" + enumTypeName + ", string>()");
        builder.AppendLine("        {");
        var seenValues = new System.Collections.Generic.HashSet<long>();
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

        // AttributeDefinition
        builder.AppendLine("    public static AttributeDefinition AttributeDefinition { get; } =");
        builder.AppendLine("        new AttributeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ", new " + className + "AssemblyFormat(), factory: static context => new " + className + "(context));");
        builder.AppendLine();

        // Constructor
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Location)");
        builder.AppendLine("    {");
        builder.AppendLine("        Value = ParseEnumValue(context.Syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Typed constructor
        builder.AppendLine("    public " + className + "(" + enumTypeName + " value)");
        builder.AppendLine("        : base(null, MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        builder.AppendLine("        Value = value;");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Value property
        builder.AppendLine("    public " + enumTypeName + " Value { get; }");
        builder.AppendLine();

        // Name and Definition properties
        builder.AppendLine("    public override string? Name => AttributeDefinition.Name;");
        builder.AppendLine("    public override AttributeDefinition? Definition => AttributeDefinition;");
        builder.AppendLine();

        // ParseEnumValue helper
        EmitEnumParseHelper(builder, enumModel, enumTypeName, isBitEnum: enumModel.IsBitEnum, indent: "    ");

        // PrintEnumValue helper
        EmitEnumPrintHelper(builder, enumModel, enumTypeName, isBitEnum: enumModel.IsBitEnum, indent: "    ");

        builder.AppendLine("}");
        builder.AppendLine();

        // Assembly format class
        EmitEnumAssemblyFormatClass(builder, className, enumTypeName, enumModel);
    }

    private static void EmitEnumParseHelper(StringBuilder builder, EnumModel enumModel, string enumTypeName, bool isBitEnum, string indent)
    {
        builder.AppendLine(indent + "private " + enumTypeName + " ParseEnumValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    if (syntax == null) return default;");
        builder.AppendLine(indent + "    var raw = syntax.GetRawText().Text.Trim();");
        if (isBitEnum)
        {
            var sep = enumModel.Separator.Contains(",") ? "," : "|";
            builder.AppendLine(indent + "    var parts = raw.Split('" + sep + "');");
            builder.AppendLine(indent + "    var result = (" + enumTypeName + ")0;");
            builder.AppendLine(indent + "    foreach (var part in parts)");
            builder.AppendLine(indent + "    {");
            builder.AppendLine(indent + "        var trimmed = part.Trim();");
            builder.AppendLine(indent + "        if (SymbolToEnum.TryGetValue(trimmed, out var flag)) result |= flag;");
            builder.AppendLine(indent + "    }");
            builder.AppendLine(indent + "    return result;");
        }
        else
        {
            builder.AppendLine(indent + "    return SymbolToEnum.TryGetValue(raw, out var v) ? v : default;");
        }

        builder.AppendLine(indent + "}");
        builder.AppendLine();
    }

    private static void EmitEnumPrintHelper(StringBuilder builder, EnumModel enumModel, string enumTypeName, bool isBitEnum, string indent)
    {
        builder.AppendLine(indent + "internal string PrintEnumValue(" + enumTypeName + " value)");
        builder.AppendLine(indent + "{");
        if (isBitEnum)
        {
            var sep = enumModel.Separator.Contains(",") ? "\", \"" : "\" | \"";
            builder.AppendLine(indent + "    if (EnumToSymbol.TryGetValue(value, out var directStr)) return directStr;");
            builder.AppendLine(indent + "    var parts = new global::System.Collections.Generic.List<string>();");
            builder.AppendLine(indent + "    foreach (var pair in EnumToSymbol)");
            builder.AppendLine(indent + "    {");
            builder.AppendLine(indent + "        var flag = pair.Key;");
            builder.AppendLine(indent + "        if ((long)(object)flag != 0 && ((long)(object)(value & flag) == (long)(object)flag))");
            builder.AppendLine(indent + "        {");
            builder.AppendLine(indent + "            parts.Add(pair.Value);");
            builder.AppendLine(indent + "            value &= ~flag;");
            builder.AppendLine(indent + "        }");
            builder.AppendLine(indent + "    }");
            builder.AppendLine(indent + "    return string.Join(" + sep + ", parts);");
        }
        else
        {
            builder.AppendLine(indent + "    return EnumToSymbol.TryGetValue(value, out var s) ? s : value.ToString();");
        }

        builder.AppendLine(indent + "}");
        builder.AppendLine();
    }

    private static void EmitEnumAssemblyFormatClass(StringBuilder builder, string attributeClassName, string enumTypeName, EnumModel enumModel)
    {
        var formatClassName = attributeClassName + "AssemblyFormat";
        builder.AppendLine("internal sealed class " + formatClassName + " : IAttributeAssemblyFormat");
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
        builder.AppendLine("        var enumAttr = (" + attributeClassName + ")attribute;");
        builder.AppendLine("        var text = enumAttr.PrintEnumValue(enumAttr.Value);");
        builder.AppendLine("        return new MLIR.Syntax.Attributes.Primitives.StringAttributeValueSyntax(");
        builder.AppendLine("            new MLIR.Text.SyntaxToken(text), text);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
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
