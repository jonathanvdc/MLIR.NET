namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a list literal.
/// </summary>
public sealed class TableGenListSyntax(IReadOnlyList<TableGenExpressionSyntax> items) : TableGenExpressionSyntax
{
    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<TableGenExpressionSyntax> Items { get; } = items;
}
