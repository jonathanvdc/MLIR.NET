namespace MLIR.Semantics;

using MLIR.Text;

/// <summary>
/// Represents a diagnostic reported while interpreting dialect-specific assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AssemblyDiagnostic"/> class.
/// </remarks>
/// <param name="location">The source location of the diagnostic, if known.</param>
/// <param name="message">The diagnostic message.</param>
public sealed class AssemblyDiagnostic(SourceLocation location, string message)
{
    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the source location of the diagnostic, if known.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
