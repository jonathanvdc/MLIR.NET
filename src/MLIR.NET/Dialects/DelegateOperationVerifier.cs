namespace MLIR.Dialects;

using System;
using MLIR.Semantics;

/// <summary>
/// Adapts a delegate to <see cref="IOperationVerifier"/> for lightweight registrations.
/// </summary>
public sealed class DelegateOperationVerifier : IOperationVerifier
{
    private readonly Action<Operation, VerificationContext> action;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateOperationVerifier"/> class.
    /// </summary>
    /// <param name="action">The delegate to invoke when verifying an operation.</param>
    public DelegateOperationVerifier(Action<Operation, VerificationContext> action)
    {
        this.action = action;
    }

    /// <inheritdoc/>
    public void Verify(Operation operation, VerificationContext context)
    {
        action(operation, context);
    }
}
