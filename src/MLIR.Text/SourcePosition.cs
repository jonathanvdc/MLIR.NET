namespace MLIR.Text;

/// <summary>
/// Represents diagnostic display coordinates in original source text.
/// </summary>
public readonly struct SourcePosition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourcePosition"/> struct.
    /// </summary>
    /// <param name="fileName">The logical file name or path, if known.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <param name="column">The 1-based column number.</param>
    public SourcePosition(string? fileName, int line, int column)
    {
        FileName = fileName;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the logical file name or path, if known.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// Gets the 1-based source line.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the 1-based source column.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets an unknown source position.
    /// </summary>
    public static SourcePosition Unknown => default;
}
