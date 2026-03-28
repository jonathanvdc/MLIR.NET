namespace MLIR.Text;

/// <summary>
/// Describes a parser diagnostic at a specific source location.
/// </summary>
public sealed class MlirDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MlirDiagnostic"/> class.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="line">The 1-based source line.</param>
    /// <param name="column">The 1-based source column.</param>
    public MlirDiagnostic(string message, int line, int column)
    {
        Message = message;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the 1-based source line.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the 1-based source column.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Formats the diagnostic as a human-readable string.
    /// </summary>
    /// <returns>The formatted diagnostic text.</returns>
    public override string ToString()
    {
        return $"({Line},{Column}): {Message}";
    }
}
