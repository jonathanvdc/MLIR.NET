namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen <c>def</c> declaration.
/// </summary>
public sealed class TableGenDefSyntax(
    string name,
    IReadOnlyList<TableGenBaseSyntax> bases,
    IReadOnlyList<TableGenBodyItemSyntax> bodyItems) : TableGenTopLevelSyntax
{
    /// <summary>
    /// Gets the definition name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the inherited base classes.
    /// </summary>
    public IReadOnlyList<TableGenBaseSyntax> Bases { get; } = bases;

    /// <summary>
    /// Gets the definition body items.
    /// </summary>
    public IReadOnlyList<TableGenBodyItemSyntax> BodyItems { get; } = bodyItems;
}
