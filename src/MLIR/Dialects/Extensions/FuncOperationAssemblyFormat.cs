namespace MLIR.Dialects.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Syntax.Types.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Runtime-backed custom assembly strategy for <c>func.func</c>.
/// </summary>
public sealed class FuncOperationAssemblyFormat : IOperationAssemblyFormat
{
    private FuncOperationAssemblyFormat()
    {
    }

    /// <summary>
    /// Gets the singleton instance of the func.func assembly format.
    /// </summary>
    public static FuncOperationAssemblyFormat Instance { get; } = new();

    /// <inheritdoc/>
    public ParseResult<OperationBodySyntax> TryParse(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        OperationParsingContext context)
    {
        if (resultTokens.Count != 0 || equalsToken.HasValue)
        {
            return ParseResult<OperationBodySyntax>.Failure(
                new Diagnostic("func.func cannot have SSA results.", SourceLocation.FromToken(nameToken).Line, SourceLocation.FromToken(nameToken).Column));
        }

        SyntaxToken? visibilityToken = null;
        if (context.IsKeyword("public"))
        {
            var visibilityResult = context.ExpectKeyword("public", "Expected 'public'.");
            if (!visibilityResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(visibilityResult.Diagnostic!);
            }

            visibilityToken = visibilityResult.Value;
        }
        else if (context.IsKeyword("private"))
        {
            var visibilityResult = context.ExpectKeyword("private", "Expected 'private'.");
            if (!visibilityResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(visibilityResult.Diagnostic!);
            }

            visibilityToken = visibilityResult.Value;
        }
        else if (context.IsKeyword("nested"))
        {
            var visibilityResult = context.ExpectKeyword("nested", "Expected 'nested'.");
            if (!visibilityResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(visibilityResult.Diagnostic!);
            }

            visibilityToken = visibilityResult.Value;
        }

        var symbolNameResult = context.TryParseRawUntilDelimiter(TokenKind.LParen);
        if (!symbolNameResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(symbolNameResult.Diagnostic!);
        }

        var lParenResult = context.Expect(TokenKind.LParen, "Expected '(' after function name.");
        if (!lParenResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(lParenResult.Diagnostic!);
        }

        var arguments = new List<FuncFunctionArgumentSyntax>();
        var argumentCommas = new List<SyntaxToken>();
        while (!context.TryMatch(TokenKind.RParen, out var _))
        {
            var argumentResult = TryParseArgument(context);
            if (!argumentResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(argumentResult.Diagnostic!);
            }

            arguments.Add(argumentResult.Value);

            if (context.TryMatch(TokenKind.Comma, out var commaToken))
            {
                argumentCommas.Add(commaToken);
                continue;
            }

            var closingParenResult = context.Expect(TokenKind.RParen, "Expected ')' after function arguments.");
            if (!closingParenResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(closingParenResult.Diagnostic!);
            }

            break;
        }

        SyntaxToken? arrowToken = null;
        DelimitedSyntaxList<FuncFunctionResultSyntax>? resultTypes = null;
        if (context.TryMatch(TokenKind.Arrow, out var parsedArrowToken))
        {
            arrowToken = parsedArrowToken;

            if (context.TryMatch(TokenKind.LParen, out var _))
            {
                var results = new List<FuncFunctionResultSyntax>();
                var resultCommas = new List<SyntaxToken>();

                while (!context.TryMatch(TokenKind.RParen, out var _))
                {
                    var resultSyntaxResult = TryParseResult(context);
                    if (!resultSyntaxResult.IsSuccess)
                    {
                        return ParseResult<OperationBodySyntax>.Failure(resultSyntaxResult.Diagnostic!);
                    }

                    results.Add(resultSyntaxResult.Value);

                    if (context.TryMatch(TokenKind.Comma, out var commaToken))
                    {
                        resultCommas.Add(commaToken);
                        continue;
                    }

                    var closingParenResult = context.Expect(TokenKind.RParen, "Expected ')' after function results.");
                    if (!closingParenResult.IsSuccess)
                    {
                        return ParseResult<OperationBodySyntax>.Failure(closingParenResult.Diagnostic!);
                    }

                    break;
                }

                resultTypes = new DelimitedSyntaxList<FuncFunctionResultSyntax>(new SyntaxToken("("), results, resultCommas, new SyntaxToken(")"));
            }
            else
            {
                var resultType = context.TryParseTypeSyntax();
                if (!resultType.IsSuccess)
                {
                    return ParseResult<OperationBodySyntax>.Failure(resultType.Diagnostic!);
                }

                var singleResult = new FuncFunctionResultSyntax(resultType.Value, new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null));
                resultTypes = new DelimitedSyntaxList<FuncFunctionResultSyntax>(null, [singleResult], [], null);
            }
        }

