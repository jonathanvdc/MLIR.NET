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
    /// Creates a source location from a syntax token.
    /// </summary>
    /// <param name="token">The syntax token.</param>
    /// <returns>The resulting source location.</returns>
    public static SourceLocation FromToken(SyntaxToken token)
    {
        return token.HasSourceLocation ? new SourceLocation(token.Line, token.Column) : default;
    }

    /// <summary>
    /// Returns the source location as text.
    /// </summary>
    /// <returns>The formatted location text.</returns>
    public override string ToString()
    {
        return IsKnown ? $"{Line}:{Column}" : string.Empty;
    }
}
