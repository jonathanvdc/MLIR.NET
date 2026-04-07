namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an attribute description extracted from ODS.
/// </summary>
public sealed class AttributeModel(
    string name,
    string recordName,
    string? className = null,
    EnumModel? enumModel = null,
    IReadOnlyList<AttrOrTypeParameterModel>? parameters = null,
    AssemblyFormatModel? assemblyFormat = null)
{
    /// <summary>
    /// Gets the canonical attribute name.
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
    /// Gets the enum model for this attribute, if it is backed by an enum definition.
    /// </summary>
    public EnumModel? EnumModel { get; } = enumModel;

    /// <summary>
    /// Gets the ordered list of parameters declared in the <c>parameters</c> dag of this attribute definition.
    /// Empty when the attribute has no parameters.
    /// </summary>
    public IReadOnlyList<AttrOrTypeParameterModel> Parameters { get; } = parameters ?? EmptyParameters;

    /// <summary>
    /// Gets the declarative assembly format for this attribute, if one was specified.
    /// When set, the generator produces a structured <c>AttributeValueSyntax</c> subclass and
    /// a matching <c>IAttributeAssemblyFormat</c> that handles parsing and printing.
    /// </summary>
    public AssemblyFormatModel? AssemblyFormat { get; } = assemblyFormat;

    private static readonly IReadOnlyList<AttrOrTypeParameterModel> EmptyParameters = new AttrOrTypeParameterModel[0];
}

