namespace MLIR.Semantics;

using System.Collections.Generic;

/// <summary>
/// Accumulates verification diagnostics for a single operation.
/// </summary>
public sealed class VerificationContext
{
    private readonly List<VerificationDiagnostic> diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationContext"/> class.
    /// </summary>
    /// <param name="operation">The operation being verified.</param>
    /// <param name="diagnostics">The shared diagnostic sink.</param>
    public VerificationContext(Operation operation, List<VerificationDiagnostic> diagnostics)
    {
        Operation = operation;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the operation being verified.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Reports a verification diagnostic for the current operation.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    public void Report(string message)
    {
        diagnostics.Add(new VerificationDiagnostic(Operation, message, Operation.Location));
    }
}
