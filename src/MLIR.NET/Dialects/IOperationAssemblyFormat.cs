namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Prints a semantic operation using a dialect-specific assembly format.
/// </summary>
/// <remarks>
/// The generic parser and printer remain the source of truth for syntax preservation.
/// Custom assembly format implementations are an opt-in printing layer over semantic operations.
/// </remarks>
public interface IOperationAssemblyFormat
{
    /// <summary>
    /// Interprets the generic concrete syntax of the supplied operation into semantic properties.
    /// </summary>
    /// <param name="operation">The operation to interpret.</param>
    /// <param name="context">The binding context.</param>
    void Bind(Operation operation, OperationAssemblyBindingContext context);

    /// <summary>
    /// Prints the supplied operation.
    /// </summary>
    /// <param name="operation">The operation to print.</param>
    /// <param name="context">The printing context.</param>
    void Print(Operation operation, OperationPrintingContext context);
}
