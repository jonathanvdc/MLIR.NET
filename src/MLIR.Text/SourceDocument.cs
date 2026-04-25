namespace MLIR.Text;

/// <summary>
/// Represents a source text buffer that can be consumed by a lexer or parser.
/// </summary>
/// <remarks>
/// <para>
/// A source document may be an original user-authored file or a derived view such as
/// preprocessed text. <see cref="SourceLocation"/> values store offsets relative to this
/// document's <see cref="Text"/>, while document implementations decide how those offsets
/// resolve back to original source coordinates for diagnostics.
/// </para>
/// <para>
/// Public position lookup returns diagnostic coordinates. For mapped documents, those
/// coordinates may come from an original document rather than from this document's own text.
/// </para>
/// </remarks>
public abstract class SourceDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDocument"/> class for the given text.
    /// </summary>
    /// <param name="text">The text buffer. A <see langword="null"/> value is treated as an empty string.</param>
    protected SourceDocument(string text)
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Gets the text buffer consumed by the lexer/parser.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the logical file name or path associated with the diagnostic position at the start
    /// of this document, if known.
    /// </summary>
    public virtual string? FileName => GetLineColumn(0).FileName;

    /// <summary>
    /// Gets the length of the document text in characters.
    /// </summary>
    public int Length => Text.Length;

    /// <summary>
    /// Resolves a span in this document's text to one or more spans in original source text.
    /// </summary>
    /// <param name="start">The zero-based start offset within <see cref="Text"/>.</param>
    /// <param name="length">The span length within <see cref="Text"/>.</param>
    /// <returns>The resolved original-source span information.</returns>
    public abstract ResolvedSourceSpan ResolveSpan(int start, int length);

    /// <summary>
    /// Returns the diagnostic file, line, and column for the given zero-based offset.
    /// </summary>
    /// <param name="offset">The zero-based offset within <see cref="Text"/>.</param>
    /// <returns>The resolved diagnostic position.</returns>
    public abstract SourcePosition GetLineColumn(int offset);
}
