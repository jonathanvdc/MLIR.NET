namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a TableGen !foldl expression.
/// Unlike other bang operators, !foldl binds two variable names (accVar and curVar)
/// that are in scope when the body expression is evaluated.
/// </summary>
public sealed class FoldlSyntax(
    ExpressionSyntax init,
    ExpressionSyntax list,
    string accVar,
    string curVar,
    ExpressionSyntax body,
    SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the initial accumulator value expression.
    /// </summary>
    public ExpressionSyntax Init { get; } = init;

    /// <summary>
    /// Gets the list expression to fold over.
    /// </summary>
    public ExpressionSyntax List { get; } = list;

    /// <summary>
    /// Gets the name of the accumulator variable bound in the body.
    /// </summary>
    public string AccVar { get; } = accVar;

    /// <summary>
    /// Gets the name of the current element variable bound in the body.
    /// </summary>
    public string CurVar { get; } = curVar;

    /// <summary>
    /// Gets the body expression evaluated on each fold iteration.
    /// </summary>
    public ExpressionSyntax Body { get; } = body;
}