        SyntaxToken? attributesKeyword = null;
        DelimitedSyntaxList<NamedAttributeSyntax> attributes = new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null);
        if (context.IsKeyword("attributes"))
        {
            var keywordResult = context.ExpectKeyword("attributes", "Expected 'attributes'.");
            if (!keywordResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(keywordResult.Diagnostic!);
            }

            attributesKeyword = keywordResult.Value;
            var attrDictResult = context.TryParseAttrDict();
            if (!attrDictResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(attrDictResult.Diagnostic!);
            }

            if (!attrDictResult.Value.OpenToken.HasValue)
            {
                return ParseResult<OperationBodySyntax>.Failure(
                    new Diagnostic("Expected '{' after 'attributes'.", keywordResult.Value.Line, keywordResult.Value.Column));
            }

            attributes = attrDictResult.Value;
        }

        RegionSyntax? bodyRegion = null;
        if (context.Is(TokenKind.LBrace))
        {
            var bodyRegionResult = context.TryParseRegion();
            if (bodyRegionResult.IsSuccess)
            {
                bodyRegion = bodyRegionResult.Value;
            }
            else if (bodyRegionResult.Diagnostic is not null)
            {
                return ParseResult<OperationBodySyntax>.Failure(bodyRegionResult.Diagnostic);
            }
        }

        return ParseResult<OperationBodySyntax>.Success(new FuncOperationBodySyntax(
            visibilityToken,
            symbolNameResult.Value,
            lParenResult.Value,
            new DelimitedSyntaxList<FuncFunctionArgumentSyntax>(new SyntaxToken("("), arguments, argumentCommas, new SyntaxToken(")")),
            arrowToken,
            resultTypes,
            attributesKeyword,
            attributes,
            bodyRegion));
    }

    /// <inheritdoc/>
    public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
    {
        if (syntax.Body is not FuncOperationBodySyntax body)
        {
            binder.Report(new AssemblyDiagnostic(
                syntax.Location,
                "Expected a FuncOperationBodySyntax but found " + syntax.Body.GetType().Name + "."));
            return new UninterpretedOperation(syntax, definition.Name);
        }

        var functionAttributes = new List<NamedAttribute>
        {
            binder.BindNamedAttribute(CreateStringAttribute("sym_name", NormalizeSymbolName(body.SymbolName.Text)), definition),
        };

        if (body.VisibilityToken.HasValue)
        {
            functionAttributes.Add(binder.BindNamedAttribute(
                CreateStringAttribute("sym_visibility", body.VisibilityToken.Value.Text),
                definition));
        }

        foreach (var attribute in body.Attributes.Items)
        {
            functionAttributes.Add(binder.BindNamedAttribute(attribute, definition));
        }

        var inputTypeTexts = body.Arguments.Items.Select(static argument => argument.Type.GetRawText().Text);
        var resultTypeText = body.ResultTypes != null
            ? body.ResultTypes.Items.Count switch
            {
                0 => "()",
                1 => body.ResultTypes.Items[0].Type.GetRawText().Text,
                _ => "(" + string.Join(", ", body.ResultTypes.Items.Select(static result => result.Type.GetRawText().Text)) + ")",
            }
            : "()";
        var functionTypeSyntax = Parser.ParseType(
            "(" + string.Join(", ", inputTypeTexts) + ") -> " + resultTypeText);

        var regions = new List<Region>();
        if (body.BodyRegion != null)
        {
            regions.Add(binder.BindRegion(body.BodyRegion));
        }
        else
        {
            regions.Add(new Region(null, []));
        }

        return definition.Factory(new OperationConstructionContext(
            syntax,
            definition.Name,
            definition,
            regions,
            new NamedAttributeCollection(functionAttributes),
            binder.BindTypeReference(functionTypeSyntax),
            [],
            new Value?[] { null },
            []));
    }

    /// <inheritdoc/>
    public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
    {
        if (operation.Syntax?.Body is FuncOperationBodySyntax)
        {
            return operation.Syntax;
        }

        if (operation.TypeSignatureReference?.Syntax is not FunctionTypeSyntax functionTypeSyntax)
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        if (!operation.HasAttribute("sym_name"))
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        var symName = GetStringValue(operation.GetAttribute("sym_name").Value);
        if (symName == null)
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        SyntaxToken? visibilityToken = null;
        if (operation.HasAttribute("sym_visibility"))
        {
            var visibilityText = GetStringValue(operation.GetAttribute("sym_visibility").Value);
            if (visibilityText != null)
            {
                visibilityToken = new SyntaxToken(visibilityText);
            }
        }

        var arguments = new List<FuncFunctionArgumentSyntax>(functionTypeSyntax.InputTypes.Items.Count);
        var argumentCommas = new List<SyntaxToken>(Math.Max(0, functionTypeSyntax.InputTypes.Items.Count - 1));
        var blockArguments = operation.Regions.FirstOrDefault()?.Blocks.FirstOrDefault()?.Arguments;
        for (var i = 0; i < functionTypeSyntax.InputTypes.Items.Count; i++)
        {
            if (i > 0)
            {
                argumentCommas.Add(new SyntaxToken(","));
            }

            var name = blockArguments != null && i < blockArguments.Count
                ? blockArguments[i].Name
                : "%arg" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);

            arguments.Add(new FuncFunctionArgumentSyntax(
                new SyntaxToken(name),
                new SyntaxToken(":"),
                functionTypeSyntax.InputTypes.Items[i],
                new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null)));
        }

        DelimitedSyntaxList<FuncFunctionResultSyntax>? resultTypes = null;
        if (functionTypeSyntax.HasDelimitedResults)
        {
            var results = new List<FuncFunctionResultSyntax>(functionTypeSyntax.ResultTypes.Items.Count);
            var resultCommas = new List<SyntaxToken>(Math.Max(0, functionTypeSyntax.ResultTypes.Items.Count - 1));
            for (var i = 0; i < functionTypeSyntax.ResultTypes.Items.Count; i++)
            {
                if (i > 0)
                {
                    resultCommas.Add(new SyntaxToken(","));
                }

                results.Add(new FuncFunctionResultSyntax(
                    functionTypeSyntax.ResultTypes.Items[i],
                    new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null)));
            }

            resultTypes = new DelimitedSyntaxList<FuncFunctionResultSyntax>(
                new SyntaxToken("("),
                results,
                resultCommas,
                new SyntaxToken(")"));
        }
        else if (functionTypeSyntax.ResultType != null)
        {
            resultTypes = new DelimitedSyntaxList<FuncFunctionResultSyntax>(
                null,
                [new FuncFunctionResultSyntax(functionTypeSyntax.ResultType, new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null))],
                [],
                null);
        }

        var explicitAttributes = context.BuildAttrDict(
            new NamedAttributeCollection(operation.Attributes.Where(static attribute =>
                attribute.Name != "sym_name" && attribute.Name != "sym_visibility")));

        var bodyRegion = operation.Regions.Count > 0 && operation.Regions[0].Blocks.Count > 0
            ? context.TransformRegion(operation.Regions[0])
            : null;

        return context.RewriteOperation(
            operation,
            new FuncOperationBodySyntax(
                visibilityToken,
                new RawSyntaxText("@" + symName),
                new SyntaxToken("("),
                new DelimitedSyntaxList<FuncFunctionArgumentSyntax>(new SyntaxToken("("), arguments, argumentCommas, new SyntaxToken(")")),
                resultTypes != null ? new SyntaxToken("->") : null,
                resultTypes,
                explicitAttributes.OpenToken.HasValue ? new SyntaxToken("attributes") : null,
                explicitAttributes,
                bodyRegion),
            new SyntaxToken(operation.Name));
    }

    private static string NormalizeSymbolName(string text)
    {
        return text.StartsWith("@", StringComparison.Ordinal) ? text.Substring(1) : text;
    }

    private static string? GetStringValue(AttributeValue? value)
    {
        if (value?.Syntax is StringAttributeValueSyntax stringSyntax)
        {
            return stringSyntax.Value;
        }

        var rawText = value?.Syntax?.GetRawText().Text;
        if (rawText == null)
        {
            return null;
        }

        return rawText.Length >= 2 && rawText[0] == '"' && rawText[rawText.Length - 1] == '"'
            ? rawText.Substring(1, rawText.Length - 2)
            : rawText;
    }

    private static ParseResult<FuncFunctionArgumentSyntax> TryParseArgument(OperationParsingContext context)
    {
        var nameResult = context.TryParseSsaToken();
        if (!nameResult.IsSuccess)
        {
            return ParseResult<FuncFunctionArgumentSyntax>.Failure(nameResult.Diagnostic!);
        }

        var colonResult = context.Expect(TokenKind.Colon, "Expected ':' after function argument name.");
        if (!colonResult.IsSuccess)
        {
            return ParseResult<FuncFunctionArgumentSyntax>.Failure(colonResult.Diagnostic!);
        }

        var typeResult = context.TryParseTypeSyntax(TokenKind.LBrace, TokenKind.Comma, TokenKind.RParen);
        if (!typeResult.IsSuccess)
        {
            return ParseResult<FuncFunctionArgumentSyntax>.Failure(typeResult.Diagnostic!);
        }

        var attrDictResult = context.TryParseAttrDict();
        if (!attrDictResult.IsSuccess)
        {
            return ParseResult<FuncFunctionArgumentSyntax>.Failure(attrDictResult.Diagnostic!);
        }

        return ParseResult<FuncFunctionArgumentSyntax>.Success(
            new FuncFunctionArgumentSyntax(
                nameResult.Value,
                colonResult.Value,
                typeResult.Value,
                attrDictResult.Value));
    }

    private static ParseResult<FuncFunctionResultSyntax> TryParseResult(OperationParsingContext context)
    {
        var typeResult = context.TryParseTypeSyntax(TokenKind.LBrace, TokenKind.Comma, TokenKind.RParen);
        if (!typeResult.IsSuccess)
        {
            return ParseResult<FuncFunctionResultSyntax>.Failure(typeResult.Diagnostic!);
        }

        var attrDictResult = context.TryParseAttrDict();
        if (!attrDictResult.IsSuccess)
        {
            return ParseResult<FuncFunctionResultSyntax>.Failure(attrDictResult.Diagnostic!);
        }

        return ParseResult<FuncFunctionResultSyntax>.Success(
            new FuncFunctionResultSyntax(typeResult.Value, attrDictResult.Value));
    }

    private static NamedAttributeSyntax CreateStringAttribute(string name, string value)
    {
        var literal = new SyntaxToken("\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
        return new NamedAttributeSyntax(
            new SyntaxToken(name),
            new SyntaxToken("="),
            new StringAttributeValueSyntax(literal, value));
    }
}

