namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen <c>def</c> declaration.
/// </summary>
public sealed class DefSyntax(
    string name,
    IReadOnlyList<BaseSyntax> bases,
    IReadOnlyList<LetSyntax> topLevelLets,
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
    /// Gets top-level let bindings lexically applied to this definition.
    /// </summary>
    public IReadOnlyList<LetSyntax> TopLevelLets { get; } = topLevelLets;

    /// <summary>
    /// Gets the definition body items.
    /// </summary>
    public IReadOnlyList<BodyItemSyntax> BodyItems { get; } = bodyItems;
}
