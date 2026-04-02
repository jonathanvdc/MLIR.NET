namespace TableGen.Syntax;

/// <summary>
/// Represents a subscript expression such as <c>values[i]</c>.
/// </summary>
public sealed class SubscriptSyntax(ExpressionSyntax target, ExpressionSyntax index) : ExpressionSyntax
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
