namespace MLIR.Semantics;

using System.Collections.Generic;

/// <summary>
/// Represents the result of semantic verification.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VerificationResult"/> class.
/// </remarks>
/// <param name="diagnostics">The verification diagnostics that were reported.</param>
public sealed class VerificationResult(IReadOnlyList<VerificationDiagnostic> diagnostics)
{
    /// <summary>
    /// Gets the verification diagnostics that were reported.
    /// </summary>
    public IReadOnlyList<VerificationDiagnostic> Diagnostics { get; } = diagnostics;

    /// <summary>
    /// Gets a value indicating whether verification completed without diagnostics.
    /// </summary>
    public bool IsSuccess => Diagnostics.Count == 0;
}
