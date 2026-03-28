namespace MLIR.Syntax;

/// <summary>
/// Represents a single syntax token together with the trivia that precedes it.
/// </summary>
public readonly struct SyntaxToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxToken"/> class.
    /// </summary>
    /// <param name="text">The token text.</param>
    /// <param name="leadingTrivia">The whitespace and comments that precede the token.</param>
    /// <param name="line">The 1-based source line of the token text, if known.</param>
    /// <param name="column">The 1-based source column of the token text, if known.</param>
    public SyntaxToken(string text, string leadingTrivia = "", int line = 0, int column = 0)
    {
        Text = text ?? string.Empty;
        LeadingTrivia = leadingTrivia ?? string.Empty;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the whitespace and comments that precede the token.
    /// </summary>
    public string LeadingTrivia { get; }

    /// <summary>
    /// Gets the token text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the 1-based source line of the token text, if known.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the 1-based source column of the token text, if known.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets a value indicating whether the token has source location information.
    /// </summary>
    public bool HasSourceLocation => Line > 0 && Column > 0;

    /// <summary>
    /// Gets the complete token text including leading trivia.
    /// </summary>
    public string FullText => LeadingTrivia + Text;

    /// <summary>
    /// Returns the complete token text including leading trivia.
    /// </summary>
    /// <returns>The full token text.</returns>
    public override string ToString()
    {
        return FullText;
    }
}
