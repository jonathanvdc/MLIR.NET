namespace MLIR.Text;

/// <summary>
/// Describes a parser diagnostic at a specific source location.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Diagnostic"/> class.
/// </remarks>
/// <param name="message">The diagnostic message.</param>
/// <param name="line">The 1-based source line.</param>
/// <param name="column">The 1-based source column.</param>
public sealed class Diagnostic(string message, int line, int column)
{
    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the 1-based source line.
    /// </summary>
    public int Line { get; } = line;

    /// <summary>
    /// Gets the 1-based source column.
    /// </summary>
    public int Column { get; } = column;

    /// <summary>
    /// Formats the diagnostic as a human-readable string.
    /// </summary>
    /// <returns>The formatted diagnostic text.</returns>
    public override string ToString()
    {
        return $"({Line},{Column}): {Message}";
    }
}
