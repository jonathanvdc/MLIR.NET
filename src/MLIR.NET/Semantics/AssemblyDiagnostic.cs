namespace MLIR.Semantics;

/// <summary>
/// Represents a diagnostic reported while interpreting dialect-specific assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AssemblyDiagnostic"/> class.
/// </remarks>
/// <param name="operation">The operation that triggered the diagnostic.</param>
/// <param name="message">The diagnostic message.</param>
public sealed class AssemblyDiagnostic(Operation operation, string message)
{
    /// <summary>
    /// Gets the operation that triggered the diagnostic.
    /// </summary>
    public Operation Operation { get; } = operation;

    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; } = message;
}
