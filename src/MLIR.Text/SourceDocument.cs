namespace MLIR.Text;

using System.Collections.Generic;

/// <summary>
/// Represents a source text document and provides efficient offset-to-line/column mapping.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SourceDocument"/> is the single owner of the original source text for a parse.
/// All <see cref="SourceLocation"/> values derived from a parse point back to their owning
/// document so that line/column can be recomputed on demand rather than stored directly in
/// every token.
/// </para>
/// <para>
/// The line-start table is built once during construction (O(n) in source length) and stored in
/// a sorted array. Subsequent offset-to-line/column lookups run in O(log n) using binary search.
/// </para>
/// </remarks>
public sealed class SourceDocument
{
    private readonly int[] lineStarts;

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDocument"/> class for the given source text.
    /// </summary>
    /// <param name="text">The full source text. A <see langword="null"/> value is treated as an empty string.</param>
    public SourceDocument(string text)
    {
        Text = text ?? string.Empty;
        lineStarts = ComputeLineStarts(Text);
    }

    /// <summary>
    /// Gets the full source text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the length of the source text in characters.
    /// </summary>
    public int Length => Text.Length;

    /// <summary>
    /// Returns the 1-based line and column numbers for the given zero-based character offset.
    /// </summary>
    /// <param name="offset">
    /// The zero-based character offset into <see cref="Text"/>.
    /// Clamped to the valid range <c>[0, Length]</c>.
    /// </param>
    /// <returns>
    /// A <c>(Line, Column)</c> tuple with both components 1-based,
    /// suitable for display in diagnostics and error messages.
    /// </returns>
    public (int Line, int Column) GetLineColumn(int offset)
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

        var line = lo + 1;        // Convert to 1-based line
        var column = offset - lineStarts[lo] + 1; // Convert to 1-based column
        return (line, column);
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