/// <summary>
/// Concrete syntax node for func.func custom assembly.
/// </summary>
public sealed class FuncOperationBodySyntax : OperationBodySyntax
{
    public FuncOperationBodySyntax(
        SyntaxToken? visibilityToken,
        RawSyntaxText symbolName,
        SyntaxToken lParenToken,
        DelimitedSyntaxList<FuncFunctionArgumentSyntax> arguments,
        SyntaxToken? arrowToken,
        DelimitedSyntaxList<FuncFunctionResultSyntax>? resultTypes,
        SyntaxToken? attributesKeyword,
        DelimitedSyntaxList<NamedAttributeSyntax> attributes,
        RegionSyntax? bodyRegion)
    {
        VisibilityToken = visibilityToken;
        SymbolName = symbolName;
        LParenToken = lParenToken;
        Arguments = arguments;
        ArrowToken = arrowToken;
        ResultTypes = resultTypes;
        AttributesKeyword = attributesKeyword;
        Attributes = attributes;
        BodyRegion = bodyRegion;
    }

    public SyntaxToken? VisibilityToken { get; }

    public RawSyntaxText SymbolName { get; }

    public SyntaxToken LParenToken { get; }

    public DelimitedSyntaxList<FuncFunctionArgumentSyntax> Arguments { get; }

