namespace TableGen.Syntax;

/// <summary>
/// Represents a <c>let</c> override in a TableGen body.
/// </summary>
public sealed class TableGenLetSyntax(string name, TableGenExpressionSyntax value) : TableGenBodyItemSyntax
{
    /// <summary>
    /// Gets the overridden field name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the replacement value.
    /// </summary>
    public TableGenExpressionSyntax Value { get; } = value;
}
