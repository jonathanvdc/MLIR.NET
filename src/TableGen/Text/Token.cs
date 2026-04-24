namespace TableGen.Text;

using MLIR.Text;

/// <summary>
/// Represents one token emitted by the lexer.
/// </summary>
/// <param name="kind">The token classification.</param>
/// <param name="text">The token payload text after literal decoding where applicable.</param>
/// <param name="location">The source location of the token.</param>
internal readonly struct Token(TokenKind kind, string text, SourceLocation location)
{
    /// <summary>
    /// Gets the token classification.
    /// </summary>
    public TokenKind Kind { get; } = kind;

    /// <summary>
    /// Gets the token payload text.
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    /// Gets the source location of the token.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
