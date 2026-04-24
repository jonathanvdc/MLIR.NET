namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a TableGen !foreach expression.
/// Evaluates body for each element of list, binding the element to varName.
/// </summary>
public sealed class ForeachSyntax(
    string varName,
    ExpressionSyntax list,
    ExpressionSyntax body,
    SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the name of the loop variable bound to each list element.
    /// </summary>
    public string VarName { get; } = varName;

    /// <summary>
    /// Gets the list expression to iterate over.
    /// </summary>
    public ExpressionSyntax List { get; } = list;

    /// <summary>
    /// Gets the body expression evaluated for each element.
    /// </summary>
    public ExpressionSyntax Body { get; } = body;
}
