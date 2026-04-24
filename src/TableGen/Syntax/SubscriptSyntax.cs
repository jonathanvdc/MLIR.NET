namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a subscript expression such as <c>values[i]</c>.
/// </summary>
public sealed class SubscriptSyntax(ExpressionSyntax target, ExpressionSyntax index, SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the indexed expression.
    /// </summary>
    public ExpressionSyntax Target { get; } = target;

    /// <summary>
    /// Gets the index expression.
    /// </summary>
    public ExpressionSyntax Index { get; } = index;
}
