namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Generates the structured <c>AttributeValueSyntax</c> subclass and the
/// <c>IAttributeAssemblyFormat</c> implementation for an <c>AttrDef</c> with a
/// declarative <c>assemblyFormat</c> string.
/// </summary>
/// <remarks>
/// <para>
/// Two classes are emitted per parametrised attribute with a declarative format:
/// </para>
/// <list type="bullet">
///   <item>
///     <c>{ClassName}Syntax</c> — a sealed subclass of <c>AttributeValueSyntax</c> that
///     stores one <c>AttributeValueSyntax</c> field per variable reference in the format.
///     Its <c>WriteTo</c> method replays the literal tokens and delegates each variable
///     slot to the stored field.
///   </item>
///   <item>
///     <c>{ClassName}AssemblyFormat</c> — a sealed implementation of
///     <c>IAttributeAssemblyFormat</c> with <c>TryParse</c>, <c>Bind</c>, and
///     <c>BuildCustomAssemblySyntax</c> methods derived from the format elements.
///   </item>
/// </list>
/// </remarks>
internal static class AttributeAssemblyFormatEmitter
{
    // -----------------------------------------------------------------------
    // Public entry points
    // -----------------------------------------------------------------------

    /// <summary>
    /// Emits the structured <c>AttributeValueSyntax</c> subclass for the given attribute.
    /// The class name is <c>{className}Syntax</c>.
    /// </summary>
    public static void EmitSyntaxClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var format = attribute.AssemblyFormat!;
        var variables = CollectVariables(format);
        var syntaxClassName = className + "Syntax";

        builder.AppendLine("public sealed class " + syntaxClassName + " : AttributeValueSyntax");
        builder.AppendLine("{");

        // Constructor
        builder.Append("    public " + syntaxClassName + "(");
        for (var i = 0; i < variables.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append("AttributeValueSyntax " + EmitterHelpers.LowerFirst(variables[i]) + "Syntax");
        }

