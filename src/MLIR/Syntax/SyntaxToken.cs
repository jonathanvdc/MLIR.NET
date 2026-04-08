using MLIR.Semantics;
using MLIR.Text;

namespace MLIR.Syntax;

/// <summary>
/// Represents a single syntax token together with the trivia that precedes it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SyntaxToken"/> is the leaf node of the concrete syntax tree.
/// It carries the token kind, the token text, the whitespace/comment trivia that precedes it,
/// and an optional reference back to its owning <see cref="SourceDocument"/> together with the
/// character offset and length of the token text.
/// </para>
/// <para>
/// <strong>Synthetic tokens</strong> (tokens created without a source document) have no
/// location information. Use <see cref="SyntaxTokenFactory"/> for convenient construction of
/// synthetic tokens with the correct <see cref="TokenKind"/> already set.
/// </para>
/// <para>
/// <strong>Source-backed tokens</strong> are created by the parser via the internal constructor
/// that accepts a <see cref="SourceDocument"/>. They expose a <see cref="Location"/> that can
/// resolve back to line/column on demand.
/// </para>
/// </remarks>
public readonly struct SyntaxToken
{
    /// <summary>
    /// Initializes a synthetic <see cref="SyntaxToken"/> without source location information.
    /// </summary>
    /// <param name="tokenKind">The lexical kind of the token.</param>
    /// <param name="text">The token text.</param>
    /// <param name="leadingTrivia">The whitespace and comments that precede the token.</param>
    public SyntaxToken(TokenKind tokenKind, string text, string leadingTrivia = "")
    {
        TokenKind = tokenKind;
        Text = text ?? string.Empty;
        LeadingTrivia = leadingTrivia ?? string.Empty;
        Document = null;
        TokenStart = 0;
        TokenLength = 0;
    }

    /// <summary>
    /// Initializes a source-backed <see cref="SyntaxToken"/> with document-relative offset information.
    /// </summary>
    /// <param name="tokenKind">The lexical kind of the token.</param>
    /// <param name="text">The token text.</param>
    /// <param name="leadingTrivia">The whitespace and comments that precede the token.</param>
    /// <param name="document">The source document that owns the token.</param>
    /// <param name="tokenStart">The zero-based start offset of the token text in the document.</param>
    /// <param name="tokenLength">The length of the token text in characters.</param>
    internal SyntaxToken(TokenKind tokenKind, string text, string leadingTrivia, SourceDocument document, int tokenStart, int tokenLength)
    {
        TokenKind = tokenKind;
        Text = text ?? string.Empty;
        LeadingTrivia = leadingTrivia ?? string.Empty;
        Document = document;
        TokenStart = tokenStart;
        TokenLength = tokenLength;
    }

    /// <summary>
    /// Gets the lexical kind of the token.
    /// </summary>
    public TokenKind TokenKind { get; }

    /// <summary>
    /// Gets the whitespace and comments that precede the token.
    /// </summary>
    public string LeadingTrivia { get; }

    /// <summary>
    /// Gets the token text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the source document that owns this token, or <see langword="null"/> for synthetic tokens.
    /// </summary>
    internal SourceDocument? Document { get; }

    /// <summary>
    /// Gets the zero-based start offset of the token text in <see cref="Document"/>.
    /// Only meaningful when <see cref="HasSourceLocation"/> is <see langword="true"/>.
    /// </summary>
    internal int TokenStart { get; }

    /// <summary>
    /// Gets the length of the token text in characters.
    /// Only meaningful when <see cref="HasSourceLocation"/> is <see langword="true"/>.
    /// </summary>
    internal int TokenLength { get; }

    /// <summary>
    /// Gets a value indicating whether the token has source location information.
    /// </summary>
    public bool HasSourceLocation => Document != null;

    /// <summary>
    /// Gets the source location of the token, if known; otherwise, <see cref="SourceLocation.Unknown"/>.
    /// </summary>
    public SourceLocation Location => HasSourceLocation
        ? new SourceLocation(Document!, TokenStart, TokenLength)
        : SourceLocation.Unknown;

    /// <summary>
    /// Gets the complete token text including leading trivia.
    /// </summary>
    public string FullText => LeadingTrivia + Text;

    /// <summary>
    /// Returns a new <see cref="SyntaxToken"/> with the given <paramref name="newText"/> but with
    /// all other fields (token kind, leading trivia and source location) copied from this instance.
    /// </summary>
    /// <param name="newText">The replacement token text.</param>
    /// <returns>
    /// A synthetic token with the new text and the same token kind and leading trivia when this
    /// token has no source location; otherwise a source-backed token pointing at the same span in
    /// the document.
    /// </returns>
    public SyntaxToken WithText(string newText)
    {
        return HasSourceLocation
            ? new SyntaxToken(TokenKind, newText, LeadingTrivia, Document!, TokenStart, TokenLength)
            : new SyntaxToken(TokenKind, newText, LeadingTrivia);
    }

    /// <summary>
    /// Returns the complete token text including leading trivia.
    /// </summary>
    /// <returns>The full token text.</returns>
    public override string ToString()
    {
        return FullText;
    }

}
