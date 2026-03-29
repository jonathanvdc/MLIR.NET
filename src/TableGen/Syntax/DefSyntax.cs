namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen <c>def</c> declaration.
/// </summary>
public sealed class DefSyntax(
    string name,
    IReadOnlyList<BaseSyntax> bases,
    IReadOnlyList<BodyItemSyntax> bodyItems) : TopLevelSyntax
{
    /// <summary>
    /// Gets the definition name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the inherited base classes.
    /// </summary>
    public IReadOnlyList<BaseSyntax> Bases { get; } = bases;

    /// <summary>
    /// Gets the definition body items.
    /// </summary>
    public IReadOnlyList<BodyItemSyntax> BodyItems { get; } = bodyItems;
}
