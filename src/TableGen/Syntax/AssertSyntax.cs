namespace TableGen.Syntax;

/// <summary>
/// Represents a TableGen <c>assert</c> statement.
/// </summary>
public sealed class AssertSyntax(ExpressionSyntax condition, ExpressionSyntax? message) : BodyItemSyntax
{
    /// <summary>
    /// Gets the asserted condition.
    /// </summary>
    public ExpressionSyntax Condition { get; } = condition;

    /// <summary>
    /// Gets the optional assertion message.
    /// </summary>
    public ExpressionSyntax? Message { get; } = message;
}
