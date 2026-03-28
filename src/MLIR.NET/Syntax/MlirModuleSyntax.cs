namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents the top-level generic MLIR syntax tree.
/// </summary>
public sealed class MlirModuleSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MlirModuleSyntax"/> class.
    /// </summary>
    /// <param name="operations">The top-level operations in the module.</param>
    public MlirModuleSyntax(IReadOnlyList<OperationSyntax> operations)
    {
        Operations = operations;
    }

    /// <summary>
    /// Gets the top-level operations in the module.
    /// </summary>
    public IReadOnlyList<OperationSyntax> Operations { get; }
}
