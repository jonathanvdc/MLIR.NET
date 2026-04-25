namespace TableGen;

using MLIR.Text;

/// <summary>
/// Represents one source-map segment from preprocessed output back to original source text.
/// </summary>
internal sealed class PreprocessedSourceMapSegment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreprocessedSourceMapSegment"/> class.
    /// </summary>
    public PreprocessedSourceMapSegment(int outputStart, int outputLength, OriginalSourceSpan originalSpan)
    {
        OutputStart = outputStart;
        OutputLength = outputLength;
        OriginalSpan = originalSpan;
    }

    /// <summary>
    /// Gets the zero-based output start offset.
    /// </summary>
    public int OutputStart { get; }

    /// <summary>
    /// Gets the output span length.
    /// </summary>
    public int OutputLength { get; }

    /// <summary>
    /// Gets the exclusive output end offset.
    /// </summary>
    public int OutputEnd => OutputStart + OutputLength;

    /// <summary>
    /// Gets the original source span represented by this output segment.
    /// </summary>
    public OriginalSourceSpan OriginalSpan { get; }
}
