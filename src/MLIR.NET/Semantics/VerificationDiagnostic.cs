namespace MLIR.Semantics;

/// <summary>
/// Represents a semantic verification diagnostic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VerificationDiagnostic"/> class.
/// </remarks>
/// <param name="operation">The operation that triggered the diagnostic.</param>
/// <param name="message">The diagnostic message.</param>
/// <param name="location">The source location of the diagnostic, if known.</param>
public sealed class VerificationDiagnostic(OperationBase operation, string message, SourceLocation location)
{
    /// <summary>
    /// Gets the operation that triggered the diagnostic.
    /// </summary>
    public OperationBase Operation { get; } = operation;

    /// <summary>
    /// Gets the diagnostic message.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Gets the source location of the diagnostic, if known.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
