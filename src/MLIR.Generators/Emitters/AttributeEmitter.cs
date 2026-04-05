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

    private static void EmitEnumAttributeClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var enumModel = attribute.EnumModel!;
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        builder.AppendLine("public sealed class " + className + " : AttributeValue");
        builder.AppendLine("{");

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
        builder.AppendLine("    public " + enumTypeName + " TypedValue => Value;");
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
        builder.AppendLine(indent + "    var raw = syntax.ToString();");
        if (isBitEnum)
        {
            builder.AppendLine(indent + "    if (raw.Length >= 2 && raw[0] == '<' && raw[raw.Length - 1] == '>')");
            builder.AppendLine(indent + "    {");
            builder.AppendLine(indent + "        raw = raw.Substring(1, raw.Length - 2).Trim();");
            builder.AppendLine(indent + "    }");
        }

        EnumEmitter.EmitParseExpression(builder, enumModel, enumTypeName, "raw", indent + "    ");
        builder.AppendLine(indent + "}");
        builder.AppendLine();
    }

    private static void EmitEnumPrintHelper(StringBuilder builder, EnumModel enumModel, string enumTypeName, bool isBitEnum, string indent)
    {
        builder.AppendLine(indent + "internal string PrintEnumValue(" + enumTypeName + " value)");
        builder.AppendLine(indent + "{");
        EnumEmitter.EmitFormatExpression(builder, enumModel, "value", indent + "    ");
        builder.AppendLine(indent + "}");
        builder.AppendLine();
    }

    private static void EmitEnumAssemblyFormatClass(StringBuilder builder, string attributeClassName, string enumTypeName, EnumModel enumModel)
    {
        var formatClassName = attributeClassName + "AssemblyFormat";
        builder.AppendLine("internal sealed class " + formatClassName + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var firstToken)");
        builder.AppendLine("            && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out firstToken)");
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("            && !context.TryMatch(MLIR.Text.TokenKind.LessThan, out firstToken))");
        }
        else
        {
            builder.AppendLine(")");
        }
        builder.AppendLine("        {");
        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var rawText = firstToken.Text;");
        if (enumModel.IsBitEnum)
        {
            var sepKind = EnumEmitter.GetSeparatorTokenKind(enumModel);
            builder.AppendLine("        if (firstToken.Text == \"<\")");
            builder.AppendLine("        {");
            builder.AppendLine("            if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
            builder.AppendLine("                && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
            builder.AppendLine("            {");
            builder.AppendLine("                return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic(\"Expected an enum element.\", firstToken.Line, firstToken.Column));");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            rawText += nextToken.Text;");
            builder.AppendLine("            while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
            builder.AppendLine("            {");
            builder.AppendLine("                rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + ";");
                builder.AppendLine("                if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out nextToken)");
                builder.AppendLine("                    && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
                builder.AppendLine("                {");
                builder.AppendLine("                    return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic(\"Expected an enum element.\", firstToken.Line, firstToken.Column));");
                builder.AppendLine("                }");
            builder.AppendLine();
            builder.AppendLine("                rawText += nextToken.Text;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            var greaterThanResult = context.Expect(MLIR.Text.TokenKind.GreaterThan, \"Expected '>' to close the enum attribute.\");");
            builder.AppendLine("            if (!greaterThanResult.IsSuccess)");
            builder.AppendLine("            {");
            builder.AppendLine("                return ParseResult<AttributeValueSyntax>.Failure(greaterThanResult.Diagnostic!);");
            builder.AppendLine("            }");
            builder.AppendLine("            rawText += greaterThanResult.Value.Text;");
            builder.AppendLine("        }");
            builder.AppendLine("        else");
            builder.AppendLine("        {");
            builder.AppendLine("            while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
            builder.AppendLine("            {");
            builder.AppendLine("                if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
            builder.AppendLine("                    && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
            builder.AppendLine("                {");
            builder.AppendLine("                    break;");
            builder.AppendLine("                }");
            builder.AppendLine();
            builder.AppendLine("                rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + " + nextToken.Text;");
            builder.AppendLine("            }");
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
        builder.AppendLine("        var enumAttr = (" + attributeClassName + ")attribute;");
        builder.AppendLine("        var text = enumAttr.PrintEnumValue(enumAttr.Value);");
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("        text = \"<\" + text + \">\";");
        }

        builder.AppendLine("        return new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(text));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
