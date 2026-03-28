namespace TableGen.Text;

/// <summary>
/// Represents a TableGen parse diagnostic.
/// </summary>
public sealed class TableGenDiagnostic(string message, int line, int column)
{
    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the 1-based line number.
    /// </summary>
    public int Line { get; } = line;

    /// <summary>
    /// Gets the 1-based column number.
    /// </summary>
    public int Column { get; } = column;
}
