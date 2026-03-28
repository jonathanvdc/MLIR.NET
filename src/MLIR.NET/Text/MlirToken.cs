namespace MLIR.Text;

/// <summary>
/// Stores a lexical token together with its source span and preserved leading trivia.
/// </summary>
/// <param name="kind">The token kind.</param>
/// <param name="leadingTrivia">The whitespace and comments that precede the token.</param>
/// <param name="text">The token text without leading trivia.</param>
/// <param name="fullStart">The start offset of the leading trivia.</param>
/// <param name="tokenStart">The start offset of the token text.</param>
/// <param name="end">The exclusive end offset of the token text.</param>
/// <param name="line">The 1-based source line of the token text.</param>
/// <param name="column">The 1-based source column of the token text.</param>
internal readonly struct MlirToken(
    MlirTokenKind kind,
    string leadingTrivia,
    string text,
    int fullStart,
    int tokenStart,
    int end,
    int line,
    int column)
{
    /// <summary>
    /// Gets the token kind.
    /// </summary>
    public MlirTokenKind Kind { get; } = kind;

    /// <summary>
    /// Gets the whitespace and comments that precede the token.
    /// </summary>
    public string LeadingTrivia { get; } = leadingTrivia;

    /// <summary>
    /// Gets the token text without leading trivia.
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    /// Gets the start offset of the token including its leading trivia.
    /// </summary>
    public int FullStart { get; } = fullStart;

    /// <summary>
    /// Gets the start offset of the token text.
    /// </summary>
    public int TokenStart { get; } = tokenStart;

    /// <summary>
    /// Gets the exclusive end offset of the token text.
    /// </summary>
    public int End { get; } = end;

    /// <summary>
    /// Gets the 1-based source line of the token text.
    /// </summary>
    public int Line { get; } = line;

    /// <summary>
    /// Gets the 1-based source column of the token text.
    /// </summary>
    public int Column { get; } = column;
}