        builder.AppendLine(")");
        builder.AppendLine("    {");
        foreach (var v in variables)
        {
            builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(v) + "Syntax = " + EmitterHelpers.LowerFirst(v) + "Syntax;");
        }

        builder.AppendLine("    }");

        if (variables.Count > 0)
        {
            builder.AppendLine();
            foreach (var v in variables)
            {
                builder.AppendLine("    public AttributeValueSyntax " + DialectGeneratorNaming.ToPascalCase(v) + "Syntax { get; }");
            }
        }

        // Location property – delegate to the first variable's syntax, or unknown when there are none.
        builder.AppendLine();
        if (variables.Count > 0)
        {
            builder.AppendLine("    public override SourceLocation Location => " + DialectGeneratorNaming.ToPascalCase(variables[0]) + "Syntax.Location;");
        }
        else
        {
            builder.AppendLine("    public override SourceLocation Location => SourceLocation.Unknown;");
        }

        // WriteTo
        builder.AppendLine();
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        EmitWriteToBody(builder, format);
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    /// <summary>
    /// Emits the <c>IAttributeAssemblyFormat</c> implementation class for the given attribute.
    /// The class name is <c>{className}AssemblyFormat</c>.
    /// </summary>
    public static void EmitAssemblyFormatClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var format = attribute.AssemblyFormat!;
        var variables = CollectVariables(format);
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        builder.AppendLine("internal sealed class " + formatClassName + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");

        // TryParse
        builder.AppendLine("    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)");
        builder.AppendLine("    {");
        EmitTryParseBody(builder, attribute, format, variables, syntaxClassName);
        builder.AppendLine("    }");
        builder.AppendLine();

        // Bind
        builder.AppendLine("    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return definition.Factory(new AttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));");
        builder.AppendLine("    }");
        builder.AppendLine();

        // BuildCustomAssemblySyntax
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        EmitBuildCustomAssemblySyntaxBody(builder, attribute, variables, className, syntaxClassName);
        builder.AppendLine("    }");

        builder.AppendLine("}");
    }

    // -----------------------------------------------------------------------
    // WriteTo body
    // -----------------------------------------------------------------------

    private static void EmitWriteToBody(StringBuilder builder, AssemblyFormatModel format)
    {
        foreach (var element in format.Elements)
        {
            switch (element)
            {
                case LiteralChunk literal:
                    EmitLiteralWriteTo(builder, literal);
                    break;

                case VariableChunk variable:
                    builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(variable.Name) + "Syntax.WriteTo(writer);");
                    break;

                // Whitespace, newline, empty: no output
            }
        }
    }

    private static void EmitLiteralWriteTo(StringBuilder builder, LiteralChunk literal)
    {
        foreach (var lit in literal.Value)
        {
            switch (lit)
            {
                case PunctuationLiteral punc:
                    builder.AppendLine("        writer.WriteToken(new SyntaxToken(" + EmitterHelpers.ToCSharpStringLiteral(GetPunctuationText(punc.TokenKind)) + "));");
                    break;

                case KeywordLiteral kw:
                    builder.AppendLine("        writer.WriteToken(new SyntaxToken(" + EmitterHelpers.ToCSharpStringLiteral(kw.Spelling) + "));");
                    break;

                case WhitespaceLiteral ws:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(ws.Spaces) + ");");
                    break;

                case NewlineLiteral _:
                    builder.AppendLine("        writer.SuggestTrivia(\"\\n\");");
                    break;

                // EmptyLiteral: no output
            }
        }
    }

    // -----------------------------------------------------------------------
    // TryParse body
    // -----------------------------------------------------------------------

    private static void EmitTryParseBody(
        StringBuilder builder,
        AttributeModel attribute,
        AssemblyFormatModel format,
        IReadOnlyList<string> variables,
        string syntaxClassName)
    {
        var elements = format.Elements;
        var isFirst = true;

        // Track which variable index is next so we can assign to the right local.
        var varIndex = 0;

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];

            switch (element)
            {
                case LiteralChunk literal:
                    EmitLiteralParse(builder, literal, isFirst);
                    // After the first literal has been consumed, subsequent ones are required.
                    if (HasSignificantLiterals(literal))
                    {
                        isFirst = false;
                    }
                    break;

                case VariableChunk variable:
                {
                    var stopTokens = FindStopTokensForVariable(elements, i);
                    var varLocalName = EmitterHelpers.LowerFirst(variable.Name) + "Syntax";
                    var paramModel = FindParameter(attribute, variable.Name);
                    EmitVariableParse(builder, variable, varLocalName, stopTokens, paramModel);
                    varIndex++;
                    isFirst = false;
                    break;
                }
            }
        }

        // Construct and return the syntax
        builder.Append("        return ParseResult<AttributeValueSyntax>.Success(new " + syntaxClassName + "(");
        for (var i = 0; i < variables.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(EmitterHelpers.LowerFirst(variables[i]) + "Syntax");
        }

        builder.AppendLine("));");
    }

    private static void EmitLiteralParse(StringBuilder builder, LiteralChunk literal, bool isFirst)
    {
        foreach (var lit in literal.Value)
        {
            switch (lit)
            {
                case PunctuationLiteral punc:
                {
                    var kindExpr = "TokenKind." + punc.TokenKind;
                    var text = GetPunctuationText(punc.TokenKind);
                    var escapedText = EmitterHelpers.ToCSharpStringLiteral(text);
                    if (isFirst)
                    {
                        // For the first literal, use TryMatch so TryParse can return NoMatch
                        // when the attribute is not present.
                        builder.AppendLine("        if (!context.TryMatch(" + kindExpr + ", out _))");
                        builder.AppendLine("        {");
                        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
                        builder.AppendLine("        }");
                        isFirst = false;
                    }
                    else
                    {
                        builder.AppendLine("        {");
                        builder.AppendLine("            var literalResult = context.Expect(" + kindExpr + ", \"Expected '" + EscapeForString(text) + "'.\");");
                        builder.AppendLine("            if (!literalResult.IsSuccess)");
                        builder.AppendLine("                return ParseResult<AttributeValueSyntax>.Failure(literalResult.Diagnostic!);");
                        builder.AppendLine("        }");
                    }

                    break;
                }

                case KeywordLiteral kw:
                {
                    var spellingExpr = EmitterHelpers.ToCSharpStringLiteral(kw.Spelling);
                    if (isFirst)
                    {
                        builder.AppendLine("        if (!context.TryMatch(TokenKind.Identifier, out var kw0) || kw0.Text != " + spellingExpr + ")");
                        builder.AppendLine("        {");
                        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
                        builder.AppendLine("        }");
                        isFirst = false;
                    }
                    else
                    {
                        builder.AppendLine("        {");
                        builder.AppendLine("            var kwResult = context.Expect(TokenKind.Identifier, \"Expected keyword '" + EscapeForString(kw.Spelling) + "'.\");");
                        builder.AppendLine("            if (!kwResult.IsSuccess)");
                        builder.AppendLine("                return ParseResult<AttributeValueSyntax>.Failure(kwResult.Diagnostic!);");
                        builder.AppendLine("        }");
                    }

                    break;
                }

                // Whitespace, newline, empty: no parse action needed
            }
        }
    }

    private static void EmitVariableParse(
        StringBuilder builder,
        VariableChunk variable,
        string varLocalName,
        IReadOnlyList<TokenKind> stopTokens,
        AttrOrTypeParameterModel? paramModel)
    {
        string parseExpr;

        if (!string.IsNullOrEmpty(paramModel?.CsharpParser))
        {
            // Custom parser from MLIRNet_AttrOrTypeParameterExtension.csharpParser:
            // substitute $_parser → context.
            parseExpr = paramModel!.CsharpParser!.Replace("$_parser", "context");
        }
        else
        {
            // Default: parse any attribute value, stopping before the inferred delimiters.
            var stopExpr = BuildStopTokensExpression(stopTokens);
            parseExpr = "context.TryParseAttributeValueSyntax(" + stopExpr + ")";
        }

        builder.AppendLine("        var " + varLocalName + "Result = " + parseExpr + ";");
        builder.AppendLine("        if (!" + varLocalName + "Result.IsSuccess)");
        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.Failure(" + varLocalName + "Result.Diagnostic!);");
        builder.AppendLine("        var " + varLocalName + " = " + varLocalName + "Result.Value;");
    }

    // -----------------------------------------------------------------------
    // BuildCustomAssemblySyntax body
    // -----------------------------------------------------------------------

    private static void EmitBuildCustomAssemblySyntaxBody(
        StringBuilder builder,
        AttributeModel attribute,
        IReadOnlyList<string> variables,
        string className,
        string syntaxClassName)
    {
        builder.AppendLine("        var attr = (" + className + ")attribute;");
        builder.AppendLine("        if (attr.Syntax is " + syntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");

        // For each variable, build the syntax from the attribute's property.
        foreach (var varName in variables)
        {
            var paramModel = FindParameter(attribute, varName);
            var propertyName = DialectGeneratorNaming.ToPascalCase(varName);
            var localSyntaxName = EmitterHelpers.LowerFirst(varName) + "Syntax";
            var buildExpr = BuildSyntaxFromPropertyExpression("attr." + propertyName, paramModel);
            builder.AppendLine("        var " + localSyntaxName + " = " + buildExpr + ";");
        }

        builder.Append("        return new " + syntaxClassName + "(");
        for (var i = 0; i < variables.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(EmitterHelpers.LowerFirst(variables[i]) + "Syntax");
        }

        builder.AppendLine(");");
    }

    /// <summary>
    /// Returns a C# expression that converts an attribute property value to
    /// an <c>AttributeValueSyntax</c> suitable for storage in the syntax class.
    /// </summary>
    private static string BuildSyntaxFromPropertyExpression(string propertyExpr, AttrOrTypeParameterModel? param)
    {
        if (!string.IsNullOrEmpty(param?.CsharpPrinter))
        {
            // Custom printer from MLIRNet_AttrOrTypeParameterExtension.csharpPrinter:
            // substitute $_self → the property expression.
            return param!.CsharpPrinter!.Replace("$_self", propertyExpr);
        }

        var csharpType = GetResolvedCSharpType(param);

        switch (csharpType)
        {
            case "string":
                return "new StringAttributeValueSyntax(new SyntaxToken(StringLiteralAttributeAssemblyFormat.Quote(" + propertyExpr + ")), " + propertyExpr + ")";

            case "global::System.Numerics.BigInteger":
                return "new IntegerAttributeValueSyntax(new SyntaxToken(" + propertyExpr + ".ToString(global::System.Globalization.CultureInfo.InvariantCulture)), " + propertyExpr + ")";

            case "double":
                return "new FloatingPointAttributeValueSyntax(new global::MLIR.Syntax.RawSyntaxText(" + propertyExpr + ".ToString(\"G\", global::System.Globalization.CultureInfo.InvariantCulture)), " + propertyExpr + ".ToString(\"G\", global::System.Globalization.CultureInfo.InvariantCulture))";

            default:
                // Unknown type: attribute stores the AttributeValueSyntax directly.
                return propertyExpr;
        }
    }

    // -----------------------------------------------------------------------
    // Parameter helper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Finds the parameter model for the given variable name, or null when not found.
    /// </summary>
    internal static AttrOrTypeParameterModel? FindParameter(AttributeModel attribute, string variableName)
    {
        foreach (var param in attribute.Parameters)
        {
            if (string.Equals(param.Name, variableName, System.StringComparison.Ordinal))
            {
                return param;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the C# type that should be used for the given parameter's typed property.
    /// Falls back to <c>"AttributeValueSyntax"</c> when no better mapping is known.
    /// </summary>
    internal static string GetResolvedCSharpType(AttrOrTypeParameterModel? param)
    {
        if (param == null)
        {
            return "AttributeValueSyntax";
        }

        if (!string.IsNullOrEmpty(param.CsharpType))
        {
            return param.CsharpType!;
        }

        // Fall back to AttributeValueSyntax for parameters without a known C# mapping.
        return "AttributeValueSyntax";
    }

    // -----------------------------------------------------------------------
    // Stop-token analysis
    // -----------------------------------------------------------------------

    /// <summary>
    /// Determines the set of <see cref="TokenKind"/> values that terminate the parse of a
    /// variable at position <paramref name="variableIndex"/> in the format element list.
    /// </summary>
    /// <remarks>
    /// Looks ahead past whitespace to the first literal chunk after the variable and
    /// collects its punctuation token kinds. Keyword literals add
    /// <see cref="TokenKind.Identifier"/> as a stop kind so the parser does not consume
    /// the keyword that closes the parameter.
    /// </remarks>
    private static IReadOnlyList<TokenKind> FindStopTokensForVariable(IReadOnlyList<Element> elements, int variableIndex)
    {
        var stopTokens = new List<TokenKind>();
        for (var i = variableIndex + 1; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element is LiteralChunk literal)
            {
                foreach (var lit in literal.Value)
                {
                    if (lit is PunctuationLiteral punc)
                    {
                        stopTokens.Add(punc.TokenKind);
                    }
                    else if (lit is KeywordLiteral)
                    {
                        stopTokens.Add(TokenKind.Identifier);
                    }

                    // WhitespaceLiteral / NewlineLiteral / EmptyLiteral: continue scanning
                }

                if (stopTokens.Count > 0)
                {
                    break;
                }
            }
            else if (element is VariableChunk || element is DirectiveChunk)
            {
                // Another variable or directive follows: stop scanning.
                break;
            }
        }

        return stopTokens;
    }

    private static string BuildStopTokensExpression(IReadOnlyList<TokenKind> stopTokens)
    {
        if (stopTokens.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>(stopTokens.Count);
        foreach (var kind in stopTokens)
        {
            parts.Add("TokenKind." + kind);
        }

        return string.Join(", ", parts);
    }

    // -----------------------------------------------------------------------
    // Utilities
    // -----------------------------------------------------------------------

    /// <summary>
    /// Collects the ordered list of variable names referenced in the format.
    /// </summary>
    private static IReadOnlyList<string> CollectVariables(AssemblyFormatModel format)
    {
        var names = new List<string>();
        foreach (var element in format.Elements)
        {
            if (element is VariableChunk variable)
            {
                names.Add(variable.Name);
            }
        }

        return names;
    }

    /// <summary>
    /// Returns true when the literal chunk contains at least one significant (non-whitespace,
    /// non-empty, non-newline) token that would be consumed during parsing.
    /// </summary>
    private static bool HasSignificantLiterals(LiteralChunk literal)
    {
        foreach (var lit in literal.Value)
        {
            if (lit is PunctuationLiteral || lit is KeywordLiteral)
            {
                return true;
            }
        }

        return false;
    }

    private static string EscapeForString(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'");
    }

    private static string GetPunctuationText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Comma => ",",
            TokenKind.LParen => "(",
            TokenKind.RParen => ")",
            TokenKind.LBracket => "[",
            TokenKind.RBracket => "]",
            TokenKind.LBrace => "{",
            TokenKind.RBrace => "}",
            TokenKind.LessThan => "<",
            TokenKind.GreaterThan => ">",
            TokenKind.Question => "?",
            TokenKind.Star => "*",
            TokenKind.Plus => "+",
            TokenKind.Minus => "-",
            TokenKind.Dot => ".",
            TokenKind.Colon => ":",
            TokenKind.Equal => "=",
            TokenKind.At => "@",
            TokenKind.Hash => "#",
            TokenKind.Arrow => "->",
            _ => kind.ToString(),
        };
    }
}
