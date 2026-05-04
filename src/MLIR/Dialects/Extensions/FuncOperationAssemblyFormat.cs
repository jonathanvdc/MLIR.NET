namespace MLIR.Dialects.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Syntax.Types.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Runtime-backed custom assembly strategy for <c>func.func</c>.
/// </summary>
public sealed class FuncOperationAssemblyFormat : BodyOnlyOperationAssemblyFormat
{
    private readonly OperationDefinition definition;

    /// <summary>
    /// Initializes a new <c>func.func</c> assembly format bound to the operation definition being registered.
    /// </summary>
    public FuncOperationAssemblyFormat(OperationDefinition definition)
    {
        this.definition = definition;
    }

    /// <inheritdoc/>
    protected override ParseResult<OperationBodySyntax> TryParseBody(
            OperationParseHeader header,
            OperationParsingContext context)
    {
        if (header.ResultList.Count != 0 || header.EqualsToken.HasValue)
        {
            return ParseResult<OperationBodySyntax>.Failure(
                new Diagnostic("func.func cannot have SSA results.", header.NameToken.Location));
        }

        Token? visibilityToken = null;
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
        var argumentCommas = new List<Token>();
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

        Token? arrowToken = null;
        DelimitedSyntaxList<FuncFunctionResultSyntax>? resultTypes = null;
        if (context.TryMatch(TokenKind.Arrow, out var parsedArrowToken))
        {
            arrowToken = parsedArrowToken;

            if (context.TryMatch(TokenKind.LParen, out var _))
            {
                var results = new List<FuncFunctionResultSyntax>();
                var resultCommas = new List<Token>();

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

                resultTypes = new DelimitedSyntaxList<FuncFunctionResultSyntax>(TokenFactory.LParen(), results, resultCommas, TokenFactory.RParen());
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

        Token? attributesKeyword = null;
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
                    new Diagnostic("Expected '{' after 'attributes'.", keywordResult.Value.Location));
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
            new DelimitedSyntaxList<FuncFunctionArgumentSyntax>(TokenFactory.LParen(), arguments, argumentCommas, TokenFactory.RParen()),
            arrowToken,
            resultTypes,
            attributesKeyword,
            attributes,
            bodyRegion));
    }

    /// <inheritdoc/>
    public override Operation Bind(OperationSyntax syntax, Binder binder)
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

        var inputTypeTexts = body.Arguments.Items.Select(static argument => argument.Type.ToString());
        var resultTypeText = body.ResultTypes != null
            ? body.ResultTypes.OpenToken.HasValue || body.ResultTypes.Items.Count != 1
                ? "(" + string.Join(", ", body.ResultTypes.Items.Select(static result => result.Type.ToString())) + ")"
                : body.ResultTypes.Items[0].Type.ToString()
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
    public override OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
    {
        var sourceBody = operation.Syntax?.Body as FuncOperationBodySyntax;
        if (operation.TypeSignatureReference is null || context.BuildTypeSyntax(operation.TypeSignatureReference) is not FunctionTypeSyntax functionTypeSyntax)
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

        Token? visibilityToken = null;
        if (operation.HasAttribute("sym_visibility"))
        {
            var visibilityText = GetStringValue(operation.GetAttribute("sym_visibility").Value);
            if (visibilityText != null)
            {
                visibilityToken = TokenFactory.Identifier(visibilityText);
            }
        }

        var arguments = new List<FuncFunctionArgumentSyntax>(functionTypeSyntax.InputTypes.Items.Count);
        var argumentCommas = new List<Token>(Math.Max(0, functionTypeSyntax.InputTypes.Items.Count - 1));
        var blockArguments = operation.Regions.FirstOrDefault()?.Blocks.FirstOrDefault()?.Arguments;
        for (var i = 0; i < functionTypeSyntax.InputTypes.Items.Count; i++)
        {
            if (i > 0)
            {
                argumentCommas.Add(TokenFactory.Comma());
            }

            var nameToken = sourceBody != null && i < sourceBody.Arguments.Items.Count
                ? context.NormalizeToken(sourceBody.Arguments.Items[i].Name)
                : blockArguments != null && i < blockArguments.Count
                    ? context.NormalizeToken(blockArguments[i].Syntax.NameToken)
                    : TokenFactory.SsaName("%arg" + i.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var attrDict = sourceBody != null && i < sourceBody.Arguments.Items.Count
                ? sourceBody.Arguments.Items[i].AttrDict
                : new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null);

            arguments.Add(new FuncFunctionArgumentSyntax(
                nameToken,
                TokenFactory.Colon(),
                functionTypeSyntax.InputTypes.Items[i],
                attrDict));
        }

        DelimitedSyntaxList<FuncFunctionResultSyntax>? resultTypes = null;
        if (functionTypeSyntax.HasDelimitedResults)
        {
            var results = new List<FuncFunctionResultSyntax>(functionTypeSyntax.ResultTypes.Items.Count);
            var resultCommas = new List<Token>(Math.Max(0, functionTypeSyntax.ResultTypes.Items.Count - 1));
            for (var i = 0; i < functionTypeSyntax.ResultTypes.Items.Count; i++)
            {
                if (i > 0)
                {
                    resultCommas.Add(TokenFactory.Comma());
                }

                var attrDict = sourceBody != null
                    && sourceBody.ResultTypes != null
                    && i < sourceBody.ResultTypes.Items.Count
                    ? sourceBody.ResultTypes.Items[i].AttrDict
                    : new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null);

                results.Add(new FuncFunctionResultSyntax(
                    functionTypeSyntax.ResultTypes.Items[i],
                    attrDict));
            }

            resultTypes = new DelimitedSyntaxList<FuncFunctionResultSyntax>(
                TokenFactory.LParen(),
                results,
                resultCommas,
                TokenFactory.RParen());
        }
        else if (functionTypeSyntax.ResultType != null)
        {
            var attrDict = sourceBody != null && sourceBody.ResultTypes != null && sourceBody.ResultTypes.Items.Count > 0
                ? sourceBody.ResultTypes.Items[0].AttrDict
                : new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null);

            resultTypes = new DelimitedSyntaxList<FuncFunctionResultSyntax>(
                null,
                [new FuncFunctionResultSyntax(functionTypeSyntax.ResultType, attrDict)],
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
                TokenFactory.LParen(),
                new DelimitedSyntaxList<FuncFunctionArgumentSyntax>(TokenFactory.LParen(), arguments, argumentCommas, TokenFactory.RParen()),
                resultTypes != null ? TokenFactory.Arrow() : null,
                resultTypes,
                explicitAttributes.OpenToken.HasValue ? TokenFactory.Identifier("attributes") : null,
                explicitAttributes,
                bodyRegion),
            TokenFactory.Identifier(operation.Name));
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

        var rawText = value?.Syntax?.ToString();
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
        var literal = TokenFactory.StringLiteral("\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
        return new NamedAttributeSyntax(
            TokenFactory.Identifier(name),
            TokenFactory.Equal(),
            new StringAttributeValueSyntax(literal, value));
    }

}

