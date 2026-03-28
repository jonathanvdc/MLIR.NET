namespace MLIR.Abstractions;

using MLIR.Syntax;

/// <summary>
/// Defines an extension point for dialect-specific handling on top of generic MLIR syntax.
/// </summary>
public interface IMlirDialect
{
    /// <summary>
    /// Determines whether this dialect can interpret the supplied operation.
    /// </summary>
    /// <param name="operation">The operation to inspect.</param>
    /// <returns><see langword="true"/> when the dialect recognizes the operation; otherwise, <see langword="false"/>.</returns>
    bool CanHandle(OperationSyntax operation);
}
