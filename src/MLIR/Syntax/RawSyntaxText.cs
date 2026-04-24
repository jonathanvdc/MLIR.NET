using MLIR.Semantics;

namespace MLIR.Syntax;

using MLIR.Text;

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
    {
        Tokens = [TokenFactory.Identifier(text)];
        Text = text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RawSyntaxText"/> class.
    /// </summary>
    /// <param name="tokens">The syntax tokens that comprise the preserved text.</param>
    public RawSyntaxText(IReadOnlyList<Token> tokens)
    {
        Tokens = tokens;
        if (tokens.Count == 0)
        {
            Text = string.Empty;
        }
        else
        {
            Text = tokens[0].Text + string.Concat(tokens.Skip(1).Select(t => t.FullText));
        }
    }

    internal RawSyntaxText(IReadOnlyList<Token> tokens, string text)
    {
        Tokens = tokens;
        Text = text;
    }

    /// <summary>
    /// Gets the syntax tokens that comprise the preserved text.
    /// The first token's leading trivia is considered the leading trivia for the entire text.
    /// The concatenated full text of all tokens is considered the full text of the raw syntax.
    /// The concatenated text without leading trivia of all tokens is considered the text of the raw syntax.
    /// This design allows for flexible preservation of arbitrary syntax fragments while still providing access to structured token information when needed.
    /// For example, a custom operation assembly format could preserve the entire operation syntax as raw text while still allowing access to individual tokens for binding and diagnostics.
    /// </summary>
    /// <remarks>
    /// The raw syntax text is intended to preserve the original source text for unrecognized or uninterpreted syntax fragments, such as custom operation assembly forms or unrecognized attributes.
    /// Consumers can choose to preserve the raw syntax text as-is, or they can analyze and manipulate the underlying tokens as needed for binding, diagnostics, or transformations.
    /// </remarks>
    public IReadOnlyList<Token> Tokens { get; }

    /// <summary>
    /// Gets the whitespace and comments that precede the text.
    /// </summary>
    public string? LeadingTrivia => Tokens.Count == 0 ? null : Tokens[0].LeadingTrivia;

    /// <summary>
    /// Gets the preserved syntax text without leading trivia.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the complete raw syntax text including leading trivia.
    /// </summary>
    public string FullText => (LeadingTrivia ?? string.Empty) + Text;

    /// <summary>
    /// Gets a value indicating whether the raw text already has explicit leading trivia.
    /// </summary>
    public bool HasLeadingTrivia => Tokens.Count > 0 && Tokens[0].HasLeadingTrivia;

    /// <summary>
    /// Gets the merged source location spanning from the first to the last token.
    /// Returns an unknown location when no tokens are present or no token has a known location.
    /// </summary>
    public SourceLocation Location
    {
        get
        {
            var result = SourceLocation.Unknown;
            foreach (var token in Tokens)
            {
                result = SourceLocation.Merge(result, token.Location);
            }

            return result;
        }
    }

    /// <summary>
    /// Returns the preserved syntax text.
    /// </summary>
    /// <returns>The underlying text.</returns>
    public override string ToString()
    {
        return FullText;
    }
}
