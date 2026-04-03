namespace MLIR.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Types.Collections;
using MLIR.Syntax.Types.Primitives;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;

/// <summary>
/// Parses generic MLIR syntax into a concrete syntax tree.
/// </summary>
public sealed partial class Parser
{
    private readonly struct ParseMark
    {
        public ParseMark(int position)
        {
            Position = position;
        }

        public int Position { get; }
    }

    private readonly string source;
    private readonly IReadOnlyList<Token> tokens;
    private readonly DialectRegistry? dialectRegistry;
    private int position;
    private static readonly BooleanLiteralAttributeAssemblyFormat BooleanLiteralAttributeAssemblyFormat = new();
    private static readonly IntegerLiteralAttributeAssemblyFormat IntegerLiteralAttributeAssemblyFormat = new();
    private static readonly FloatingPointLiteralAttributeAssemblyFormat FloatingPointLiteralAttributeAssemblyFormat = new();
    private static readonly StringLiteralAttributeAssemblyFormat StringLiteralAttributeAssemblyFormat = new();
    private static readonly DenseIntegerArrayAttributeAssemblyFormat DenseArrayAttributeAssemblyFormat = new();
    private static readonly ElementsAttributeAssemblyFormat ElementsAttributeAssemblyFormat = new();

    private Parser(string source, IReadOnlyList<Token> tokens, DialectRegistry? dialectRegistry = null)
    {
        this.source = source;
        this.dialectRegistry = dialectRegistry;
        this.tokens = tokens;
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source)
    {
        return TryParseModule(source, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <param name="dialectRegistry">The dialect registry used to recognize custom assembly formats.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source, DialectRegistry? dialectRegistry)
    {
        return TryParseModule(source, dialectRegistry, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Tries to parse a module from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseModule(string source, out ModuleSyntax? syntax, out Diagnostic? diagnostic)
    {
        return TryParseModule(source, null, out syntax, out diagnostic);
    }

    /// <summary>
    /// Tries to parse a module from the supplied MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    public static bool TryParseModule(string source, DialectRegistry? dialectRegistry, out ModuleSyntax? syntax, out Diagnostic? diagnostic)
    {
        syntax = null;
        var parserResult = TryCreateParser(source, dialectRegistry);
        if (!parserResult.IsSuccess)
        {
            diagnostic = parserResult.Diagnostic;
            return false;
        }

        var result = parserResult.Value.TryParseModuleCoreResult();
        syntax = result.IsSuccess ? result.Value : null;
        diagnostic = result.Diagnostic;
        return result.IsSuccess;
    }

    /// <summary>
    /// Parses a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static AttributeValueSyntax ParseAttributeValue(string source, DialectRegistry? dialectRegistry = null, AttributeConstraintDefinition? expectedDefinition = null)
    {
        return TryParseAttributeValue(source, dialectRegistry, expectedDefinition, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Tries to parse a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseAttributeValue(
        string source,
        out AttributeValueSyntax? syntax,
        out Diagnostic? diagnostic)
    {
        return TryParseAttributeValue(source, null, null, out syntax, out diagnostic);
    }

    /// <summary>
    /// Tries to parse a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseAttributeValue(
        string source,
        DialectRegistry? dialectRegistry,
        out AttributeValueSyntax? syntax,
        out Diagnostic? diagnostic)
    {
        return TryParseAttributeValue(source, dialectRegistry, null, out syntax, out diagnostic);
    }

    /// <summary>
    /// Tries to parse a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseAttributeValue(
        string source,
        DialectRegistry? dialectRegistry,
        AttributeConstraintDefinition? expectedDefinition,
        out AttributeValueSyntax? syntax,
        out Diagnostic? diagnostic)
    {
        syntax = null;
        var parserResult = TryCreateParser(source, dialectRegistry);
        if (!parserResult.IsSuccess)
        {
            diagnostic = parserResult.Diagnostic;
            return false;
        }

        var parser = parserResult.Value;
        var result = parser.TryParseStandaloneAttributeValueResult(expectedDefinition);
        syntax = result.IsSuccess ? result.Value : null;
        diagnostic = result.Diagnostic;
        return result.IsSuccess;
    }

    /// <summary>
    /// Parses a standalone type from the supplied MLIR source text.
    /// </summary>
    public static TypeSyntax ParseType(string source, DialectRegistry? dialectRegistry = null)
    {
        return TryParseType(source, dialectRegistry, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Tries to parse a standalone type from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseType(string source, DialectRegistry? dialectRegistry, out TypeSyntax? syntax, out Diagnostic? diagnostic)
    {
        syntax = null;
        var parserResult = TryCreateParser(source, dialectRegistry);
        if (!parserResult.IsSuccess)
        {
            diagnostic = parserResult.Diagnostic;
            return false;
        }

        var parser = parserResult.Value;
        var result = parser.TryParseStandaloneTypeResult();
        syntax = result.IsSuccess ? result.Value : null;
        diagnostic = result.Diagnostic;
        return result.IsSuccess;
    }

    /// <summary>
    /// Tries to parse a standalone type from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseType(string source, out TypeSyntax? syntax, out Diagnostic? diagnostic)
    {
        return TryParseType(source, null, out syntax, out diagnostic);
    }

    private ParseResult<ModuleSyntax> TryParseModuleCoreResult()
    {
        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.EndOfFile))
        {
            var operationResult = TryParseOperationResult();
            if (!operationResult.IsSuccess)
            {
                return ParseResult<ModuleSyntax>.Failure(operationResult.Diagnostic!);
            }

            operations.Add(operationResult.Value);
            var boundaryResult = EnsureOperationBoundaryResult(false);
            if (!boundaryResult.IsSuccess)
            {
                return ParseResult<ModuleSyntax>.Failure(boundaryResult.Diagnostic!);
            }
        }

        return ParseResult<ModuleSyntax>.Success(new ModuleSyntax(operations, ToSyntaxToken(ConsumeToken())));
    }

    private ParseResult<OperationSyntax> TryParseOperationResult()
    {
        var resultTokens = new List<SyntaxToken>();
        var resultCommaTokens = new List<SyntaxToken>();
        SyntaxToken? equalsToken = null;

        if (Is(TokenKind.SsaName))
        {
            var firstResultTokenResult = TryParseSsaTokenResult();
            if (!firstResultTokenResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(firstResultTokenResult.Diagnostic!);
            }

            var firstResultToken = firstResultTokenResult.Value;
            resultTokens.Add(firstResultToken);

            if (TryMatch(TokenKind.Colon, out _))
            {
                var countTokenResult = ExpectRawTokenResult(TokenKind.Integer, "Expected result count after ':'.");
                if (!countTokenResult.IsSuccess)
                {
                    return ParseResult<OperationSyntax>.Failure(countTokenResult.Diagnostic!);
                }

                var countToken = countTokenResult.Value;
                var count = int.Parse(countToken.Text, CultureInfo.InvariantCulture);
                for (var i = 1; i < count; i++)
                {
                    resultTokens.Add(new SyntaxToken(firstResultToken.Text + "#" + i.ToString(CultureInfo.InvariantCulture)));
                }
            }

            while (TryMatch(TokenKind.Comma, out var resultCommaToken))
            {
                resultCommaTokens.Add(ToSyntaxToken(resultCommaToken));
                var nextResultToken = TryParseSsaTokenResult();
                if (!nextResultToken.IsSuccess)
                {
                    return ParseResult<OperationSyntax>.Failure(nextResultToken.Diagnostic!);
                }

                resultTokens.Add(nextResultToken.Value);
            }

            var equalsTokenResult = ExpectTokenResult(TokenKind.Equal, "Expected '=' after operation result list.");
            if (!equalsTokenResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(equalsTokenResult.Diagnostic!);
            }

            equalsToken = equalsTokenResult.Value;
        }

        var nameTokenResult = TryParseOperationNameTokenResult();
        if (!nameTokenResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(nameTokenResult.Diagnostic!);
        }

        var nameToken = nameTokenResult.Value;
        if (!nameToken.Text.StartsWith("\"", System.StringComparison.Ordinal)
            && TryParseCustomAssembly(nameToken, resultTokens, resultCommaTokens, equalsToken, out var customBody))
        {
            return ParseResult<OperationSyntax>.Success(new OperationSyntax(
                resultTokens,
                resultCommaTokens,
                equalsToken,
                nameToken,
                customBody));
        }

        if (!nameToken.Text.StartsWith("\"", System.StringComparison.Ordinal)
            && TryParseProjectedCustomLikeOperationBody(out var projectedBody))
        {
            return ParseResult<OperationSyntax>.Success(new OperationSyntax(
                resultTokens,
                resultCommaTokens,
                equalsToken,
                nameToken,
                projectedBody!));
        }

        var operandsResult = TryParseOperandsResult();
        if (!operandsResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(operandsResult.Diagnostic!);
        }

        var successorsResult = TryParseSuccessorsResult();
        if (!successorsResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(successorsResult.Diagnostic!);
        }

        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace) && IsRegionStart())
        {
            var regionResult = TryParseRegionResult();
            if (!regionResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(regionResult.Diagnostic!);
            }

            regions.Add(regionResult.Value);
        }

        var attributesResult = TryParseAttrDictResult();
        if (!attributesResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(attributesResult.Diagnostic!);
        }

        SyntaxToken? typeSignatureColonToken = null;
        TypeSyntax? typeSignatureSyntax = null;
        if (Is(TokenKind.Colon))
        {
            var colonResult = ExpectTokenResult(TokenKind.Colon, "Expected ':' before the type signature.");
            if (!colonResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(colonResult.Diagnostic!);
            }

            typeSignatureColonToken = colonResult.Value;
            var typeResult = TryParseTypeSyntaxUntilOperationBoundaryResult();
            if (!typeResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(typeResult.Diagnostic!);
            }

            typeSignatureSyntax = typeResult.Value;
        }

        return ParseResult<OperationSyntax>.Success(new OperationSyntax(
            resultTokens,
            resultCommaTokens,
            equalsToken,
            nameToken,
            operandsResult.Value,
            successorsResult.Value,
            regions,
            attributesResult.Value,
            typeSignatureColonToken,
            typeSignatureSyntax));
    }

    private bool TryParseProjectedCustomLikeOperationBody(out OperationBodySyntax? body)
    {
        body = null;
        var checkpoint = Mark();

        var operandTokens = new List<SyntaxToken>();
        var operandCommaTokens = new List<SyntaxToken>();
        if (Is(TokenKind.SsaName))
        {
            ParseCommaSeparatedItems(operandTokens, operandCommaTokens, () => ThrowIfFailure(TryParseSsaTokenResult()));
        }

        var attributeDict = ParseAttrDictInternal();
        if (!TryMatch(TokenKind.Colon, out var colonToken))
        {
            Reset(checkpoint);
            return false;
        }

        var typeSignature = new RawTypeSyntax(ParseRawUntilOperationBoundaryInternal());
        body = new GenericOperationBodySyntax(
            new DelimitedSyntaxList<SyntaxToken>(
                new SyntaxToken("("),
                operandTokens,
                operandCommaTokens,
                new SyntaxToken(")")),
            new DelimitedSyntaxList<SyntaxToken>(null, new List<SyntaxToken>(), new List<SyntaxToken>(), null),
            new List<RegionSyntax>(),
            attributeDict,
            ToSyntaxToken(colonToken),
            typeSignature);
        return true;
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

        var checkpoint = Mark();
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

        Reset(checkpoint);
        return false;
    }

    private ParseResult<RegionSyntax> TryParseRegionResult()
    {
        var openBraceResult = ExpectTokenResult(TokenKind.LBrace, "Expected '{' to start a region.");
        if (!openBraceResult.IsSuccess)
        {
            return ParseResult<RegionSyntax>.Failure(openBraceResult.Diagnostic!);
        }

        var openBraceToken = openBraceResult.Value;
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

                var blockResult = TryParseBlockResult();
                if (!blockResult.IsSuccess)
                {
                    return ParseResult<RegionSyntax>.Failure(blockResult.Diagnostic!);
                }

                blocks.Add(blockResult.Value);
            }
            else
            {
                var operationResult = TryParseOperationResult();
                if (!operationResult.IsSuccess)
                {
                    return ParseResult<RegionSyntax>.Failure(operationResult.Diagnostic!);
                }

                pendingEntryOperations.Add(operationResult.Value);
                var boundaryResult = EnsureOperationBoundaryResult(true);
                if (!boundaryResult.IsSuccess)
                {
                    return ParseResult<RegionSyntax>.Failure(boundaryResult.Diagnostic!);
                }
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

        var closeBraceResult = ExpectTokenResult(TokenKind.RBrace, "Expected '}' to close a region.");
        if (!closeBraceResult.IsSuccess)
        {
            return ParseResult<RegionSyntax>.Failure(closeBraceResult.Diagnostic!);
        }

        return ParseResult<RegionSyntax>.Success(new RegionSyntax(openBraceToken, blocks, closeBraceResult.Value));
    }

    private ParseResult<BlockSyntax> TryParseBlockResult()
    {
        var labelResult = TryParseBlockLabelTokenResult();
        if (!labelResult.IsSuccess)
        {
            return ParseResult<BlockSyntax>.Failure(labelResult.Diagnostic!);
        }

        var argumentsResult = TryParseOptionalCommaSeparatedDelimitedList(
            TokenKind.LParen,
            TokenKind.RParen,
            TryParseBlockArgumentResult,
            "Expected ')' after block argument list.");
        if (!argumentsResult.IsSuccess)
        {
            return ParseResult<BlockSyntax>.Failure(argumentsResult.Diagnostic!);
        }

        var colonResult = ExpectTokenResult(TokenKind.Colon, "Expected ':' after block label.");
        if (!colonResult.IsSuccess)
        {
            return ParseResult<BlockSyntax>.Failure(colonResult.Diagnostic!);
        }

        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.RBrace) && !Is(TokenKind.BlockLabel))
        {
            var operationResult = TryParseOperationResult();
            if (!operationResult.IsSuccess)
            {
                return ParseResult<BlockSyntax>.Failure(operationResult.Diagnostic!);
            }

            operations.Add(operationResult.Value);
            var boundaryResult = EnsureOperationBoundaryResult(true);
            if (!boundaryResult.IsSuccess)
            {
                return ParseResult<BlockSyntax>.Failure(boundaryResult.Diagnostic!);
            }
        }

