namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rewrites a dialect-specific operation assembly format.
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
    /// <returns>The parsed operation body, a no-match result, or a diagnostic-producing failure.</returns>
    ParseResult<OperationBodySyntax> TryParse(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        OperationParsingContext context);

    /// <summary>
    /// Interprets the supplied concrete syntax tree in the assembly format into semantic properties.
    /// </summary>
    /// <param name="syntax">The operation syntax to interpret.</param>
    /// <param name="definition">The operation definition.</param>
    /// <param name="binder">The binding context.</param>
    /// <returns>The interpreted operation.</returns>
    Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied operation.
    /// </summary>
    /// <param name="operation">The operation to rewrite.</param>
    /// <param name="context">The CST transformation context.</param>
    /// <returns>The custom assembly operation syntax.</returns>
    OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context);
}
