namespace MLIR.ODS.Model;

using System.Collections.Generic;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Represents a type description extracted from ODS.
/// </summary>
public sealed class TypeModel(
    string name,
    string recordName,
    string? className = null,
    string? summary = null,
    string? description = null,
    string? csharpName = null,
    string? csharpAssemblyFormat = null,
    IReadOnlyList<AttrOrTypeParameterModel>? parameters = null,
    AssemblyFormatModel? assemblyFormat = null)
{
    /// <summary>
    /// Gets the canonical type name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the originating ODS record name.
    /// </summary>
    public string RecordName { get; } = recordName;

    /// <summary>
    /// Gets the generated C# class name, if one was specified explicitly.
    /// </summary>
    public string? ClassName { get; } = className;

    /// <summary>
    /// Gets the type summary, if known.
    /// </summary>
    public string? Summary { get; } = summary;

    /// <summary>
    /// Gets the type description, if known.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    /// Gets the C# expression used to compute the type's canonical name, if one was specified.
    /// </summary>
    public string? CsharpName { get; } = csharpName;

    /// <summary>
    /// Gets the C# expression used to instantiate the type's custom assembly formatter, if one was specified.
    /// </summary>
    public string? CsharpAssemblyFormat { get; } = csharpAssemblyFormat;

    /// <summary>
    /// Gets the ordered list of parameters declared in the <c>parameters</c> dag of this type definition.
    /// Empty when the type has no parameters.
    /// </summary>
    public IReadOnlyList<AttrOrTypeParameterModel> Parameters { get; } = parameters ?? EmptyParameters;

    /// <summary>
    /// Gets the declarative assembly format for this type, if one was specified.
    /// </summary>
    public AssemblyFormatModel? AssemblyFormat { get; } = assemblyFormat;

    private static readonly IReadOnlyList<AttrOrTypeParameterModel> EmptyParameters = new AttrOrTypeParameterModel[0];
}
