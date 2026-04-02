namespace MLIR.Text;

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
    public bool TryMatch(TokenKind kind, out SyntaxToken token)
    {
        if (Parser.TryMatchToken(kind, out var rawToken))
        {
            token = Parser.ToSyntaxToken(rawToken);
            return true;
        }

        token = default;
        return false;
    }

    /// <summary>
    /// Expects a token of the supplied kind.
    /// </summary>
    public SyntaxToken Expect(TokenKind kind, string message)
    {
        return Parser.ExpectTokenInternal(kind, message);
    }

    /// <summary>
    /// Parses raw syntax until one of the supplied delimiters is reached at the outermost nesting level.
    /// </summary>
    public RawSyntaxText ParseRawUntilDelimiter(params TokenKind[] delimiters)
    {
        return Parser.ParseRawUntilDelimiterInternal(delimiters);
    }

    /// <summary>
    /// Parses a nested type syntax node, stopping before any of the supplied delimiters.
    /// </summary>
    public TypeSyntax ParseTypeSyntax(params TokenKind[] stopBefore)
    {
        return Parser.ParseTypeSyntaxInternal(stopBefore);
    }

    /// <summary>
    /// Parses a nested attribute value syntax node, stopping before any of the supplied delimiters.
    /// </summary>
    public AttributeValueSyntax ParseAttributeValueSyntax(params TokenKind[] stopBefore)
    {
        return Parser.ParseAttributeValueSyntaxInternal(stopBefore);
    }

    /// <summary>
    /// Parses a nested attribute value syntax node, preferring the supplied expected attribute definition
    /// when one is known, and stopping before any of the supplied delimiters.
    /// </summary>
    public AttributeValueSyntax ParseAttributeValueSyntax(string expectedDefinitionName, params TokenKind[] stopBefore)
    {
        return Parser.ParseAttributeValueSyntaxInternal(expectedDefinitionName, stopBefore);
    }
}