/// <summary>
/// Concrete syntax node for func.func custom assembly.
/// </summary>
public sealed class FuncOperationBodySyntax : OperationBodySyntax
{
    /// <summary>
    /// Initializes a new <see cref="FuncOperationBodySyntax"/> instance.
    /// </summary>
    /// <param name="visibilityToken">The optional visibility keyword.</param>
    /// <param name="symbolName">The function symbol name as written in source.</param>
    /// <param name="lParenToken">The opening parenthesis token for the argument list.</param>
    /// <param name="arguments">The parsed function arguments.</param>
    /// <param name="arrowToken">The optional arrow token before the result list.</param>
    /// <param name="resultTypes">The optional result type list.</param>
    /// <param name="attributesKeyword">The optional <c>attributes</c> keyword.</param>
    /// <param name="attributes">The parsed attribute dictionary.</param>
    /// <param name="bodyRegion">The optional function body region.</param>
    public FuncOperationBodySyntax(
        Token? visibilityToken,
        RawSyntaxText symbolName,
        Token lParenToken,
        DelimitedSyntaxList<FuncFunctionArgumentSyntax> arguments,
        Token? arrowToken,
        DelimitedSyntaxList<FuncFunctionResultSyntax>? resultTypes,
        Token? attributesKeyword,
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

    /// <summary>
    /// Gets the optional visibility keyword token.
    /// </summary>
    public Token? VisibilityToken { get; }

    /// <summary>
    /// Gets the raw symbol name token sequence.
    /// </summary>
    public RawSyntaxText SymbolName { get; }

    /// <summary>
    /// Gets the opening parenthesis token for the argument list.
    /// </summary>
    public Token LParenToken { get; }

    /// <summary>
    /// Gets the parsed function arguments.
    /// </summary>
    public DelimitedSyntaxList<FuncFunctionArgumentSyntax> Arguments { get; }

    /// <summary>
    /// Gets the optional arrow token introducing the result list.
    /// </summary>
    public Token? ArrowToken { get; }

    /// <summary>
    /// Gets the optional function result list.
    /// </summary>
    public DelimitedSyntaxList<FuncFunctionResultSyntax>? ResultTypes { get; }

    /// <summary>
    /// Gets the optional <c>attributes</c> keyword token.
    /// </summary>
    public Token? AttributesKeyword { get; }

    /// <summary>
    /// Gets the parsed attribute dictionary.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; }

    /// <summary>
    /// Gets the optional function body region.
    /// </summary>
    public RegionSyntax? BodyRegion { get; }

    /// <inheritdoc/>
    public override SourceLocation Location
    {
        get
        {
            // Merge all source-backed tokens and subtrees in parse order to cover the full
            // func.func header from the optional visibility keyword through to the body region
            // (or to the last result/attribute when no body is present).
            var result = SourceLocation.Unknown;
            if (VisibilityToken.HasValue)
                result = SourceLocation.Merge(result, VisibilityToken.Value.Location);
            result = SourceLocation.Merge(result, SymbolName.Location);
            result = SourceLocation.Merge(result, LParenToken.Location);
            if (Arguments.CloseToken.HasValue)
                result = SourceLocation.Merge(result, Arguments.CloseToken.Value.Location);
            if (ArrowToken.HasValue)
                result = SourceLocation.Merge(result, ArrowToken.Value.Location);
            if (ResultTypes != null)
            {
                if (ResultTypes.CloseToken.HasValue)
                    result = SourceLocation.Merge(result, ResultTypes.CloseToken.Value.Location);
                else if (ResultTypes.Items.Count > 0)
                    result = SourceLocation.Merge(result, ResultTypes.Items[ResultTypes.Items.Count - 1].Type.Location);
            }

            if (AttributesKeyword.HasValue)
                result = SourceLocation.Merge(result, AttributesKeyword.Value.Location);
            if (Attributes.CloseToken.HasValue)
                result = SourceLocation.Merge(result, Attributes.CloseToken.Value.Location);
            if (BodyRegion != null)
                result = SourceLocation.Merge(result, BodyRegion.Location);
            return result;
        }
    }

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        if (VisibilityToken.HasValue)
        {
            writer.WriteToken(VisibilityToken.Value, " ");
        }

        writer.WriteRaw(SymbolName, " ");
        writer.WriteToken(LParenToken);
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
            writer.WriteRegion(BodyRegion);
        }
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new FuncOperationBodySyntax(
            rewriter.VisitToken(VisibilityToken),
            rewriter.VisitRawText(SymbolName),
            rewriter.VisitToken(LParenToken),
            RewriteArguments(rewriter),
            rewriter.VisitToken(ArrowToken),
            RewriteResultTypes(rewriter),
            rewriter.VisitToken(AttributesKeyword),
            rewriter.VisitDelimitedList(Attributes),
            BodyRegion != null ? (RegionSyntax)rewriter.Visit(BodyRegion) : null);
    }

    private DelimitedSyntaxList<FuncFunctionArgumentSyntax> RewriteArguments(SyntaxRewriter rewriter)
    {
        var items = new List<FuncFunctionArgumentSyntax>(Arguments.Items.Count);
        foreach (var argument in Arguments.Items)
        {
            items.Add(new FuncFunctionArgumentSyntax(
                rewriter.VisitToken(argument.Name),
                rewriter.VisitToken(argument.ColonToken),
                (TypeSyntax)rewriter.Visit(argument.Type),
                rewriter.VisitDelimitedList(argument.AttrDict)));
        }

        return new DelimitedSyntaxList<FuncFunctionArgumentSyntax>(
            rewriter.VisitToken(Arguments.OpenToken),
            items,
            rewriter.VisitTokenList(Arguments.SeparatorTokens),
            rewriter.VisitToken(Arguments.CloseToken));
    }

    private DelimitedSyntaxList<FuncFunctionResultSyntax>? RewriteResultTypes(SyntaxRewriter rewriter)
    {
        if (ResultTypes == null)
        {
            return null;
        }

        var items = new List<FuncFunctionResultSyntax>(ResultTypes.Items.Count);
        foreach (var result in ResultTypes.Items)
        {
            items.Add(new FuncFunctionResultSyntax(
                (TypeSyntax)rewriter.Visit(result.Type),
                rewriter.VisitDelimitedList(result.AttrDict)));
        }

        return new DelimitedSyntaxList<FuncFunctionResultSyntax>(
            rewriter.VisitToken(ResultTypes.OpenToken),
            items,
            rewriter.VisitTokenList(ResultTypes.SeparatorTokens),
            rewriter.VisitToken(ResultTypes.CloseToken));
    }

    private void WriteArgumentList(SyntaxWriter writer)
    {
        for (var i = 0; i < Arguments.Items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(Arguments.SeparatorTokens[i - 1]);
                writer.SuggestTrivia(" ");
            }

            Arguments.Items[i].WriteTo(writer);
        }

        writer.WriteToken(Arguments.CloseToken!.Value);
    }

    private void WriteResultList(SyntaxWriter writer)
    {
        if (ResultTypes!.OpenToken.HasValue)
        {
            writer.WriteToken(ResultTypes.OpenToken.Value, " ");
        }
        else if (ResultTypes.Items.Count > 0)
        {
            writer.SuggestTrivia(" ");
        }

        for (var i = 0; i < ResultTypes.Items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(ResultTypes.SeparatorTokens[i - 1]);
                writer.SuggestTrivia(" ");
            }

            ResultTypes.Items[i].WriteTo(writer);
        }

        if (ResultTypes.CloseToken.HasValue)
        {
            writer.WriteToken(ResultTypes.CloseToken.Value);
        }
    }
}

