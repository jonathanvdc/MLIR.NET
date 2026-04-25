namespace MLIR.Text;

using System.Collections.Generic;

/// <summary>
/// Represents a user-authored source document and owns its display coordinate mapping.
/// </summary>
/// <remarks>
/// Original documents are the final diagnostic coordinate space. Mapped or preprocessed
/// documents ultimately resolve their locations to <see cref="OriginalSourceSpan"/> values
/// backed by instances of this type.
/// </remarks>
public sealed class OriginalSourceDocument : SourceDocument
{
    private readonly int[] lineStarts;

    /// <summary>
    /// Initializes a new instance of the <see cref="OriginalSourceDocument"/> class.
    /// </summary>
    /// <param name="text">The original source text. A <see langword="null"/> value is treated as an empty string.</param>
    /// <param name="fileName">The logical file name or path associated with the source text, if known.</param>
    public OriginalSourceDocument(string text, string? fileName = null)
        : base(text)
    {
        FileName = fileName;
        lineStarts = ComputeLineStarts(Text);
    }

    /// <summary>
    /// Gets the logical file name or path associated with the original source text, if known.
    /// </summary>
    public override string? FileName { get; }

    /// <inheritdoc/>
    public override ResolvedSourceSpan ResolveSpan(int start, int length)
    {
        var span = new OriginalSourceSpan(this, start, length);
        return new ResolvedSourceSpan(span, new[] { span });
    }

    /// <inheritdoc/>
    public override SourcePosition GetLineColumn(int offset)
    {
        // Clamp the offset to the valid range so callers do not have to guard against
        // off-by-one errors near the end of file.
        if (offset < 0)
        {
            offset = 0;
        }
        else if (offset > Text.Length)
        {
            offset = Text.Length;
        }

        // Binary-search for the largest lineStarts entry that is <= offset.
        var lo = 0;
        var hi = lineStarts.Length - 1;
        while (lo < hi)
        {
            // Use ceiling division so the search converges from the top down and avoids
            // an infinite loop when lo + 1 == hi.
            var mid = lo + (hi - lo + 1) / 2;
            if (lineStarts[mid] <= offset)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        var line = lo + 1;
        var column = offset - lineStarts[lo] + 1;
        return new SourcePosition(FileName, line, column);
    }

    /// <summary>
    /// Builds the sorted array of character offsets at which each line starts.
    /// Line 1 always starts at offset 0. Each newline character causes a new entry
    /// at <c>newlineOffset + 1</c>.
    /// </summary>
    private static int[] ComputeLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                starts.Add(i + 1);
            }
        }

        return starts.ToArray();
    }
}
