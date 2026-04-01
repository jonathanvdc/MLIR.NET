namespace TableGen.Syntax;

/// <summary>
/// Represents a TableGen string concatenation expression using the '#' operator.
/// </summary>
public sealed class ConcatSyntax(ExpressionSyntax left, ExpressionSyntax right) : ExpressionSyntax
{
    /// <summary>
    /// Gets the left operand.
    /// </summary>
    public ExpressionSyntax Left { get; } = left;

    /// <summary>
    /// Gets the right operand.
    /// </summary>
    public ExpressionSyntax Right { get; } = right;
}