/// <summary>
/// Concrete syntax node for a func.func argument.
/// </summary>
public sealed class FuncFunctionArgumentSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="FuncFunctionArgumentSyntax"/> instance.
    /// </summary>
    /// <param name="name">The argument name token.</param>
    /// <param name="colonToken">The colon token separating the name and type.</param>
    /// <param name="type">The argument type syntax.</param>
    /// <param name="attrDict">The optional argument attribute dictionary.</param>
    public FuncFunctionArgumentSyntax(
        Token name,
        Token colonToken,
        TypeSyntax type,
        DelimitedSyntaxList<NamedAttributeSyntax> attrDict)
    {
        Name = name;
        ColonToken = colonToken;
        Type = type;
        AttrDict = attrDict;
    }

    /// <summary>
    /// Gets the argument name token.
    /// </summary>
    public Token Name { get; }

    /// <summary>
    /// Gets the colon token separating the argument name from the type.
    /// </summary>
    public Token ColonToken { get; }

    /// <summary>
    /// Gets the argument type syntax.
    /// </summary>
    public TypeSyntax Type { get; }

    /// <summary>
    /// Gets the optional attribute dictionary attached to the argument.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }

    /// <inheritdoc/>
    public override SourceLocation Location => Name.Location;

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new FuncFunctionArgumentSyntax(
            rewriter.VisitToken(Name),
            rewriter.VisitToken(ColonToken),
            rewriter.Visit(Type),
            rewriter.VisitDelimitedList(AttrDict));
    }

    /// <summary>
    /// Writes the argument syntax to the provided writer using the current pending suggested trivia
    /// for leading whitespace.
    /// </summary>
    /// <param name="writer">The writer to receive the rendered syntax.</param>
    public override void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(Name);
        writer.WriteToken(ColonToken, " ");
        writer.SuggestTrivia(" ");
        Type.WriteTo(writer);
        if (AttrDict.OpenToken.HasValue)
        {
            writer.Write(" ");
            writer.WriteDelimitedList(AttrDict);
        }
    }
}