    public SyntaxToken? ArrowToken { get; }

    public DelimitedSyntaxList<FuncFunctionResultSyntax>? ResultTypes { get; }

    public SyntaxToken? AttributesKeyword { get; }

    public DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; }

    public RegionSyntax? BodyRegion { get; }

    public override void WriteTo(SyntaxWriter writer, int indentLevel)
    {
        if (VisibilityToken.HasValue)
        {
            writer.WriteToken(VisibilityToken.Value, " ");
        }

        writer.WriteRaw(SymbolName, " ");
        writer.WriteToken(LParenToken, string.Empty);
        WriteArgumentList(writer);

        if (ArrowToken.HasValue && ResultTypes != null)
        {
            writer.WriteToken(ArrowToken.Value, " ");
            WriteResultList(writer);
        }

        if (AttributesKeyword.HasValue)
        {
            writer.WriteToken(AttributesKeyword.Value, " ");
            writer.WriteDelimitedList(Attributes, " ");
        }

        if (BodyRegion != null)
        {
            writer.Write(" ");
            BodyRegion.WriteTo(writer, indentLevel);
        }
    }

    private void WriteArgumentList(SyntaxWriter writer)
    {
        for (var i = 0; i < Arguments.Items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(Arguments.SeparatorTokens[i - 1], string.Empty);
            }

            Arguments.Items[i].WriteTo(writer, i > 0 ? " " : string.Empty);
        }

        writer.WriteToken(Arguments.CloseToken!.Value, string.Empty);
    }

    private void WriteResultList(SyntaxWriter writer)
    {
        if (ResultTypes!.OpenToken.HasValue)
        {
            writer.WriteToken(ResultTypes.OpenToken.Value, " ");
        }

        for (var i = 0; i < ResultTypes.Items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(ResultTypes.SeparatorTokens[i - 1], string.Empty);
            }

            ResultTypes.Items[i].WriteTo(writer, i > 0 ? " " : string.Empty);
        }

        if (ResultTypes.CloseToken.HasValue)
        {
            writer.WriteToken(ResultTypes.CloseToken.Value, string.Empty);
        }
    }
}

