namespace MLIR.Text;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Provides shared parser access for dialect-specific parsing contexts.
/// </summary>
public abstract class DialectParsingContext
{
    private protected readonly Parser Parser;

    internal DialectParsingContext(Parser parser)
    {
        Parser = parser;
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
    /// Parses raw syntax until one of the supplied delimiters is reached at the outermost nesting level.
    /// </summary>
    public ParseResult<RawSyntaxText> TryParseRawUntilDelimiter(params TokenKind[] delimiters)
    {
        return Parser.TryParseRawUntilDelimiterInternal(delimiters);
    }

    /// <summary>
    /// Parses raw syntax until one of the supplied delimiters or keywords is reached at the outermost nesting level.
    /// </summary>
    public ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrKeyword(string[] keywords, params TokenKind[] delimiters)
    {
        return Parser.TryParseRawUntilDelimiterOrKeywordInternal(keywords, delimiters);
    }

    /// <summary>
    /// Parses raw syntax until an operation boundary is reached.
    /// </summary>
    public ParseResult<RawSyntaxText> TryParseRawUntilOperationBoundary()
    {
        return Parser.TryParseRawUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Parses a nested type syntax node, stopping before any of the supplied delimiters.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseTypeSyntax(params TokenKind[] stopBefore)
    {
        return Parser.TryParseTypeSyntaxInternal(stopBefore);
    }

    /// <summary>
    /// Parses a nested type syntax node, stopping before any of the supplied delimiters or keywords.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseTypeSyntax(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        return Parser.TryParseTypeSyntaxInternal(stopBeforeKeywords, stopBefore);
    }

    /// <summary>
    /// Parses a type syntax node, consuming tokens until an operation boundary is reached.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseTypeSyntaxUntilOperationBoundary()
    {
        return Parser.TryParseTypeSyntaxUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Parses a nested attribute value syntax node, stopping before any of the supplied delimiters.
    /// </summary>
    public ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueSyntaxInternal(stopBefore);
    }

    /// <summary>
    /// Parses a nested attribute value syntax node, preferring the supplied expected attribute definition
    /// when one is known, and stopping before any of the supplied delimiters.
    /// </summary>
    public ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(string expectedDefinitionName, params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueSyntaxInternal(expectedDefinitionName, stopBefore);
    }

    /// <summary>
    /// Parses a nested attribute value syntax node, preferring the supplied expected attribute definition
    /// when one is known, and stopping before any of the supplied delimiters.
    /// </summary>
    public ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(AttributeConstraintDefinition expectedDefinition, params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueSyntaxInternal(expectedDefinition, stopBefore);
    }

    /// <summary>
    /// Parses a named attribute entry.
    /// </summary>
    public ParseResult<NamedAttributeSyntax> TryParseNamedAttributeSyntax()
    {
        return Parser.TryParseAttributeInternal();
    }

    /// <summary>
    /// Parses an attribute dictionary.
    /// </summary>
    public ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttributeDictionarySyntax()
    {
        return Parser.TryParseAttrDictInternal();
    }
}
