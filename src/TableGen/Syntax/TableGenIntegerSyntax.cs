namespace TableGen.Syntax;

/// <summary>
/// Represents an integer literal.
/// </summary>
public sealed class TableGenIntegerSyntax(int value) : TableGenExpressionSyntax
{
    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public int Value { get; } = value;
}
