namespace TableGen.Syntax;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents a list literal.
/// </summary>
public sealed class ListSyntax(IReadOnlyList<ExpressionSyntax> items, SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<ExpressionSyntax> Items { get; } = items;
}