        return ParseResult<BlockSyntax>.Success(new BlockSyntax(
            labelResult.Value,
            argumentsResult.Value,
            colonResult.Value,
            operations));
    }

    private ParseResult<BlockArgumentSyntax> TryParseBlockArgumentResult()
    {
        var nameResult = TryParseSsaTokenResult();
        if (!nameResult.IsSuccess)
        {
            return ParseResult<BlockArgumentSyntax>.Failure(nameResult.Diagnostic!);
        }

        var colonResult = ExpectTokenResult(TokenKind.Colon, "Expected ':' after block argument name.");
        if (!colonResult.IsSuccess)
        {
            return ParseResult<BlockArgumentSyntax>.Failure(colonResult.Diagnostic!);
        }

        var typeResult = TryParseTypeSyntaxResult(TokenKind.Comma, TokenKind.RParen);
        if (!typeResult.IsSuccess)
        {
            return ParseResult<BlockArgumentSyntax>.Failure(typeResult.Diagnostic!);
        }

        return ParseResult<BlockArgumentSyntax>.Success(new BlockArgumentSyntax(nameResult.Value, colonResult.Value, typeResult.Value));
    }

    private ParseResult<NamedAttributeSyntax> TryParseAttributeResult()
    {
        SyntaxToken nameToken;
        if (Is(TokenKind.Identifier) || Is(TokenKind.StringLiteral))
        {
            nameToken = ToSyntaxToken(ConsumeToken());
        }
        else
        {
            return ParseResult<NamedAttributeSyntax>.Failure(CreateDiagnostic("Expected an attribute name."));
        }

        SyntaxToken separatorToken;
        if (TryMatch(TokenKind.Equal, out var equalsToken))
        {
            separatorToken = ToSyntaxToken(equalsToken);
        }
        else if (TryMatch(TokenKind.Colon, out var colonToken))
        {
            separatorToken = ToSyntaxToken(colonToken);
        }
        else
        {
            return ParseResult<NamedAttributeSyntax>.Failure(CreateDiagnostic("Expected '=' or ':' after attribute name."));
        }

        var valueResult = TryParseAttributeValueSyntaxResult(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBrace);
        if (!valueResult.IsSuccess)
        {
            return ParseResult<NamedAttributeSyntax>.Failure(valueResult.Diagnostic!);
        }

        return ParseResult<NamedAttributeSyntax>.Success(new NamedAttributeSyntax(nameToken, separatorToken, valueResult.Value));
    }

    private static string TokenText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.LParen => "(",
            TokenKind.RParen => ")",
            TokenKind.LessThan => "<",
            TokenKind.GreaterThan => ">",
            _ => kind.ToString(),
        };
    }

    private ParseResult<SyntaxToken> TryParseOperationNameTokenResult()
    {
        if (!Is(TokenKind.Identifier) && !Is(TokenKind.StringLiteral))
        {
            return ParseResult<SyntaxToken>.Failure(CreateDiagnostic("Expected an operation name."));
        }

        return ParseResult<SyntaxToken>.Success(ToSyntaxToken(ConsumeToken()));
    }

    private ParseResult<SyntaxToken> TryParseSsaTokenResult()
    {
        return ExpectTokenResult(TokenKind.SsaName, "Expected an SSA value name.");
    }

    private ParseResult<SyntaxToken> TryParseBlockLabelTokenResult()
    {
        return ExpectTokenResult(TokenKind.BlockLabel, "Expected a block label name.");
    }

    private RawSyntaxText ParseRawUntilDelimiter(params TokenKind[] delimiters)
    {
        var result = TryParseRawUntilDelimiterResult(delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private RawSyntaxText ParseRawUntilDelimiterOrKeyword(TokenKind[] delimiters, string[] keywords)
    {
        var result = TryParseRawUntilDelimiterOrKeywordResult(delimiters, keywords);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private ParseResult<RawSyntaxText> TryParseRawUntilDelimiterResult(params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterOrKeywordResult(delimiters, []);
    }

    private ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrKeywordResult(TokenKind[] delimiters, string[] keywords)
    {
        return TryScanRawFragment(
            delimiters,
            keywords,
            stopAtOperationBoundary: false,
            allowEmpty: false,
            eofMessage: "Unexpected end of file while parsing raw syntax.");
    }

    private RawSyntaxText ParseRawUntilOperationBoundary()
    {
        var result = TryParseRawUntilOperationBoundaryResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private ParseResult<RawSyntaxText> TryParseRawUntilOperationBoundaryResult()
    {
        return TryScanRawFragment([], [], stopAtOperationBoundary: true, allowEmpty: false, eofMessage: null);
    }

    private ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrBoundaryResult(params TokenKind[] delimiters)
    {
        return TryScanRawFragment(delimiters, [], stopAtOperationBoundary: true, allowEmpty: true, eofMessage: null);
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
        return secondLookahead.Kind != TokenKind.Equal
            && secondLookahead.Kind != TokenKind.Colon
            && secondLookahead.Kind != TokenKind.Comma;
    }

    private ParseResult<bool> EnsureOperationBoundaryResult(bool allowBlockStart)
    {
        return IsOperationBoundary(Current, allowBlockStart)
            ? ParseResult<bool>.Success(true)
            : ParseResult<bool>.Failure(CreateDiagnostic("Expected the end of the operation."));
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

    private ParseResult<SyntaxToken> ExpectTokenResult(TokenKind kind, string message)
    {
        var rawTokenResult = ExpectRawTokenResult(kind, message);
        return rawTokenResult.IsSuccess
            ? ParseResult<SyntaxToken>.Success(ToSyntaxToken(rawTokenResult.Value))
            : ParseResult<SyntaxToken>.Failure(rawTokenResult.Diagnostic!);
    }

    private ParseResult<Token> ExpectRawTokenResult(TokenKind kind, string message)
    {
        if (!TryMatch(kind, out var token))
        {
            return ParseResult<Token>.Failure(CreateDiagnostic(message));
        }

        return ParseResult<Token>.Success(token);
    }

    private bool Is(TokenKind kind)
    {
        return Current.Kind == kind;
    }

    private ParseMark Mark()
    {
        return new ParseMark(position);
    }

    private void Reset(ParseMark mark)
    {
        position = mark.Position;
    }

    private void ParseCommaSeparatedItems<T>(
        List<T> items,
        List<SyntaxToken> separators,
        Func<T> parseElement)
    {
        items.Add(parseElement());
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            separators.Add(ToSyntaxToken(comma));
            items.Add(parseElement());
        }
    }

    private DelimitedSyntaxList<T> ParseRequiredCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<T> parseElement,
        string openMessage,
        string closeMessage)
    {
        var openTokenResult = ExpectTokenResult(openKind, openMessage);
        if (!openTokenResult.IsSuccess)
        {
            throw new ParseException(openTokenResult.Diagnostic!);
        }

        var openToken = openTokenResult.Value;
        return ParseCommaSeparatedDelimitedListCore(openToken, closeKind, parseElement, closeMessage);
    }

    private DelimitedSyntaxList<T> ParseOptionalCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<T> parseElement,
        string closeMessage)
    {
        if (!TryMatch(openKind, out var openToken))
        {
            return EmptyDelimitedSyntaxList<T>();
        }

        return ParseCommaSeparatedDelimitedListCore(ToSyntaxToken(openToken), closeKind, parseElement, closeMessage);
    }

    private ParseResult<DelimitedSyntaxList<T>> TryParseOptionalCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<ParseResult<T>> parseElement,
        string closeMessage)
    {
        if (!TryMatch(openKind, out var openToken))
        {
            return ParseResult<DelimitedSyntaxList<T>>.Success(EmptyDelimitedSyntaxList<T>());
        }

        return TryParseCommaSeparatedDelimitedListCore(ToSyntaxToken(openToken), closeKind, parseElement, closeMessage);
    }

    private ParseResult<DelimitedSyntaxList<T>> TryParseRequiredCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<ParseResult<T>> parseElement,
        string openMessage,
        string closeMessage)
    {
        var openTokenResult = ExpectTokenResult(openKind, openMessage);
        if (!openTokenResult.IsSuccess)
        {
            return ParseResult<DelimitedSyntaxList<T>>.Failure(openTokenResult.Diagnostic!);
        }

        return TryParseCommaSeparatedDelimitedListCore(openTokenResult.Value, closeKind, parseElement, closeMessage);
    }

    private ParseResult<DelimitedSyntaxList<T>> TryParseCommaSeparatedDelimitedListCore<T>(
        SyntaxToken openToken,
        TokenKind closeKind,
        Func<ParseResult<T>> parseElement,
        string closeMessage)
    {
        var items = new List<T>();
        var separators = new List<SyntaxToken>();
        if (!TryMatch(closeKind, out var closeToken))
        {
            var itemsResult = TryParseCommaSeparatedItems(items, separators, parseElement);
            if (!itemsResult.IsSuccess)
            {
                return ParseResult<DelimitedSyntaxList<T>>.Failure(itemsResult.Diagnostic!);
            }

            var closeTokenResult = ExpectRawTokenResult(closeKind, closeMessage);
            if (!closeTokenResult.IsSuccess)
            {
                return ParseResult<DelimitedSyntaxList<T>>.Failure(closeTokenResult.Diagnostic!);
            }

            closeToken = closeTokenResult.Value;
        }

        return ParseResult<DelimitedSyntaxList<T>>.Success(new DelimitedSyntaxList<T>(openToken, items, separators, ToSyntaxToken(closeToken)));
    }

    private DelimitedSyntaxList<T> ParseCommaSeparatedDelimitedListCore<T>(
        SyntaxToken openToken,
        TokenKind closeKind,
        Func<T> parseElement,
        string closeMessage)
    {
        var items = new List<T>();
        var separators = new List<SyntaxToken>();
        if (!TryMatch(closeKind, out var closeToken))
        {
            ParseCommaSeparatedItems(items, separators, parseElement);
            var closeTokenResult = ExpectRawTokenResult(closeKind, closeMessage);
            if (!closeTokenResult.IsSuccess)
            {
                throw new ParseException(closeTokenResult.Diagnostic!);
            }

            closeToken = closeTokenResult.Value;
        }

        return new DelimitedSyntaxList<T>(openToken, items, separators, ToSyntaxToken(closeToken));
    }

    private ParseResult<RawSyntaxText> TryScanRawFragment(
        TokenKind[] delimiters,
        string[] keywords,
        bool stopAtOperationBoundary,
        bool allowEmpty,
        string? eofMessage)
    {
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        // Raw syntax fragments may themselves contain nested delimiters, so only stop when
        // we reach a requested delimiter or operation boundary at the outermost nesting level.
        while (true)
        {
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0)
            {
                if (IsAnyDelimiter(delimiters, Current.Kind))
                {
                    break;
                }

                if (Current.Kind == TokenKind.Identifier && IsAnyKeyword(keywords, Current.Text))
                {
                    break;
                }

                if (stopAtOperationBoundary && IsOperationBoundary(Current, false))
                {
                    break;
                }
            }

            if (Is(TokenKind.EndOfFile))
            {
                if (eofMessage != null)
                {
                    return ParseResult<RawSyntaxText>.Failure(CreateDiagnostic(eofMessage));
                }

                break;
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        if (position == firstTokenIndex)
        {
            return allowEmpty
                ? ParseResult<RawSyntaxText>.Success(new RawSyntaxText(new List<SyntaxToken>(), string.Empty))
                : ParseResult<RawSyntaxText>.Failure(CreateDiagnostic("Expected raw syntax."));
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;

        return ParseResult<RawSyntaxText>.Success(new RawSyntaxText(
            CreateSyntaxTokenList(tokens, firstTokenIndex, position),
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart)));
    }

    private static bool IsAnyDelimiter(TokenKind[] delimiters, TokenKind kind)
    {
        for (var i = 0; i < delimiters.Length; i++)
        {
            if (delimiters[i] == kind)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAnyKeyword(string[] keywords, string text)
    {
        for (var i = 0; i < keywords.Length; i++)
        {
            if (string.Equals(keywords[i], text, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static DelimitedSyntaxList<T> EmptyDelimitedSyntaxList<T>()
    {
        return new DelimitedSyntaxList<T>(null, new List<T>(), new List<SyntaxToken>(), null);
    }

    private ParseResult<bool> TryParseCommaSeparatedItems<T>(
        List<T> items,
        List<SyntaxToken> separators,
        Func<ParseResult<T>> parseElement)
    {
        var firstItem = parseElement();
        if (!firstItem.IsSuccess)
        {
            return ParseResult<bool>.Failure(firstItem.Diagnostic!);
        }

        items.Add(firstItem.Value);
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            separators.Add(ToSyntaxToken(comma));
            var item = parseElement();
            if (!item.IsSuccess)
            {
                return ParseResult<bool>.Failure(item.Diagnostic!);
            }

            items.Add(item.Value);
        }

        return ParseResult<bool>.Success(true);
    }

    private Token ConsumeToken()
    {
        var token = Current;
        position++;
        return token;
    }

    private static ParseResult<Parser> TryCreateParser(string source, DialectRegistry? dialectRegistry)
    {
        var lexResult = Lexer.TryLexCore(source);
        if (!lexResult.IsSuccess)
        {
            return ParseResult<Parser>.Failure(lexResult.Diagnostic!);
        }

        return ParseResult<Parser>.Success(new Parser(source, lexResult.Value, dialectRegistry));
    }

    private ParseResult<TypeSyntax> TryParseStandaloneTypeResult()
    {
        var parsed = TryParseTypeSyntaxUntilOperationBoundaryResult();
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        return !Is(TokenKind.EndOfFile)
            ? ParseResult<TypeSyntax>.Failure(CreateDiagnostic("Expected the type to consume the entire input."))
            : parsed;
    }

    private ParseResult<AttributeValueSyntax> TryParseStandaloneAttributeValueResult(AttributeConstraintDefinition? expectedDefinition)
    {
        var parsed = TryParseAttributeValueSyntaxResult(false, expectedDefinition);
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        return !Is(TokenKind.EndOfFile)
            ? ParseResult<AttributeValueSyntax>.Failure(CreateDiagnostic("Expected the attribute value to consume the entire input."))
            : parsed;
    }

    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopAtOperationBoundary: false);
    }

    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopBeforeKeywords, stopAtOperationBoundary: false);
    }

    private ParseResult<TypeSyntax> TryParseTypeSyntaxUntilOperationBoundaryResult()
    {
        return TryParseTypeSyntaxCoreResult([], stopAtOperationBoundary: true);
    }

    private ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxResult(bool stopAtOperationBoundary, AttributeConstraintDefinition? expectedDefinition, params TokenKind[] stopBefore)
    {
        if (expectedDefinition != null)
        {
            var expectedResult = TryParseCustomAttributeSyntaxResult(expectedDefinition);
            if (!expectedResult.IsNoMatch)
            {
                return expectedResult;
            }
        }

        var selfIdentifyingResult = TryParseSelfIdentifyingAttributeSyntaxResult();
        if (!selfIdentifyingResult.IsNoMatch)
        {
            return selfIdentifyingResult;
        }

        var builtinStructuredResult = TryParseBuiltinStructuredAttributeSyntaxResult();
        if (!builtinStructuredResult.IsNoMatch)
        {
            return builtinStructuredResult;
        }

        var rawResult = stopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(stopBefore)
            : TryParseRawUntilDelimiterResult(stopBefore);
        return rawResult.IsSuccess
            ? ParseResult<AttributeValueSyntax>.Success(new RawAttributeValueSyntax(rawResult.Value))
            : ParseResult<AttributeValueSyntax>.Failure(rawResult.Diagnostic!);
    }

    private ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxResult(bool stopAtOperationBoundary, string? expectedDefinitionName, params TokenKind[] stopBefore)
    {
        AttributeConstraintDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeConstraint(expectedDefinitionName!, out expectedDefinition);
        }

        return TryParseAttributeValueSyntaxResult(stopAtOperationBoundary, expectedDefinition, stopBefore);
    }

    private ParseResult<TypeSyntax> TryParseTypeSyntaxCoreResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, [], stopAtOperationBoundary);
    }

    private ParseResult<TypeSyntax> TryParseTypeSyntaxCoreResult(TokenKind[] stopBefore, string[] stopBeforeKeywords, bool stopAtOperationBoundary)
    {
        var builtinTypeResult = TryParseBuiltinTypeSyntaxResult(stopBefore, stopAtOperationBoundary);
        if (!builtinTypeResult.IsNoMatch)
        {
            return builtinTypeResult;
        }

        var customTypeResult = TryParseCustomTypeSyntaxResult();
        if (!customTypeResult.IsNoMatch)
        {
            return customTypeResult;
        }

        var rawResult = stopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(stopBefore)
            : TryParseRawUntilDelimiterOrKeywordResult(stopBefore, stopBeforeKeywords);
        return rawResult.IsSuccess
            ? ParseResult<TypeSyntax>.Success(new RawTypeSyntax(rawResult.Value))
            : ParseResult<TypeSyntax>.Failure(rawResult.Diagnostic!);
    }

    private ParseResult<DelimitedSyntaxList<SyntaxToken>> TryParseOperandsResult()
    {
        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LParen,
            TokenKind.RParen,
            TryParseSsaTokenResult,
            "Expected '(' for the operand list.",
            "Expected ')' to close the operand list.");
    }

    private ParseResult<DelimitedSyntaxList<SyntaxToken>> TryParseSuccessorsResult()
    {
        if (!Is(TokenKind.LBracket))
        {
            return ParseResult<DelimitedSyntaxList<SyntaxToken>>.Success(EmptyDelimitedSyntaxList<SyntaxToken>());
        }

        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            TryParseBlockLabelTokenResult,
            "Expected '[' for the successor list.",
            "Expected ']' to close the successor list.");
    }

    private ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictResult()
    {
        if (!Is(TokenKind.LBrace))
        {
            return ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>>.Success(EmptyDelimitedSyntaxList<NamedAttributeSyntax>());
        }

        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBrace,
            TokenKind.RBrace,
            TryParseAttributeResult,
            "Expected '{' to start the attribute dictionary.",
            "Expected '}' to close the attribute dictionary.");
    }

    private ParseResult<AttributeValueSyntax> TryParseCustomAttributeSyntaxResult(AttributeConstraintDefinition? definition)
    {
        if (definition?.AssemblyFormat == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var syntax))
        {
            return ParseResult<AttributeValueSyntax>.Success(syntax!);
        }

        Reset(checkpoint);
        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    private ParseResult<AttributeValueSyntax> TryParseSelfIdentifyingAttributeSyntaxResult()
    {
        if (dialectRegistry == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetAttribute(canonicalName, out var definition))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return TryParseCustomAttributeSyntaxResult(definition);
    }

    private ParseResult<AttributeValueSyntax> TryParseBuiltinStructuredAttributeSyntaxResult()
    {
        if (Is(TokenKind.LBracket))
        {
            var arrayResult = TryParseArrayAttributeValueSyntaxResult();
            return arrayResult.IsSuccess
                ? ParseResult<AttributeValueSyntax>.Success(arrayResult.Value)
                : ParseResult<AttributeValueSyntax>.Failure(arrayResult.Diagnostic!);
        }

        if (Is(TokenKind.LBrace))
        {
            var dictResult = TryParseAttrDictResult();
            return dictResult.IsSuccess
                ? ParseResult<AttributeValueSyntax>.Success(new DictionaryAttributeValueSyntax(dictResult.Value))
                : ParseResult<AttributeValueSyntax>.Failure(dictResult.Diagnostic!);
        }

        var denseArrayResult = TryParseAttributeAssemblyFormatResult(BuiltinAttributeConstraintDefinition("DenseArrayAttr"), DenseArrayAttributeAssemblyFormat);
        if (!denseArrayResult.IsNoMatch)
        {
            return denseArrayResult;
        }

        return TryParseAttributeAssemblyFormatResult(BuiltinAttributeConstraintDefinition("ElementsAttr"), ElementsAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseAttributeAssemblyFormatResult(
        AttributeConstraintDefinition? definition,
        IAttributeAssemblyFormat assemblyFormat)
    {
        var checkpoint = Mark();
        if (assemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var syntax))
        {
            return ParseResult<AttributeValueSyntax>.Success(syntax!);
        }

        Reset(checkpoint);
        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    private ParseResult<TypeSyntax> TryParseCustomTypeSyntaxResult()
    {
        if (dialectRegistry == null)
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var canonicalName = TryPeekTypeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetType(canonicalName, out var definition) || definition.AssemblyFormat == null)
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(new TypeParsingContext(this), out var syntax))
        {
            return ParseResult<TypeSyntax>.Success(syntax!);
        }

        Reset(checkpoint);
        return ParseResult<TypeSyntax>.NoMatch();
    }

    private ParseResult<TypeSyntax> TryParseBuiltinTypeSyntaxResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        var checkpoint = Mark();

        var functionResult = TryParseFunctionTypeSyntaxResult(stopBefore, stopAtOperationBoundary);
        if (!functionResult.IsNoMatch)
        {
            return functionResult;
        }

        Reset(checkpoint);
        var tupleResult = TryParseTupleTypeSyntaxResult();
        if (!tupleResult.IsNoMatch)
        {
            return tupleResult;
        }

        Reset(checkpoint);
        var tensorResult = TryParseTensorTypeSyntaxResult();
        if (!tensorResult.IsNoMatch)
        {
            return tensorResult;
        }

        Reset(checkpoint);
        var vectorResult = TryParseVectorTypeSyntaxResult();
        if (!vectorResult.IsNoMatch)
        {
            return vectorResult;
        }

        Reset(checkpoint);
        var memRefResult = TryParseMemRefTypeSyntaxResult();
        if (!memRefResult.IsNoMatch)
        {
            return memRefResult;
        }

        Reset(checkpoint);
        return TryParseBuiltinPrimitiveTypeSyntaxResult();
    }

    private ParseResult<TypeSyntax> TryParseBuiltinPrimitiveTypeSyntaxResult()
    {
        if (!Is(TokenKind.Identifier))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var token = ToSyntaxToken(ConsumeToken());
        if (TryParseBuiltinIntegerName(token.Text, out var signedness, out var width))
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinIntegerTypeSyntax(token, signedness, width));
        }

        if (IsBuiltinFloatName(token.Text))
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinFloatTypeSyntax(token));
        }

        if (token.Text == "index")
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinIndexTypeSyntax(token));
        }

        if (token.Text == "none")
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinNoneTypeSyntax(token));
        }

        position--;
        return ParseResult<TypeSyntax>.NoMatch();
    }

    private ParseResult<TypeSyntax> TryParseFunctionTypeSyntaxResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        if (!Is(TokenKind.LParen))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        var inputsResult = TryParseTypeListResult(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary: false);
        if (!inputsResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(inputsResult.Diagnostic!);
        }

        if (!TryMatch(TokenKind.Arrow, out var arrowToken))
        {
            Reset(checkpoint);
            return ParseResult<TypeSyntax>.NoMatch();
        }

        TypeSyntax? resultType = null;
        DelimitedSyntaxList<TypeSyntax> resultTypes;
        if (Is(TokenKind.LParen))
        {
            var resultTypesResult = TryParseTypeListResult(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary);
            if (!resultTypesResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(resultTypesResult.Diagnostic!);
            }

            resultTypes = resultTypesResult.Value;
        }
        else
        {
            resultTypes = new DelimitedSyntaxList<TypeSyntax>(null, [], [], null);
            var resultTypeResult = TryParseTypeSyntaxCoreResult(stopBefore, stopAtOperationBoundary);
            if (!resultTypeResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(resultTypeResult.Diagnostic!);
            }

            resultType = resultTypeResult.Value;
        }

        return ParseResult<TypeSyntax>.Success(new FunctionTypeSyntax(inputsResult.Value, ToSyntaxToken(arrowToken), resultType, resultTypes));
    }

    private ParseResult<TypeSyntax> TryParseTupleTypeSyntaxResult()
    {
        if (!IsKeyword("tuple"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = ExpectKeywordResult("tuple", "Expected 'tuple'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var elementsResult = TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LessThan,
            TokenKind.GreaterThan,
            () => TryParseTypeSyntaxResult(TokenKind.Comma, TokenKind.GreaterThan),
            "Expected '<' after 'tuple'.",
            "Expected '>' to close the tuple type.");
        if (!elementsResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(elementsResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new TupleTypeSyntax(keywordResult.Value, elementsResult.Value.OpenToken!.Value, elementsResult.Value.Items, elementsResult.Value.SeparatorTokens, elementsResult.Value.CloseToken!.Value));
    }

    private ParseResult<TypeSyntax> TryParseTensorTypeSyntaxResult()
    {
        if (!IsKeyword("tensor"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = ExpectKeywordResult("tensor", "Expected 'tensor'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = ExpectTokenResult(TokenKind.LessThan, "Expected '<' after 'tensor'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeResult = TryParseType(elementTypeText, dialectRegistry, out var elementType, out var diagnostic)
            ? ParseResult<TypeSyntax>.Success(elementType!)
            : ParseResult<TypeSyntax>.Failure(diagnostic!);
        if (!elementTypeResult.IsSuccess)
        {
            return elementTypeResult;
        }

        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
            var trailingResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
            if (!trailingResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(trailingResult.Diagnostic!);
            }

            trailingParameters.Add(trailingResult.Value);
        }

        var greaterThanResult = ExpectTokenResult(TokenKind.GreaterThan, "Expected '>' to close the tensor type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new TensorTypeSyntax(keywordResult.Value, lessThanResult.Value, dimensions, xTokens, unrankedToken, elementTypeResult.Value, trailingCommaTokens, trailingParameters, greaterThanResult.Value));
    }

    private ParseResult<TypeSyntax> TryParseVectorTypeSyntaxResult()
    {
        if (!IsKeyword("vector"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        var keywordResult = ExpectKeywordResult("vector", "Expected 'vector'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = ExpectTokenResult(TokenKind.LessThan, "Expected '<' after 'vector'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = TryParseRawUntilDelimiterResult(TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: false, minimumDimensionCount: 1, out var dimensions, out var xTokens, out _, out var elementTypeText))
        {
            Reset(checkpoint);
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeParse = TryParseType(elementTypeText, dialectRegistry, out var elementType, out var diagnostic)
            ? ParseResult<TypeSyntax>.Success(elementType!)
            : ParseResult<TypeSyntax>.Failure(diagnostic!);
        if (!elementTypeParse.IsSuccess)
        {
            return elementTypeParse;
        }

        var greaterThanResult = ExpectTokenResult(TokenKind.GreaterThan, "Expected '>' to close the vector type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new VectorTypeSyntax(keywordResult.Value, lessThanResult.Value, dimensions, xTokens, elementTypeParse.Value, greaterThanResult.Value));
    }

    private ParseResult<TypeSyntax> TryParseMemRefTypeSyntaxResult()
    {
        if (!IsKeyword("memref"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = ExpectKeywordResult("memref", "Expected 'memref'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = ExpectTokenResult(TokenKind.LessThan, "Expected '<' after 'memref'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeParse = TryParseType(elementTypeText, dialectRegistry, out var elementType, out var diagnostic)
            ? ParseResult<TypeSyntax>.Success(elementType!)
            : ParseResult<TypeSyntax>.Failure(diagnostic!);
        if (!elementTypeParse.IsSuccess)
        {
            return elementTypeParse;
        }

        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
            var trailingResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
            if (!trailingResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(trailingResult.Diagnostic!);
            }

            trailingParameters.Add(trailingResult.Value);
        }

        var greaterThanResult = ExpectTokenResult(TokenKind.GreaterThan, "Expected '>' to close the memref type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new MemRefTypeSyntax(keywordResult.Value, lessThanResult.Value, dimensions, xTokens, unrankedToken, elementTypeParse.Value, trailingCommaTokens, trailingParameters, greaterThanResult.Value));
    }

    private ParseException Error(string message)
    {
        return new ParseException(CreateDiagnostic(message));
    }

    private Diagnostic CreateDiagnostic(string message)
    {
        return new Diagnostic(message, Current.Line, Current.Column);
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
        var result = ExpectTokenResult(kind, message);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal SyntaxToken ParseSsaTokenInternal()
    {
        var result = TryParseSsaTokenResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal SyntaxToken ParseBlockLabelTokenInternal()
    {
        var result = TryParseBlockLabelTokenResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal RegionSyntax ParseRegionInternal()
    {
        var result = TryParseRegionResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal NamedAttributeSyntax ParseAttributeInternal()
    {
        var result = TryParseAttributeResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(false, (AttributeDefinition?)null, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(false, expectedDefinitionName, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(false, expectedDefinition, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(true, (AttributeDefinition?)null, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(true, expectedDefinitionName, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(true, expectedDefinition, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal TypeSyntax ParseTypeSyntaxInternal(params TokenKind[] delimiters)
    {
        var result = TryParseTypeSyntaxResult(delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal TypeSyntax ParseTypeSyntaxInternal(string[] stopBeforeKeywords, params TokenKind[] delimiters)
    {
        var result = TryParseTypeSyntaxResult(stopBeforeKeywords, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal TypeSyntax ParseTypeSyntaxUntilOperationBoundaryInternal()
    {
        var result = TryParseTypeSyntaxUntilOperationBoundaryResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal RawSyntaxText ParseRawUntilDelimiterInternal(params TokenKind[] delimiters)
    {
        return ParseRawUntilDelimiter(delimiters);
    }

    internal RawSyntaxText ParseRawUntilDelimiterOrKeywordInternal(string[] keywords, params TokenKind[] delimiters)
    {
        return ParseRawUntilDelimiterOrKeyword(delimiters, keywords);
    }

    internal RawSyntaxText ParseRawUntilOperationBoundaryInternal()
    {
        return ParseRawUntilOperationBoundary();
    }

    internal RawSyntaxText ParseRawUntilDelimiterOrBoundaryInternal(params TokenKind[] delimiters)
    {
        var result = TryParseRawUntilDelimiterOrBoundaryResult(delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDictInternal()
    {
        if (!Is(TokenKind.LBrace))
        {
            return EmptyDelimitedSyntaxList<NamedAttributeSyntax>();
        }

        return ParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBrace,
            TokenKind.RBrace,
            () => ThrowIfFailure(TryParseAttributeResult()),
            "Expected '{' to start the attribute dictionary.",
            "Expected '}' to close the attribute dictionary.");
    }

    internal DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDictWithKeywordInternal()
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, "attributes", System.StringComparison.Ordinal))
        {
            return EmptyDelimitedSyntaxList<NamedAttributeSyntax>();
        }

        ConsumeToken();
        return ParseAttrDictInternal();
    }

    internal SyntaxToken ExpectKeywordInternal(string spelling, string message)
    {
        var result = ExpectKeywordResult(spelling, message);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private ParseResult<SyntaxToken> ExpectKeywordResult(string spelling, string message)
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, spelling, System.StringComparison.Ordinal))
        {
            return ParseResult<SyntaxToken>.Failure(CreateDiagnostic(message));
        }

        return ParseResult<SyntaxToken>.Success(ToSyntaxToken(ConsumeToken()));
    }

    internal IReadOnlyList<RegionSyntax> ParseRegionsInternal()
    {
        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace))
        {
            var result = TryParseRegionResult();
            if (!result.IsSuccess)
            {
                throw new ParseException(result.Diagnostic!);
            }

            regions.Add(result.Value);
        }

        return regions;
    }

    internal DelimitedSyntaxList<SyntaxToken> ParseSuccessorsInternal()
    {
        if (!Is(TokenKind.LBracket))
        {
            return EmptyDelimitedSyntaxList<SyntaxToken>();
        }

        var result = TryParseSuccessorsResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal DelimitedSyntaxList<SyntaxToken> ParseOperandsInternal()
    {
        var result = TryParseOperandsResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal bool IsKeywordInternal(string spelling)
    {
        return Is(TokenKind.Identifier) && string.Equals(Current.Text, spelling, System.StringComparison.Ordinal);
    }

    private SyntaxToken ThrowIfFailure(ParseResult<SyntaxToken> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private T ThrowIfFailure<T>(ParseResult<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
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
        if (Is(TokenKind.Identifier))
        {
            return Current.Text;
        }

        if (Is(TokenKind.Bang))
        {
            var lookahead = position + 1;
            return lookahead < tokens.Count && tokens[lookahead].Kind == TokenKind.Identifier
                ? tokens[lookahead].Text
                : null;
        }

        return null;
    }

    private static string NormalizeOperationName(string name)
    {
        return name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"' ? name.Substring(1, name.Length - 2) : name;
    }

    private Token Current => tokens[position];
}
