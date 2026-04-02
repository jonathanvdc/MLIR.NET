namespace MLIR.Text;

using System.Collections.Generic;
using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Parses generic MLIR syntax into a concrete syntax tree.
/// </summary>
public sealed class Parser
{
    private readonly string source;
    private readonly IReadOnlyList<Token> tokens;
    private readonly DialectRegistry? dialectRegistry;
    private int position;

    private Parser(string source, DialectRegistry? dialectRegistry = null)
    {
        this.source = source;
        this.dialectRegistry = dialectRegistry;
        tokens = Lexer.Lex(source);
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source)
    {
        return new Parser(source).ParseModuleCore();
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <param name="dialectRegistry">The dialect registry used to recognize custom assembly formats.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source, DialectRegistry? dialectRegistry)
    {
        return new Parser(source, dialectRegistry).ParseModuleCore();
    }

    private ModuleSyntax ParseModuleCore()
    {
        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.EndOfFile))
        {
            operations.Add(ParseOperation());
            EnsureOperationBoundary(false);
        }

        return new ModuleSyntax(operations, ToSyntaxToken(ConsumeToken()));
    }

    private OperationSyntax ParseOperation()
    {
        var resultTokens = new List<SyntaxToken>();
        var resultCommaTokens = new List<SyntaxToken>();
        SyntaxToken? equalsToken = null;

        if (Is(TokenKind.SsaName))
        {
            resultTokens.Add(ParseSsaToken());
            while (TryMatch(TokenKind.Comma, out var resultCommaToken))
            {
                resultCommaTokens.Add(ToSyntaxToken(resultCommaToken));
                resultTokens.Add(ParseSsaToken());
            }

            equalsToken = ExpectToken(TokenKind.Equal, "Expected '=' after operation result list.");
        }

        var nameToken = ParseOperationNameToken();
        if (TryParseCustomAssembly(nameToken, resultTokens, resultCommaTokens, equalsToken, out var customBody))
        {
            return new OperationSyntax(
                resultTokens,
                resultCommaTokens,
                equalsToken,
                nameToken,
                customBody);
        }

        var openParenthesisToken = ExpectToken(TokenKind.LParen, "Expected '(' to start the operand list.");
        var operandTokens = new List<SyntaxToken>();
        var operandCommaTokens = new List<SyntaxToken>();

        if (!TryMatch(TokenKind.RParen, out var closeParenthesisTokenValue))
        {
            operandTokens.Add(ParseSsaToken());
            while (TryMatch(TokenKind.Comma, out var operandCommaToken))
            {
                operandCommaTokens.Add(ToSyntaxToken(operandCommaToken));
                operandTokens.Add(ParseSsaToken());
            }

            closeParenthesisTokenValue = ExpectRawToken(TokenKind.RParen, "Expected ')' to close the operand list.");
        }

        var openSuccessorBracketToken = default(SyntaxToken);
        var closeSuccessorBracketToken = default(SyntaxToken);
        var hasSuccessors = false;
        var successorTokens = new List<SyntaxToken>();
        var successorCommaTokens = new List<SyntaxToken>();

        if (TryMatch(TokenKind.LBracket, out var openSuccessorBracketValue))
        {
            hasSuccessors = true;
            openSuccessorBracketToken = ToSyntaxToken(openSuccessorBracketValue);

            if (!TryMatch(TokenKind.RBracket, out var closeSuccessorBracketValue))
            {
                successorTokens.Add(ParseBlockLabelToken());
                while (TryMatch(TokenKind.Comma, out var successorCommaToken))
                {
                    successorCommaTokens.Add(ToSyntaxToken(successorCommaToken));
                    successorTokens.Add(ParseBlockLabelToken());
                }

                closeSuccessorBracketValue = ExpectRawToken(TokenKind.RBracket, "Expected ']' to close the successor list.");
            }

            closeSuccessorBracketToken = ToSyntaxToken(closeSuccessorBracketValue);
        }

        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace) && IsRegionStart())
        {
            regions.Add(ParseRegion());
        }

        var openAttributeBraceToken = default(SyntaxToken);
        var closeAttributeBraceToken = default(SyntaxToken);
        var hasAttributes = false;
        var attributes = new List<NamedAttributeSyntax>();
        var attributeCommaTokens = new List<SyntaxToken>();
        if (Is(TokenKind.LBrace))
        {
            hasAttributes = true;
            openAttributeBraceToken = ExpectToken(TokenKind.LBrace, "Expected '{' to start an attribute dictionary.");
            if (!TryMatch(TokenKind.RBrace, out var closeAttributeBraceValue))
            {
                attributes.Add(ParseAttribute());
                while (TryMatch(TokenKind.Comma, out var attributeCommaToken))
                {
                    attributeCommaTokens.Add(ToSyntaxToken(attributeCommaToken));
                    attributes.Add(ParseAttribute());
                }

                closeAttributeBraceValue = ExpectRawToken(TokenKind.RBrace, "Expected '}' to close the attribute dictionary.");
            }

            closeAttributeBraceToken = ToSyntaxToken(closeAttributeBraceValue);
        }

        SyntaxToken? typeSignatureColonToken = null;
        TypeSyntax? typeSignatureSyntax = null;
        if (Is(TokenKind.Colon))
        {
            typeSignatureColonToken = ExpectToken(TokenKind.Colon, "Expected ':' before the type signature.");
            typeSignatureSyntax = ParseTypeSyntaxUntilOperationBoundary();
        }

        return new OperationSyntax(
            resultTokens,
            resultCommaTokens,
            equalsToken,
            nameToken,
            new DelimitedSyntaxList<SyntaxToken>(
                openParenthesisToken,
                operandTokens,
                operandCommaTokens,
                ToSyntaxToken(closeParenthesisTokenValue)),
            new DelimitedSyntaxList<SyntaxToken>(
                hasSuccessors ? openSuccessorBracketToken : null,
                successorTokens,
                successorCommaTokens,
                hasSuccessors ? closeSuccessorBracketToken : null),
            regions,
            new DelimitedSyntaxList<NamedAttributeSyntax>(
                hasAttributes ? openAttributeBraceToken : null,
                attributes,
                attributeCommaTokens,
                hasAttributes ? closeAttributeBraceToken : null),
            typeSignatureColonToken,
            typeSignatureSyntax);
    }

    private bool TryParseCustomAssembly(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        out OperationBodySyntax body)
    {
        body = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var normalizedName = NormalizeOperationName(nameToken.Text);
        if (!dialectRegistry.TryGetOperation(normalizedName, out var definition) || definition.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = position;
        if (definition.AssemblyFormat.TryParse(
            nameToken,
            resultTokens,
            resultCommaTokens,
            equalsToken,
            new OperationParsingContext(this),
            out var customBody))
        {
            body = customBody!;
            return true;
        }

        position = checkpoint;
        return false;
    }

    private RegionSyntax ParseRegion()
    {
        var openBraceToken = ExpectToken(TokenKind.LBrace, "Expected '{' to start a region.");
        var blocks = new List<BlockSyntax>();
        var pendingEntryOperations = new List<OperationSyntax>();

        while (!Is(TokenKind.RBrace))
        {
            if (Is(TokenKind.BlockLabel))
            {
                if (pendingEntryOperations.Count > 0)
                {
                    // MLIR allows unlabeled operations at the start of a region. Model them as
                    // a synthetic entry block so the CST always has a block-based shape.
                    blocks.Add(new BlockSyntax(
                        new SyntaxToken("^entry"),
                        new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<SyntaxToken>(), null),
                        new SyntaxToken(":"),
                        pendingEntryOperations.ToList()));
                    pendingEntryOperations.Clear();
                }

                blocks.Add(ParseBlock());
            }
            else
            {
                pendingEntryOperations.Add(ParseOperation());
                EnsureOperationBoundary(true);
            }
        }

        if (pendingEntryOperations.Count > 0 || blocks.Count == 0)
        {
            // Keep region bodies uniform even for empty regions and unlabeled entry operations.
            blocks.Insert(0, new BlockSyntax(
                new SyntaxToken("^entry"),
                new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<SyntaxToken>(), null),
                new SyntaxToken(":"),
                pendingEntryOperations.ToList()));
        }

        var closeBraceToken = ExpectToken(TokenKind.RBrace, "Expected '}' to close a region.");
        return new RegionSyntax(openBraceToken, blocks, closeBraceToken);
    }

    private BlockSyntax ParseBlock()
    {
        var labelToken = ParseBlockLabelToken();
        SyntaxToken? openParenthesisToken = null;
        SyntaxToken? closeParenthesisToken = null;
        var arguments = new List<BlockArgumentSyntax>();
        var argumentCommaTokens = new List<SyntaxToken>();

        if (TryMatch(TokenKind.LParen, out var openParenthesisTokenValue))
        {
            openParenthesisToken = ToSyntaxToken(openParenthesisTokenValue);
            if (!TryMatch(TokenKind.RParen, out var closeParenthesisTokenValue))
            {
                arguments.Add(ParseBlockArgument());
                while (TryMatch(TokenKind.Comma, out var argumentCommaToken))
                {
                    argumentCommaTokens.Add(ToSyntaxToken(argumentCommaToken));
                    arguments.Add(ParseBlockArgument());
                }

                closeParenthesisTokenValue = ExpectRawToken(TokenKind.RParen, "Expected ')' after block argument list.");
            }

            closeParenthesisToken = ToSyntaxToken(closeParenthesisTokenValue);
        }

        var colonToken = ExpectToken(TokenKind.Colon, "Expected ':' after block label.");
        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.RBrace) && !Is(TokenKind.BlockLabel))
        {
            operations.Add(ParseOperation());
            EnsureOperationBoundary(true);
        }

        return new BlockSyntax(
            labelToken,
            new DelimitedSyntaxList<BlockArgumentSyntax>(openParenthesisToken, arguments, argumentCommaTokens, closeParenthesisToken),
            colonToken,
            operations);
    }

    private BlockArgumentSyntax ParseBlockArgument()
    {
        var nameToken = ParseSsaToken();
        var colonToken = ExpectToken(TokenKind.Colon, "Expected ':' after block argument name.");
        var type = ParseTypeSyntax(TokenKind.Comma, TokenKind.RParen);
        return new BlockArgumentSyntax(nameToken, colonToken, type);
    }

    private NamedAttributeSyntax ParseAttribute()
    {
        SyntaxToken nameToken;
        if (Is(TokenKind.Identifier) || Is(TokenKind.StringLiteral))
        {
            nameToken = ToSyntaxToken(ConsumeToken());
        }
        else
        {
            throw Error("Expected an attribute name.");
        }

        var equalsToken = ExpectToken(TokenKind.Equal, "Expected '=' after attribute name.");
        var value = ParseAttributeValueSyntax(false, (AttributeDefinition?)null, TokenKind.Comma, TokenKind.RBrace);
        return new NamedAttributeSyntax(nameToken, equalsToken, value);
    }

    private AttributeValueSyntax ParseAttributeValueSyntax(bool stopAtOperationBoundary, string? expectedDefinitionName, params TokenKind[] stopBefore)
    {
        AttributeDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeForParsing(expectedDefinitionName!, out expectedDefinition);
        }

        return ParseAttributeValueSyntax(stopAtOperationBoundary, expectedDefinition, stopBefore);
    }

    private AttributeValueSyntax ParseAttributeValueSyntax(bool stopAtOperationBoundary, AttributeDefinition? expectedDefinition, params TokenKind[] stopBefore)
    {
        if (expectedDefinition != null && TryParseCustomAttributeSyntax(expectedDefinition, out var syntax))
        {
            return syntax;
        }

        if (TryParseSelfIdentifyingAttributeSyntax(out syntax))
        {
            return syntax;
        }

        return new RawAttributeValueSyntax(
            stopAtOperationBoundary
                ? ParseRawUntilDelimiterOrBoundaryInternal(stopBefore)
                : ParseRawUntilDelimiter(stopBefore));
    }

    private TypeSyntax ParseTypeSyntax(params TokenKind[] stopBefore)
    {
        if (TryParseCustomTypeSyntax(out var syntax))
        {
            return syntax;
        }

        return new RawTypeSyntax(ParseRawUntilDelimiter(stopBefore));
    }

    private TypeSyntax ParseTypeSyntaxUntilOperationBoundary()
    {
        if (TryParseCustomTypeSyntax(out var syntax))
        {
            return syntax;
        }

        return new RawTypeSyntax(ParseRawUntilOperationBoundary());
    }

    private bool TryParseCustomAttributeSyntax(string? expectedDefinitionName, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        if (canonicalName == null)
        {
            return false;
        }

        return dialectRegistry.TryGetAttribute(canonicalName, out var definition)
            && TryParseCustomAttributeSyntax(definition, out syntax);
    }

    private bool TryParseSelfIdentifyingAttributeSyntax(out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        return canonicalName != null
            && dialectRegistry.TryGetAttribute(canonicalName, out var definition)
            && TryParseCustomAttributeSyntax(definition, out syntax);
    }

    private bool TryParseCustomAttributeSyntax(AttributeDefinition? definition, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (definition?.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = position;
        if (definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        position = checkpoint;
        return false;
    }

    private bool TryParseCustomTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekTypeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetType(canonicalName, out var definition) || definition.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = position;
        if (definition.AssemblyFormat.TryParse(new TypeParsingContext(this), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        position = checkpoint;
        return false;
    }

    private SyntaxToken ParseOperationNameToken()
    {
        if (!Is(TokenKind.Identifier) && !Is(TokenKind.StringLiteral))
        {
            throw Error("Expected an operation name.");
        }

        return ToSyntaxToken(ConsumeToken());
    }

    private SyntaxToken ParseSsaToken()
    {
        return ExpectToken(TokenKind.SsaName, "Expected an SSA value name.");
    }

    private SyntaxToken ParseBlockLabelToken()
    {
        return ExpectToken(TokenKind.BlockLabel, "Expected a block label name.");
    }

    private RawSyntaxText ParseRawUntilDelimiter(params TokenKind[] delimiters)
    {
        var start = Current.FullStart;
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        // Raw syntax fragments may themselves contain nested delimiters, so only stop when
        // we reach one of the requested delimiters at the outermost nesting level.
        while (true)
        {
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0 && delimiters.Contains(Current.Kind))
            {
                break;
            }

            if (Is(TokenKind.EndOfFile))
            {
                throw Error("Unexpected end of file while parsing raw syntax.");
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;

        return new RawSyntaxText(
            CreateSyntaxTokenList(tokens, firstTokenIndex, position),
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart));
    }

    private RawSyntaxText ParseRawUntilOperationBoundary()
    {
        var start = Current.FullStart;
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        while (!Is(TokenKind.EndOfFile))
        {
            if (depthParen == 0 &&
                depthBrace == 0 &&
                depthBracket == 0 &&
                depthAngle == 0 &&
                IsOperationBoundary(Current, false))
            {
                break;
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;

        return new RawSyntaxText(
            CreateSyntaxTokenList(tokens, firstTokenIndex, position),
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart));
    }

    private bool IsRegionStart()
    {
        if (!Is(TokenKind.LBrace))
        {
            return false;
        }

        // A '{' can start either a region or an attribute dictionary. Peek ahead to decide
        // which production we are looking at without consuming any tokens.
        var lookahead = position + 1;
        if (tokens[lookahead].Kind == TokenKind.RBrace)
        {
            return false;
        }

        if (tokens[lookahead].Kind == TokenKind.BlockLabel || tokens[lookahead].Kind == TokenKind.StringLiteral || tokens[lookahead].Kind == TokenKind.SsaName)
        {
            return true;
        }

        if (tokens[lookahead].Kind != TokenKind.Identifier)
        {
            return false;
        }

        var secondLookahead = tokens[lookahead + 1];
        return secondLookahead.Kind != TokenKind.Equal && secondLookahead.Kind != TokenKind.Comma;
    }

    private void EnsureOperationBoundary(bool allowBlockStart)
    {
        if (!IsOperationBoundary(Current, allowBlockStart))
        {
            throw Error("Expected the end of the operation.");
        }
    }

    private bool IsOperationBoundary(Token token, bool allowBlockStart)
    {
        if (token.Kind == TokenKind.EndOfFile || token.Kind == TokenKind.RBrace)
        {
            return true;
        }

        if (allowBlockStart && token.Kind == TokenKind.BlockLabel && token.LeadingTrivia.Contains('\n'))
        {
            return true;
        }

        return token.LeadingTrivia.Contains('\n');
    }

    private static void UpdateDepth(TokenKind kind, ref int depthParen, ref int depthBrace, ref int depthBracket, ref int depthAngle)
    {
        switch (kind)
        {
            case TokenKind.LParen:
                depthParen++;
                break;
            case TokenKind.RParen:
                depthParen--;
                break;
            case TokenKind.LBrace:
                depthBrace++;
                break;
            case TokenKind.RBrace:
                depthBrace--;
                break;
            case TokenKind.LBracket:
                depthBracket++;
                break;
            case TokenKind.RBracket:
                depthBracket--;
                break;
            case TokenKind.LessThan:
                depthAngle++;
                break;
            case TokenKind.GreaterThan:
                depthAngle--;
                break;
        }
    }

    private static List<SyntaxToken> CreateSyntaxTokenList(IReadOnlyList<Token> tokens, int start, int end)
    {
        var result = new List<SyntaxToken>(end - start);
        for (var i = start; i < end; i++)
        {
            result.Add(ToSyntaxToken(tokens[i]));
        }

        return result;
    }

    private bool TryMatch(TokenKind kind, out Token token)
    {
        if (Current.Kind != kind)
        {
            token = default;
            return false;
        }

        token = ConsumeToken();
        return true;
    }

    private SyntaxToken ExpectToken(TokenKind kind, string message)
    {
        return ToSyntaxToken(ExpectRawToken(kind, message));
    }

    private Token ExpectRawToken(TokenKind kind, string message)
    {
        if (!TryMatch(kind, out var token))
        {
            throw Error(message);
        }

        return token;
    }

    private bool Is(TokenKind kind)
    {
        return Current.Kind == kind;
    }

    private Token ConsumeToken()
    {
        var token = Current;
        position++;
        return token;
    }

    private ParseException Error(string message)
    {
        return new ParseException(new Diagnostic(message, Current.Line, Current.Column));
    }

    internal static SyntaxToken ToSyntaxToken(Token token)
    {
        return new SyntaxToken(token.Text, token.LeadingTrivia, token.Line, token.Column);
    }

    internal bool IsToken(TokenKind kind)
    {
        return Is(kind);
    }

    internal bool TryMatchToken(TokenKind kind, out Token token)
    {
        return TryMatch(kind, out token);
    }

    internal SyntaxToken ExpectTokenInternal(TokenKind kind, string message)
    {
        return ExpectToken(kind, message);
    }

    internal SyntaxToken ParseSsaTokenInternal()
    {
        return ParseSsaToken();
    }

    internal SyntaxToken ParseBlockLabelTokenInternal()
    {
        return ParseBlockLabelToken();
    }

    internal RegionSyntax ParseRegionInternal()
    {
        return ParseRegion();
    }

    internal NamedAttributeSyntax ParseAttributeInternal()
    {
        return ParseAttribute();
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(false, (AttributeDefinition?)null, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(false, expectedDefinitionName, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(AttributeDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(false, expectedDefinition, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(true, (AttributeDefinition?)null, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(true, expectedDefinitionName, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(AttributeDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(true, expectedDefinition, delimiters);
    }

    internal TypeSyntax ParseTypeSyntaxInternal(params TokenKind[] delimiters)
    {
        return ParseTypeSyntax(delimiters);
    }

    internal TypeSyntax ParseTypeSyntaxUntilOperationBoundaryInternal()
    {
        return ParseTypeSyntaxUntilOperationBoundary();
    }

    internal RawSyntaxText ParseRawUntilDelimiterInternal(params TokenKind[] delimiters)
    {
        return ParseRawUntilDelimiter(delimiters);
    }

    internal RawSyntaxText ParseRawUntilOperationBoundaryInternal()
    {
        return ParseRawUntilOperationBoundary();
    }

    internal RawSyntaxText ParseRawUntilDelimiterOrBoundaryInternal(params TokenKind[] delimiters)
    {
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        while (!Is(TokenKind.EndOfFile))
        {
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0)
            {
                if (delimiters.Length > 0 && System.Linq.Enumerable.Contains(delimiters, Current.Kind))
                {
                    break;
                }

                if (IsOperationBoundary(Current, false))
                {
                    break;
                }
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        if (position == firstTokenIndex)
        {
            return new RawSyntaxText(new List<SyntaxToken>(), string.Empty);
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;
        return new RawSyntaxText(
            CreateSyntaxTokenList(tokens, firstTokenIndex, position),
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart));
    }

    internal DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDictInternal()
    {
        if (!Is(TokenKind.LBrace))
        {
            return new DelimitedSyntaxList<NamedAttributeSyntax>(null, new List<NamedAttributeSyntax>(), new List<SyntaxToken>(), null);
        }

        var openBrace = ExpectToken(TokenKind.LBrace, "Expected '{' to start the attribute dictionary.");
        var attrs = new List<NamedAttributeSyntax>();
        var commas = new List<SyntaxToken>();

        if (!TryMatch(TokenKind.RBrace, out var closeBrace))
        {
            attrs.Add(ParseAttribute());
            while (TryMatch(TokenKind.Comma, out var comma))
            {
                commas.Add(ToSyntaxToken(comma));
                attrs.Add(ParseAttribute());
            }

            closeBrace = ExpectRawToken(TokenKind.RBrace, "Expected '}' to close the attribute dictionary.");
        }

        return new DelimitedSyntaxList<NamedAttributeSyntax>(openBrace, attrs, commas, ToSyntaxToken(closeBrace));
    }

    internal DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDictWithKeywordInternal()
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, "attributes", System.StringComparison.Ordinal))
        {
            return new DelimitedSyntaxList<NamedAttributeSyntax>(null, new List<NamedAttributeSyntax>(), new List<SyntaxToken>(), null);
        }

        ConsumeToken();
        return ParseAttrDictInternal();
    }

    internal SyntaxToken ExpectKeywordInternal(string spelling, string message)
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, spelling, System.StringComparison.Ordinal))
        {
            throw Error(message);
        }

        return ToSyntaxToken(ConsumeToken());
    }

    internal IReadOnlyList<RegionSyntax> ParseRegionsInternal()
    {
        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace))
        {
            regions.Add(ParseRegion());
        }

        return regions;
    }

    internal DelimitedSyntaxList<SyntaxToken> ParseSuccessorsInternal()
    {
        if (!Is(TokenKind.LBracket))
        {
            return new DelimitedSyntaxList<SyntaxToken>(null, new List<SyntaxToken>(), new List<SyntaxToken>(), null);
        }

        var openBracket = ExpectToken(TokenKind.LBracket, "Expected '[' for the successor list.");
        var successors = new List<SyntaxToken>();
        var commas = new List<SyntaxToken>();

        if (!TryMatch(TokenKind.RBracket, out var closeBracket))
        {
            successors.Add(ParseBlockLabelToken());
            while (TryMatch(TokenKind.Comma, out var comma))
            {
                commas.Add(ToSyntaxToken(comma));
                successors.Add(ParseBlockLabelToken());
            }

            closeBracket = ExpectRawToken(TokenKind.RBracket, "Expected ']' to close the successor list.");
        }

        return new DelimitedSyntaxList<SyntaxToken>(openBracket, successors, commas, ToSyntaxToken(closeBracket));
    }

    internal DelimitedSyntaxList<SyntaxToken> ParseOperandsInternal()
    {
        var openParen = ExpectToken(TokenKind.LParen, "Expected '(' for the operand list.");
        var operands = new List<SyntaxToken>();
        var commas = new List<SyntaxToken>();

        if (!TryMatch(TokenKind.RParen, out var closeParen))
        {
            operands.Add(ParseSsaToken());
            while (TryMatch(TokenKind.Comma, out var comma))
            {
                commas.Add(ToSyntaxToken(comma));
                operands.Add(ParseSsaToken());
            }

            closeParen = ExpectRawToken(TokenKind.RParen, "Expected ')' to close the operand list.");
        }

        return new DelimitedSyntaxList<SyntaxToken>(openParen, operands, commas, ToSyntaxToken(closeParen));
    }

    internal bool IsKeywordInternal(string spelling)
    {
        return Is(TokenKind.Identifier) && string.Equals(Current.Text, spelling, System.StringComparison.Ordinal);
    }

    private string? TryPeekAttributeDefinitionName()
    {
        if (!Is(TokenKind.Hash))
        {
            return null;
        }

        var lookahead = position + 1;
        return lookahead < tokens.Count && tokens[lookahead].Kind == TokenKind.Identifier
            ? tokens[lookahead].Text
            : null;
    }

    private string? TryPeekTypeDefinitionName()
    {
        return Is(TokenKind.Identifier) ? Current.Text : null;
    }

    private static string NormalizeOperationName(string name)
    {
        return name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"' ? name.Substring(1, name.Length - 2) : name;
    }

    private Token Current => tokens[position];
}
