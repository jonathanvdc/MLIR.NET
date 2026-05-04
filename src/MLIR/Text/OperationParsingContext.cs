namespace MLIR.Text;

using System.Collections.Generic;
using System.Globalization;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Provides dialect-specific parsers controlled access to the MLIR parser.
/// </summary>
public sealed class OperationParsingContext : DialectParsingContext
{
    internal OperationParsingContext(Parser parser)
        : base(parser)
    {
    }

    /// <summary>
    /// Parses an operation header: optional SSA result list, optional equals token, and operation name token.
    /// </summary>
    public ParseResult<OperationParseHeader> TryParseHeader()
    {
        var resultItems = new List<Token>();
        var resultSeparators = new List<Token>();
        Token? equalsToken = null;

        if (Is(TokenKind.SsaName))
        {
            var firstResultTokenResult = TryParseSsaToken();
            if (!firstResultTokenResult.IsSuccess)
            {
                return ParseResult<OperationParseHeader>.Failure(firstResultTokenResult.Diagnostic!);
            }

            var firstResultToken = firstResultTokenResult.Value;
            resultItems.Add(firstResultToken);

            if (TryMatch(TokenKind.Colon, out _))
            {
                var countTokenResult = Expect(TokenKind.Integer, "Expected result count after ':'.");
                if (!countTokenResult.IsSuccess)
                {
                    return ParseResult<OperationParseHeader>.Failure(countTokenResult.Diagnostic!);
                }

                var count = int.Parse(countTokenResult.Value.Text, CultureInfo.InvariantCulture);
                for (var i = 1; i < count; i++)
                {
                    resultItems.Add(TokenFactory.SsaName(firstResultToken.Text + "#" + i.ToString(CultureInfo.InvariantCulture)));
                }
            }

            while (TryMatch(TokenKind.Comma, out var resultCommaToken))
            {
                resultSeparators.Add(resultCommaToken);
                var nextResultToken = TryParseSsaToken();
                if (!nextResultToken.IsSuccess)
                {
                    return ParseResult<OperationParseHeader>.Failure(nextResultToken.Diagnostic!);
                }

                resultItems.Add(nextResultToken.Value);
            }

            var equalsTokenResult = Expect(TokenKind.Equal, "Expected '=' after operation result list.");
            if (!equalsTokenResult.IsSuccess)
            {
                return ParseResult<OperationParseHeader>.Failure(equalsTokenResult.Diagnostic!);
            }

            equalsToken = equalsTokenResult.Value;
        }

        Token nameToken;
        if (TryMatch(TokenKind.Identifier, out var identifierToken))
        {
            nameToken = identifierToken;
        }
        else if (TryMatch(TokenKind.StringLiteral, out var stringLiteralToken))
        {
            nameToken = stringLiteralToken;
        }
        else
        {
            return ParseResult<OperationParseHeader>.Failure(CreateDiagnostic("Expected an operation name."));
        }

        return ParseResult<OperationParseHeader>.Success(new OperationParseHeader(
            nameToken,
            new SeparatedSyntaxList<Token>(resultItems, resultSeparators),
            equalsToken));
    }

    /// <summary>
    /// Parses an SSA value token.
    /// </summary>
    public ParseResult<Token> TryParseSsaToken()
    {
        return Parser.TryParseSsaTokenInternal();
    }

    /// <summary>
    /// Parses a comma-separated list of SSA value tokens, consuming as many as are present.
    /// Returns a successful result with an empty list when the current token is not an SSA name.
    /// Stops as soon as a non-SSA, non-comma token is encountered.
    /// Returns a failed result with a diagnostic if an SSA token that was expected to parse fails.
    /// </summary>
    public ParseResult<SeparatedSyntaxList<Token>> TryParseSsaTokenList()
    {
        return Parser.TryParseSsaTokenListInternal();
    }

    /// <summary>
    /// Parses a nested region.
    /// </summary>
    public ParseResult<RegionSyntax> TryParseRegion()
    {
        return Parser.TryParseRegionInternal();
    }

    /// <summary>
    /// Creates an attribute dictionary for a projected operation body.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> CreateAttributeDictionary(IReadOnlyList<NamedAttributeSyntax> attributes)
    {
        return new DelimitedSyntaxList<NamedAttributeSyntax>(
            attributes.Count > 0 ? TokenFactory.LBrace() : null,
            attributes,
            CreateCommaTokens(attributes.Count),
            attributes.Count > 0 ? TokenFactory.RBrace() : null);
    }

    /// <summary>
    /// Creates comma tokens for a projected delimited list.
    /// </summary>
    public IReadOnlyList<Token> CreateCommaTokens(int itemCount)
    {
        var commas = new List<Token>();
        for (var i = 1; i < itemCount; i++)
        {
            commas.Add(TokenFactory.Comma());
        }

        return commas;
    }

    /// <summary>
    /// Parses a type, consuming tokens until an operation boundary is reached.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseTypeSyntax()
    {
        return Parser.TryParseTypeSyntaxUntilOperationBoundary();
    }

    /// <summary>
    /// Parses a comma-separated list of types, consuming items until an operation boundary is reached.
    /// </summary>
    public IReadOnlyList<TypeSyntax> ParseTypeSyntaxList()
    {
        return Parser.ParseTypeSyntaxListUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Parses an optional attribute dictionary of the form <c>{ name = value, ... }</c>.
    /// Returns an empty list when no opening brace is present.
    /// </summary>
    public ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDict()
    {
        return Parser.TryParseAttrDictInternal();
    }

    /// <summary>
    /// Parses an optional keyword-prefixed attribute dictionary of the form
    /// <c>attributes { name = value, ... }</c>.
    /// Returns an empty list when the <c>attributes</c> keyword is absent.
    /// </summary>
    public ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictWithKeyword()
    {
        return Parser.TryParseAttrDictWithKeywordInternal();
    }

    /// <summary>
    /// Parses zero or more consecutive regions, each delimited by <c>{ ... }</c>.
    /// </summary>
    public ParseResult<IReadOnlyList<RegionSyntax>> TryParseRegions()
    {
        return Parser.TryParseRegionsInternal();
    }

    /// <summary>
    /// Parses an optional successor list of the form <c>[ ^bb1, ^bb2, ... ]</c>.
    /// Returns an empty list when no opening bracket is present.
    /// </summary>
    public ParseResult<DelimitedSyntaxList<Token>> TryParseSuccessors()
    {
        return Parser.TryParseSuccessorsInternal();
    }

    /// <summary>
    /// Parses an operand list of the form <c>( %a, %b, ... )</c>.
    /// </summary>
    public ParseResult<DelimitedSyntaxList<Token>> TryParseOperands()
    {
        return Parser.TryParseOperandsInternal();
    }

}
