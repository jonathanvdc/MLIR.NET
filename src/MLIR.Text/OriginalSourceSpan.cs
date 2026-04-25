namespace MLIR.Text;

/// <summary>
/// Represents a span in user-authored original source text.
/// </summary>
/// <remarks>
/// Unlike <see cref="SourceLocation"/>, this type is not relative to an arbitrary parsed
/// source view. It always points at an <see cref="OriginalSourceDocument"/>.
/// </remarks>
public readonly struct OriginalSourceSpan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OriginalSourceSpan"/> struct.
    /// </summary>
    /// <param name="document">The original source document that owns the span.</param>
    /// <param name="start">The zero-based start offset within <paramref name="document"/>.</param>
    /// <param name="length">The span length within <paramref name="document"/>.</param>
    public OriginalSourceSpan(OriginalSourceDocument document, int start, int length)
    {
        Document = document;
        Start = start;
        Length = length;
    }

    /// <summary>
    /// Gets the original source document that owns the span.
    /// </summary>
    public OriginalSourceDocument Document { get; }

    /// <summary>
    /// Gets the zero-based start offset within <see cref="Document"/>.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the length of the span in characters.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the exclusive end offset within <see cref="Document"/>.
    /// </summary>
    public int End => Start + Length;
}
