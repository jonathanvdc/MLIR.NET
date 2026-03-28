namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Text;
using MLIR.Syntax;

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
    /// Attempts to parse a dialect-specific custom assembly form for the supplied operation.
    /// </summary>
    /// <param name="nameToken">The parsed operation name token.</param>
    /// <param name="resultTokens">The parsed SSA result tokens.</param>
    /// <param name="resultCommaTokens">The parsed comma tokens between results.</param>
    /// <param name="equalsToken">The parsed equals token, if present.</param>
    /// <param name="context">The parsing context.</param>
    /// <param name="operation">When this method returns, contains the parsed operation syntax when custom parsing succeeded.</param>
    /// <returns><see langword="true"/> when a custom assembly form was parsed; otherwise, <see langword="false"/>.</returns>
    bool TryParse(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        OperationParsingContext context,
        out OperationSyntax? operation);

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
