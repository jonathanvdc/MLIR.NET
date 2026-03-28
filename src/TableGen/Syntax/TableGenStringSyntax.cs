namespace TableGen.Syntax;

/// <summary>
/// Represents a string literal.
/// </summary>
public sealed class TableGenStringSyntax(string value) : TableGenExpressionSyntax
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; } = value;
}
