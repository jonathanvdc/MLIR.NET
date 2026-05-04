namespace MLIR.Text;

using System.Globalization;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Numerics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Provides shared parser access for dialect-specific parsing contexts.
/// </summary>
public readonly struct ParsingContext
{
    private readonly Parser Parser;

    internal ParsingContext(Parser parser)
    {
        Parser = parser;
    }

    /// <summary>
    /// Creates a diagnostic at the current parser position.
    /// </summary>
    public Diagnostic CreateDiagnostic(string message)
    {
        return Parser.CreateDiagnostic(message);
    }

    /// <summary>
    /// Determines whether the current token has the supplied kind.
    /// </summary>
    public bool Is(TokenKind kind)
    {
        return Parser.IsToken(kind);
    }

    /// <summary>
    /// Attempts to match the current token.
    /// </summary>
    public bool TryMatch(TokenKind kind, out Token token)
    {
        return Parser.TryMatchToken(kind, out token);
    }

    /// <summary>
    /// Peeks at a token relative to the current position without consuming it.
    /// </summary>
    internal bool TryPeekToken(int offset, out TokenKind kind, out string text)
    {
        if (Parser.TryPeekToken(offset, out var token))
        {
            kind = token.TokenKind;
            text = token.Text;
            return true;
        }

        kind = default;
        text = string.Empty;
        return false;
    }

    /// <summary>
    /// Expects a token of the supplied kind.
    /// </summary>
    public ParseResult<Token> Expect(TokenKind kind, string message)
    {
        return Parser.ExpectTokenInternal(kind, message);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the current token is an identifier whose text
    /// equals <paramref name="spelling"/> exactly.
    /// </summary>
    public bool IsKeyword(string spelling)
    {
        return Parser.IsKeywordInternal(spelling);
    }

    /// <summary>
    /// Expects an identifier token whose text matches <paramref name="spelling"/> exactly.
    /// </summary>
    public ParseResult<Token> ExpectKeyword(string spelling, string message)
    {
        return Parser.ExpectKeywordInternal(spelling, message);
    }

    /// <summary>
    /// Parses raw syntax until one of the supplied delimiters is reached at the outermost nesting level.
    /// </summary>
    public ParseResult<RawSyntaxText> TryParseRawUntilDelimiter(params TokenKind[] delimiters)
    {
        return Parser.TryParseRawUntilDelimiterInternal(delimiters);
    }

    /// <summary>
    /// Parses a nested type syntax node, stopping before any of the supplied delimiters.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseTypeSyntax(params TokenKind[] stopBefore)
    {
        return Parser.TryParseTypeSyntax();
    }

    /// <summary>
    /// Parses a nested type syntax node, stopping before any of the supplied delimiters or keywords.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseTypeSyntax(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        return Parser.TryParseTypeSyntax();
    }

    /// <summary>
    /// Parses a nested attribute value syntax node, stopping before any of the supplied delimiters.
    /// </summary>
    public ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueInternal(stopBefore);
    }

    /// <summary>
    /// Parses a nested attribute value syntax node, preferring the supplied expected attribute definition
    /// when one is known, and stopping before any of the supplied delimiters.
    /// </summary>
    public ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(AttributeConstraintDefinition expectedDefinition, params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueInternal(expectedDefinition, stopBefore);
    }

    /// <summary>
    /// Parses an attribute dictionary.
    /// </summary>
    public ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttributeDictionarySyntax()
    {
        return Parser.TryParseAttrDictInternal();
    }

    /// <summary>
    /// Tries to match a string literal token and returns it as a
    /// <see cref="StringAttributeValueSyntax"/> with the surrounding double-quotes stripped
    /// and escape sequences resolved. Returns <see cref="ParseResult{T}.NoMatch"/> when the
    /// current token is not a string literal.
    /// </summary>
    /// <remarks>
    /// This helper is the intended target for <c>csharpParser</c> expressions on
    /// <c>StringRefParameter</c>-derived ODS parameter classes:
    /// <code>let csharpParser = "$_parser.TryParseStringLiteralSyntax()";</code>
    /// </remarks>
    public ParseResult<AttributeValueSyntax> TryParseStringLiteralSyntax()
    {
        if (!TryMatch(TokenKind.StringLiteral, out var token))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new StringAttributeValueSyntax(token, StringLiteralAttributeAssemblyFormat.Unescape(token.Text)));
    }

    /// <summary>
    /// Tries to parse a signed integer literal (optionally preceded by <c>+</c> or <c>-</c>)
    /// and returns it as an <see cref="IntegerAttributeValueSyntax"/>.
    /// Returns <see cref="ParseResult{T}.NoMatch"/> when the current position does not start
    /// an integer literal.
    /// </summary>
    /// <remarks>
    /// This helper is the intended target for <c>csharpParser</c> expressions on
    /// <c>APIntParameter</c>-derived ODS parameter classes:
    /// <code>let csharpParser = "$_parser.TryParseIntegerLiteralSyntax()";</code>
    /// </remarks>
    public ParseResult<AttributeValueSyntax> TryParseIntegerLiteralSyntax()
    {
        if (!IntegerLiteralAttributeAssemblyFormat.TryParseSignedIntegerLiteral(this, out var signToken, out var integerToken, out var value))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new IntegerAttributeValueSyntax(
                signToken,
                integerToken,
                ApInt.Parse(64, value.ToString(CultureInfo.InvariantCulture), isSigned: true)));
    }

    /// <summary>
    /// Tries to parse a decimal floating-point literal and returns it as a
    /// <see cref="FloatingPointAttributeValueSyntax"/>.
    /// Returns <see cref="ParseResult{T}.NoMatch"/> when the current position does not start
    /// a floating-point literal.
    /// </summary>
    /// <remarks>
    /// This helper is the intended target for <c>csharpParser</c> expressions on
    /// <c>APFloatParameter</c>-derived ODS parameter classes:
    /// <code>let csharpParser = "$_parser.TryParseFloatingPointLiteralSyntax()";</code>
    /// </remarks>
    public ParseResult<AttributeValueSyntax> TryParseFloatingPointLiteralSyntax()
    {
        return TryParseFloatingPointLiteralSyntax(FloatSemantics.IEEEDouble);
    }

    /// <summary>
    /// Tries to parse a floating-point literal using explicit semantics and returns it as a
    /// <see cref="FloatingPointAttributeValueSyntax"/>.
    /// </summary>
    public ParseResult<AttributeValueSyntax> TryParseFloatingPointLiteralSyntax(FloatSemantics semantics)
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(this, semantics);
    }

    /// <summary>
    /// Re-parses a standalone type text fragment using the current parser's dialect registry.
    /// This is used by shaped builtin type formats after splitting the outer raw body into
    /// dimensions and element-type text.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseStandaloneTypeText(string text)
    {
        return Parser.TryParseStandaloneTypeTextInternal(text);
    }

    /// <summary>
    /// Parses an operation header: optional SSA result list, optional equals token, and operation name token.
    /// </summary>
    public ParseResult<OperationHeader> TryParseOperationHeader()
    {
        return Parser.TryParseOperationHeader();
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
        return Parser.TryParseTypeSyntax();
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
}
