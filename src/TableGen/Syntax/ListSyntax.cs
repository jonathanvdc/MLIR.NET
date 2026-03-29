namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a list literal.
/// </summary>
public sealed class ListSyntax(IReadOnlyList<ExpressionSyntax> items) : ExpressionSyntax
{
    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<ExpressionSyntax> Items { get; } = items;
}
