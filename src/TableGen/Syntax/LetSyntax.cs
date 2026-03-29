namespace TableGen.Syntax;

/// <summary>
/// Represents a <c>let</c> override in a TableGen body.
/// </summary>
public sealed class LetSyntax(string name, ExpressionSyntax value) : BodyItemSyntax
{
    /// <summary>
    /// Gets the overridden field name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the replacement value.
    /// </summary>
    public ExpressionSyntax Value { get; } = value;
}
