namespace MLIR.Dialects;

using MLIR.Semantics;

/// <summary>
/// Verifies semantic operations recognized by a dialect.
/// </summary>
public interface IOperationVerifier
{
    /// <summary>
    /// Verifies the supplied operation and reports diagnostics to the context.
    /// </summary>
    /// <param name="operation">The operation to verify.</param>
    /// <param name="context">The verification context used to report diagnostics.</param>
    void Verify(OperationBase operation, VerificationContext context);
}