/// <summary>
/// Concrete syntax node for a func.func argument.
/// </summary>
public sealed class FuncFunctionArgumentSyntax
{
    public FuncFunctionArgumentSyntax(
        SyntaxToken name,
        SyntaxToken colonToken,
        TypeSyntax type,
        DelimitedSyntaxList<NamedAttributeSyntax> attrDict)
    {
        Name = name;
        ColonToken = colonToken;
        Type = type;
        AttrDict = attrDict;
    }

    public SyntaxToken Name { get; }

    public SyntaxToken ColonToken { get; }

    public TypeSyntax Type { get; }

    public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }

    public void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(Name, defaultLeadingTrivia);
        writer.WriteToken(ColonToken, " ");
        Type.WriteTo(writer, " ");
        if (AttrDict.OpenToken.HasValue)
        {
            writer.Write(" ");
            writer.WriteDelimitedList(AttrDict, string.Empty);
        }
    }
}

/// <summary>
/// Concrete syntax node for a func.func result.
/// </summary>
public sealed class FuncFunctionResultSyntax
{
    public FuncFunctionResultSyntax(
        TypeSyntax type,
        DelimitedSyntaxList<NamedAttributeSyntax> attrDict)
    {
        Type = type;
        AttrDict = attrDict;
    }

    public TypeSyntax Type { get; }

    public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }

    public void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
    {
        Type.WriteTo(writer, defaultLeadingTrivia);
        if (AttrDict.OpenToken.HasValue)
        {
            writer.Write(" ");
            writer.WriteDelimitedList(AttrDict, string.Empty);
        }
    }
}
