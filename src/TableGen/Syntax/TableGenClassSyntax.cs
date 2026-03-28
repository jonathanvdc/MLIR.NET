namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen <c>class</c> declaration.
/// </summary>
public sealed class TableGenClassSyntax(
    string name,
    IReadOnlyList<TableGenTemplateParameterSyntax> templateParameters,
    IReadOnlyList<TableGenBaseSyntax> bases,
    IReadOnlyList<TableGenBodyItemSyntax> bodyItems) : TableGenTopLevelSyntax
{
    /// <summary>
    /// Gets the class name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the template parameters.
    /// </summary>
    public IReadOnlyList<TableGenTemplateParameterSyntax> TemplateParameters { get; } = templateParameters;

    /// <summary>
    /// Gets the inherited base classes.
    /// </summary>
    public IReadOnlyList<TableGenBaseSyntax> Bases { get; } = bases;

    /// <summary>
    /// Gets the class body items.
    /// </summary>
    public IReadOnlyList<TableGenBodyItemSyntax> BodyItems { get; } = bodyItems;
}
