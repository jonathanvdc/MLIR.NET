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
        else if (attribute.AssemblyFormat != null && attribute.Parameters.Count > 0)
        {
            // Parametrised attribute with a declarative assembly format: emit the structured
            // syntax class, the typed attribute-value class, and the assembly format class.
            AttributeAssemblyFormatEmitter.EmitSyntaxClass(builder, attribute, className);
            builder.AppendLine();
            EmitParametrisedAttributeClass(builder, attribute, className);
            builder.AppendLine();
            AttributeAssemblyFormatEmitter.EmitAssemblyFormatClass(builder, attribute, className);
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

    /// <summary>
    /// Emits the typed <c>AttributeValue</c> subclass for a parametrised attribute whose
    /// assembly format is given by a declarative <c>assemblyFormat</c> string.
    /// </summary>
    /// <remarks>
    /// The generated class exposes:
    /// <list type="bullet">
    ///   <item>A context-based constructor that extracts each parameter from the parsed syntax.</item>
    ///   <item>A typed constructor that accepts each parameter as a plain C# value.</item>
    ///   <item>One read-only property per parameter.</item>
    /// </list>
    /// </remarks>
    private static void EmitParametrisedAttributeClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var parameters = attribute.Parameters;
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        builder.AppendLine("public sealed class " + className + " : AttributeValue");
        builder.AppendLine("{");

        // AttributeDefinition static property
        builder.AppendLine("    public static AttributeDefinition AttributeDefinition { get; } =");
        builder.AppendLine("        new AttributeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ", new " + formatClassName + "(), factory: static context => new " + className + "(context));");
        builder.AppendLine();

        // Context-based constructor
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Location)");
        builder.AppendLine("    {");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            var helperName = "Parse" + propertyName + "Param";
            builder.AppendLine("        " + propertyName + " = " + helperName + "(context.Syntax);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        // Typed constructor
        builder.Append("    public " + className + "(");
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var csharpType = AttributeAssemblyFormatEmitter.GetResolvedCSharpType(parameters[i]);
            builder.Append(csharpType + " " + EmitterHelpers.LowerFirst(parameters[i].Name));
        }

        builder.AppendLine(")");
        builder.AppendLine("        : base(null, MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("        " + propertyName + " = " + EmitterHelpers.LowerFirst(param.Name) + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        // Parameter properties
        foreach (var param in parameters)
        {
            var csharpType = AttributeAssemblyFormatEmitter.GetResolvedCSharpType(param);
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("    public " + csharpType + " " + propertyName + " { get; }");
        }

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeDefinition.Name;");
        builder.AppendLine("    public override AttributeDefinition? Definition => AttributeDefinition;");
        builder.AppendLine();

        // Private parse helpers
        foreach (var param in parameters)
        {
            EmitParseParamHelper(builder, attribute, param, syntaxClassName);
        }

        builder.AppendLine("}");
    }

    /// <summary>
    /// Emits a private helper method that extracts the typed value for <paramref name="param"/>
    /// from the enclosing attribute's syntax node.
    /// </summary>
    /// <remarks>
    /// The generated helper unwraps a <c>DialectPrefixedAttributeValueSyntax</c> shell if present,
    /// then checks whether the underlying syntax is the expected structured syntax class before
    /// applying the parameter's <c>csharpExtractor</c> expression from the ODS model.  If neither
    /// condition matches the <c>csharpDefault</c> expression is used as a fallback.
    /// </remarks>
    private static void EmitParseParamHelper(
        StringBuilder builder,
        AttributeModel attribute,
        AttrOrTypeParameterModel param,
        string syntaxClassName)
    {
        var csharpType = AttributeAssemblyFormatEmitter.GetResolvedCSharpType(param);
        var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
        var helperName = "Parse" + propertyName + "Param";

        builder.AppendLine("    private static " + csharpType + " " + helperName + "(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        // Unwrap DialectPrefixedAttributeValueSyntax if present so both parsed and reused syntax work.
        builder.AppendLine("        var effectiveSyntax = syntax is MLIR.Syntax.DialectPrefixedAttributeValueSyntax pfx ? pfx.Body : syntax;");
        builder.AppendLine("        if (effectiveSyntax is " + syntaxClassName + " structured)");
        builder.AppendLine("        {");
        var accessExpr = "structured." + propertyName + "Syntax";
        var extractExpr = BuildExtractValueExpression(param, accessExpr);
        builder.AppendLine("            return " + extractExpr + ";");
        builder.AppendLine("        }");
        builder.AppendLine();

        // Fallback when structured syntax is not available.
        var fallbackExpr = BuildFallbackExtractExpression(csharpType, param);
        builder.AppendLine("        return " + fallbackExpr + ";");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    /// <summary>
    /// Returns a C# expression that extracts a typed value from a parsed <c>AttributeValueSyntax</c>
    /// field exposed by the structured syntax class, using the parameter's <c>csharpExtractor</c>
    /// expression from the ODS model.
    /// </summary>
    private static string BuildExtractValueExpression(AttrOrTypeParameterModel param, string syntaxExpr)
    {
        if (!string.IsNullOrEmpty(param.CsharpExtractor))
        {
            return param.CsharpExtractor!.Replace("$_syntax", syntaxExpr);
        }

        // No extractor defined: pass the syntax node through unchanged.
        // This is only valid when csharpType is AttributeValueSyntax.
        return syntaxExpr;
    }

    /// <summary>
    /// Returns a fallback C# expression that produces the default value for the parameter
    /// when the syntax node is not of the expected structured type, using the parameter's
    /// <c>csharpDefault</c> expression from the ODS model.
    /// </summary>
    private static string BuildFallbackExtractExpression(string csharpType, AttrOrTypeParameterModel param)
    {
        if (!string.IsNullOrEmpty(param.CsharpDefault))
        {
            return param.CsharpDefault!;
        }

        // No default defined: return a raw empty syntax node as best-effort fallback.
        return "syntax ?? new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(string.Empty))";
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
