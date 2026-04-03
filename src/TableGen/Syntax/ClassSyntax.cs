namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen <c>class</c> declaration.
/// </summary>
public sealed class ClassSyntax(
    string name,
    IReadOnlyList<TemplateParameterSyntax> templateParameters,
    IReadOnlyList<BaseSyntax> bases,
    IReadOnlyList<LetSyntax> topLevelLets,
    IReadOnlyList<BodyItemSyntax> bodyItems) : TopLevelSyntax
{
    /// <summary>
    /// Gets the class name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the template parameters.
    /// </summary>
    public IReadOnlyList<TemplateParameterSyntax> TemplateParameters { get; } = templateParameters;

    /// <summary>
    /// Gets the inherited base classes.
    /// </summary>
    public IReadOnlyList<BaseSyntax> Bases { get; } = bases;

    /// <summary>
    /// Gets top-level let bindings lexically applied to this class.
    /// </summary>
    public IReadOnlyList<LetSyntax> TopLevelLets { get; } = topLevelLets;

    /// <summary>
    /// Gets the class body items.
    /// </summary>
    public IReadOnlyList<BodyItemSyntax> BodyItems { get; } = bodyItems;
}
