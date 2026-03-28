namespace MLIR.Syntax;

/// <summary>
/// Stores a fragment of MLIR syntax that is preserved as raw text.
/// </summary>
public sealed class RawSyntaxText
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawSyntaxText"/> class.
    /// </summary>
    /// <param name="text">The preserved syntax text without leading trivia.</param>
    public RawSyntaxText(string text)
        : this(text, string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RawSyntaxText"/> class.
    /// </summary>
    /// <param name="text">The preserved syntax text without leading trivia.</param>
    /// <param name="leadingTrivia">The whitespace and comments that precede the text.</param>
    public RawSyntaxText(string text, string leadingTrivia)
    {
        Text = text;
        LeadingTrivia = leadingTrivia;
    }

    /// <summary>
    /// Gets the whitespace and comments that precede the text.
    /// </summary>
    public string LeadingTrivia { get; }

    /// <summary>
    /// Gets the preserved syntax text without leading trivia.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the complete raw syntax text including leading trivia.
    /// </summary>
    public string FullText => LeadingTrivia + Text;

    /// <summary>
    /// Gets a value indicating whether the raw text already has explicit leading trivia.
    /// </summary>
    public bool HasLeadingTrivia => LeadingTrivia.Length > 0;

    /// <summary>
    /// Returns the preserved syntax text.
    /// </summary>
    /// <returns>The underlying text.</returns>
    public override string ToString()
    {
        return FullText;
    }
}
