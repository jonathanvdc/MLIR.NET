namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a TableGen <c>assert</c> statement.
/// </summary>
public sealed class AssertSyntax(ExpressionSyntax condition, ExpressionSyntax? message, SourceLocation location = default) : BodyItemSyntax(location)
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
