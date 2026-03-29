namespace TableGen.Syntax;

/// <summary>
/// Represents a string literal.
/// </summary>
public sealed class StringSyntax(string value) : ExpressionSyntax
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; } = value;
}
