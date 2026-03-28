namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Text;
using MLIR.Syntax;

/// <summary>
/// Parses, binds, and prints a dialect-specific operation assembly format.
/// </summary>
/// <remarks>
/// Generic operation syntax and custom operation bodies both preserve source text.
/// Implementations can opt into custom concrete syntax while still projecting the same
/// semantic operation shape for binding and verification.
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
    /// <param name="body">When this method returns, contains the parsed custom operation body when custom parsing succeeded.</param>
    /// <returns><see langword="true"/> when a custom assembly form was parsed; otherwise, <see langword="false"/>.</returns>
    bool TryParse(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        OperationParsingContext context,
        out CustomOperationBodySyntax? body);

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