/// <summary>
/// Concrete syntax node for a func.func result.
/// </summary>
public sealed class FuncFunctionResultSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="FuncFunctionResultSyntax"/> instance.
    /// </summary>
    /// <param name="type">The result type syntax.</param>
    /// <param name="attrDict">The optional result attribute dictionary.</param>
    public FuncFunctionResultSyntax(
        TypeSyntax type,
        DelimitedSyntaxList<NamedAttributeSyntax> attrDict)
    {
        Type = type;
        AttrDict = attrDict;
    }

    /// <summary>
    /// Gets the result type syntax.
    /// </summary>
    public TypeSyntax Type { get; }

    /// <summary>
    /// Gets the optional attribute dictionary attached to the result.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }

    /// <inheritdoc/>
    public override SourceLocation Location => Type.Location;

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new FuncFunctionResultSyntax(
            rewriter.Visit(Type),
            rewriter.VisitDelimitedList(AttrDict));
    }

    /// <summary>
    /// Writes the result syntax to the provided writer using the current pending suggested trivia
    /// for leading whitespace.
    /// </summary>
    /// <param name="writer">The writer to receive the rendered syntax.</param>
    public override void WriteTo(SyntaxWriter writer)
    {
        Type.WriteTo(writer);
        if (AttrDict.OpenToken.HasValue)
        {
            writer.Write(" ");
            writer.WriteDelimitedList(AttrDict);
        }
    }
}
