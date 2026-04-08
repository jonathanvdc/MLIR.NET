namespace MLIR.Text;

using System.Collections.Generic;
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
    /// Parses a block label token.
    /// </summary>
    public ParseResult<Token> TryParseBlockLabelToken()
    {
        return Parser.TryParseBlockLabelTokenInternal();
    }

    /// <summary>
    /// Parses a nested region.
    /// </summary>
    public ParseResult<RegionSyntax> TryParseRegion()
    {
        return Parser.TryParseRegionInternal();
    }

    /// <summary>
    /// Parses a named attribute.
    /// </summary>
    public ParseResult<NamedAttributeSyntax> TryParseAttribute()
    {
        return Parser.TryParseAttributeInternal();
    }

    /// <summary>
    /// Parses raw syntax until the end of the current operation.
    /// </summary>
    public new ParseResult<RawSyntaxText> TryParseRawUntilOperationBoundary()
    {
        return Parser.TryParseRawUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Creates an empty operand list for a generic operation projection.
    /// </summary>
    public DelimitedSyntaxList<Token> CreateEmptyOperandList()
    {
        return new DelimitedSyntaxList<Token>(TokenFactory.LParen(), new List<Token>(), new List<Token>(), TokenFactory.RParen());
    }

    /// <summary>
    /// Creates an empty successor list for a generic operation projection.
    /// </summary>
    public DelimitedSyntaxList<Token> CreateEmptySuccessorList()
    {
        return new DelimitedSyntaxList<Token>(null, new List<Token>(), new List<Token>(), null);
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
    /// Returns <see langword="true"/> when the current token is an identifier whose text
    /// equals <paramref name="spelling"/> (case-sensitive, exact match).
    /// </summary>
    public bool IsKeyword(string spelling)
    {
        return Parser.IsKeywordInternal(spelling);
    }

    /// <summary>
    /// Parses a type, consuming tokens until an operation boundary is reached.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseTypeSyntax()
    {
        return Parser.TryParseTypeSyntaxUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Parses a comma-separated list of types, consuming items until an operation boundary is reached.
    /// </summary>
    public IReadOnlyList<TypeSyntax> ParseTypeSyntaxList()
    {
        return Parser.ParseTypeSyntaxListUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Parses an attribute value, stopping before any of the supplied delimiter tokens or
    /// an operation boundary, whichever comes first.
    /// </summary>
    public new ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueSyntaxOrBoundaryInternal(stopBefore);
    }

    /// <summary>
    /// Parses an attribute value, preferring the supplied expected attribute definition and
    /// stopping before any of the supplied delimiter tokens or an operation boundary, whichever comes first.
    /// </summary>
    // This intentionally shadows the base overload: operation custom assembly parsing must also
    // stop at operation boundaries (for example a newline ending the op), not just explicit delimiters.
    public new ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(string expectedDefinitionName, params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueSyntaxOrBoundaryInternal(expectedDefinitionName, stopBefore);
    }

    /// <summary>
    /// Parses an attribute value, preferring the supplied expected attribute definition and
    /// stopping before any of the supplied delimiter tokens or an operation boundary, whichever comes first.
    /// </summary>
    public new ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(AttributeConstraintDefinition expectedDefinition, params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueSyntaxOrBoundaryInternal(expectedDefinition, stopBefore);
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
    /// Expects an identifier token whose text matches <paramref name="spelling"/> exactly.
    /// </summary>
    public ParseResult<Token> ExpectKeyword(string spelling, string message)
    {
        return Parser.ExpectKeywordInternal(spelling, message);
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
