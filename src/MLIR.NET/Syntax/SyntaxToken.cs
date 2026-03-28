namespace MLIR.Syntax;

/// <summary>
/// Represents a single syntax token together with the trivia that precedes it.
/// </summary>
public sealed class SyntaxToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxToken"/> class.
    /// </summary>
    /// <param name="text">The token text.</param>
    /// <param name="leadingTrivia">The whitespace and comments that precede the token.</param>
    public SyntaxToken(string text, string leadingTrivia = "")
    {
        Text = text;
        LeadingTrivia = leadingTrivia;
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
