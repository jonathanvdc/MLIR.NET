namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a 1-based source location associated with semantic data.
/// </summary>
public readonly struct SourceLocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceLocation"/> struct.
    /// </summary>
    /// <param name="line">The 1-based source line.</param>
    /// <param name="column">The 1-based source column.</param>
    public SourceLocation(int line, int column)
    {
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the 1-based source line.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the 1-based source column.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets a value indicating whether the location is known.
    /// </summary>
    public bool IsKnown => Line > 0 && Column > 0;

    /// <summary>
    /// Returns the source location as text.
    /// </summary>
    /// <returns>The formatted location text.</returns>
    public override string ToString()
    {
        return IsKnown ? $"{Line}:{Column}" : string.Empty;
    }

    /// <summary>
    /// Gets an unknown source location.
    /// </summary> <remarks>
    /// An unknown source location is represented by a default instance with line and column set to zero.
    /// </remarks>
    public static SourceLocation Unknown => default;
}
