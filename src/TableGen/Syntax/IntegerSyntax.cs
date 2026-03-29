namespace TableGen.Syntax;

/// <summary>
/// Represents an integer literal.
/// </summary>
public sealed class IntegerSyntax(int value) : ExpressionSyntax
{
    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public int Value { get; } = value;
}
