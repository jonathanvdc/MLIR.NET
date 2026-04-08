namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a source span associated with semantic data, backed by a document-relative
/// character offset and length rather than storing line/column directly.
/// </summary>
/// <remarks>
/// <para>
/// Line and column numbers are derived on demand through the owning <see cref="SourceDocument"/>
/// rather than being stored in every instance. This centralises the line-mapping logic and
/// makes it easy to translate any offset back to a human-readable location at diagnostic time.
/// </para>
/// <para>
/// An <em>unknown</em> location is represented by the <see langword="default"/> value (i.e., a
/// <see langword="null"/> document). Use <see cref="Unknown"/> to obtain one explicitly.
/// </para>
/// </remarks>
public readonly struct SourceLocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceLocation"/> struct with a known
    /// document and character span.
    /// </summary>
    /// <param name="document">The source document that owns the span.</param>
    /// <param name="start">The zero-based start offset of the span.</param>
    /// <param name="length">The length of the span in characters.</param>
    public SourceLocation(SourceDocument document, int start, int length)
    {
        Document = document;
        Start = start;
        Length = length;
    }

    /// <summary>
    /// Gets the source document that owns this span, or <see langword="null"/> when the
    /// location is unknown.
    /// </summary>
    public SourceDocument? Document { get; }

    /// <summary>
    /// Gets the zero-based start offset of the span within <see cref="Document"/>.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the length of the span in characters.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the exclusive end offset of the span within <see cref="Document"/>
    /// (i.e., <c>Start + Length</c>).
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Gets the 1-based source line of the span start, or zero when the location is unknown.
    /// </summary>
    public int Line => Document?.GetLineColumn(Start).Line ?? 0;

    /// <summary>
    /// Gets the 1-based source column of the span start, or zero when the location is unknown.
    /// </summary>
    public int Column => Document?.GetLineColumn(Start).Column ?? 0;

    /// <summary>
    /// Gets a value indicating whether the location is known (i.e., backed by a source document).
    /// </summary>
    public bool IsKnown => Document != null;

    /// <summary>
    /// Returns the source location as a human-readable <c>line:column</c> string,
    /// or an empty string when the location is unknown.
    /// </summary>
    /// <returns>The formatted location text.</returns>
    public override string ToString()
    {
        return IsKnown ? $"{Line}:{Column}" : string.Empty;
    }

    /// <summary>
    /// Gets an unknown source location (no document, no span).
    /// </summary>
    /// <remarks>
    /// Equivalent to the <see langword="default"/> value of <see cref="SourceLocation"/>.
    /// </remarks>
    public static SourceLocation Unknown => default;

    /// <summary>
    /// Merges two source locations into a single span that covers both.
    /// </summary>
    /// <param name="first">The first location to merge.</param>
    /// <param name="second">The second location to merge.</param>
    /// <returns>
    /// A <see cref="SourceLocation"/> whose span runs from the earliest start to the latest
    /// end of the two input spans. If either location is unknown, the other is returned
    /// unchanged. If both locations are backed by different source documents,
    /// <paramref name="first"/> is returned unchanged. If both are unknown,
    /// <see cref="Unknown"/> is returned.
    /// </returns>
    public static SourceLocation Merge(SourceLocation first, SourceLocation second)
    {
        if (!first.IsKnown) return second;
        if (!second.IsKnown) return first;
        if (!ReferenceEquals(first.Document, second.Document)) return first;
        var start = Math.Min(first.Start, second.Start);
        var end = Math.Max(first.End, second.End);
        return new SourceLocation(first.Document!, start, end - start);
    }
}
