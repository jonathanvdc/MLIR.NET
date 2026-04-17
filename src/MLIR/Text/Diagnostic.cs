using MLIR.Semantics;

namespace MLIR.Text;

/// <summary>
/// Describes a parser diagnostic at a specific source location.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Diagnostic"/> class.
/// </remarks>
/// <param name="message">The diagnostic message.</param>
/// <param name="location">The source location associated with the diagnostic.</param>
public sealed class Diagnostic(string message, SourceLocation location)
{
    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the source location associated with the diagnostic.
    /// </summary>
    public SourceLocation Location { get; } = location;

    /// <summary>
    /// Gets the 1-based source line.
    /// </summary>
    public int Line => Location.Line;

    /// <summary>
    /// Gets the 1-based source column.
    /// </summary>
    public int Column => Location.Column;

    /// <summary>
    /// Formats the diagnostic as a human-readable string.
    /// </summary>
    /// <returns>The formatted diagnostic text.</returns>
    public override string ToString()
    {
        return $"({Line},{Column}): {Message}";
    }
}
