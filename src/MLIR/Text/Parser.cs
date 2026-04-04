namespace MLIR.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;

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
        if (!nameToken.Text.StartsWith("\"", System.StringComparison.Ordinal))
        {
            var customBodyResult = TryParseCustomAssemblyResult(nameToken, resultTokens, resultCommaTokens, equalsToken);
            if (customBodyResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Success(new OperationSyntax(
                    resultTokens,
                    resultCommaTokens,
                    equalsToken,
                    nameToken,
                    customBodyResult.Value));
            }

            if (customBodyResult.IsError)
            {
                return ParseResult<OperationSyntax>.Failure(customBodyResult.Diagnostic!);
            }
        }

        if (!nameToken.Text.StartsWith("\"", System.StringComparison.Ordinal))
        {
            var projectedBodyResult = TryParseProjectedCustomLikeOperationBodyResult();
            if (projectedBodyResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Success(new OperationSyntax(
                    resultTokens,
                    resultCommaTokens,
                    equalsToken,
                    nameToken,
                    projectedBodyResult.Value));
            }
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

    private ParseResult<OperationBodySyntax> TryParseProjectedCustomLikeOperationBodyResult()
    {
        var checkpoint = Mark();

        var operandTokens = new List<SyntaxToken>();
        var operandCommaTokens = new List<SyntaxToken>();
        if (Is(TokenKind.SsaName))
        {
            var firstOperandResult = TryParseSsaTokenResult();
            if (!firstOperandResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(firstOperandResult.Diagnostic!);
            }

            operandTokens.Add(firstOperandResult.Value);
            while (TryMatch(TokenKind.Comma, out var comma))
            {
                operandCommaTokens.Add(ToSyntaxToken(comma));
                var operandResult = TryParseSsaTokenResult();
                if (!operandResult.IsSuccess)
                {
                    return ParseResult<OperationBodySyntax>.Failure(operandResult.Diagnostic!);
                }

                operandTokens.Add(operandResult.Value);
            }
        }

        var attributeDictResult = TryParseAttrDictResult();
        if (!attributeDictResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(attributeDictResult.Diagnostic!);
        }

        if (!TryMatch(TokenKind.Colon, out var colonToken))
        {
            Reset(checkpoint);
            return ParseResult<OperationBodySyntax>.NoMatch();
        }

        var typeSignatureResult = TryParseRawUntilOperationBoundaryResult();
        if (!typeSignatureResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(typeSignatureResult.Diagnostic!);
        }

        return ParseResult<OperationBodySyntax>.Success(new GenericOperationBodySyntax(
            new DelimitedSyntaxList<SyntaxToken>(
                new SyntaxToken("("),
                operandTokens,
                operandCommaTokens,
                new SyntaxToken(")")),
            new DelimitedSyntaxList<SyntaxToken>(null, new List<SyntaxToken>(), new List<SyntaxToken>(), null),
            new List<RegionSyntax>(),
            attributeDictResult.Value,
            ToSyntaxToken(colonToken),
            new RawTypeSyntax(typeSignatureResult.Value)));
    }

    private ParseResult<OperationBodySyntax> TryParseCustomAssemblyResult(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken)
    {
        if (dialectRegistry == null)
        {
            return ParseResult<OperationBodySyntax>.NoMatch();
        }

        var normalizedName = NormalizeOperationName(nameToken.Text);
        if (!dialectRegistry.TryGetOperationForParsing(normalizedName, out var definition) || definition.AssemblyFormat == null)
        {
            return ParseResult<OperationBodySyntax>.NoMatch();
        }

        var checkpoint = Mark();
        var result = definition.AssemblyFormat.TryParse(
            nameToken,
            resultTokens,
            resultCommaTokens,
            equalsToken,
            new OperationParsingContext(this));
        if (result.IsSuccess || result.IsError)
        {
            return result;
        }

        Reset(checkpoint);
        return ParseResult<OperationBodySyntax>.NoMatch();
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
}
