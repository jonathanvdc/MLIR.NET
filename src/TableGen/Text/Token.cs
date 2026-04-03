namespace TableGen.Text;

/// <summary>
/// Represents one token emitted by the lexer.
/// </summary>
/// <param name="kind">The token classification.</param>
/// <param name="text">The token payload text after literal decoding where applicable.</param>
/// <param name="position">The absolute character offset in the source text.</param>
/// <param name="line">The 1-based source line number.</param>
/// <param name="column">The 1-based source column number.</param>
internal readonly struct Token(TokenKind kind, string text, int position, int line, int column)
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
    /// Gets the absolute character offset in the source text.
    /// </summary>
    public int Position { get; } = position;

    /// <summary>
    /// Gets the 1-based source line number.
    /// </summary>
    public int Line { get; } = line;

    /// <summary>
    /// Gets the 1-based source column number.
    /// </summary>
    public int Column { get; } = column;
}
