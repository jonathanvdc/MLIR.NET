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
    IReadOnlyList<AttrOrTypeParameterModel>? parameters = null)
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

    private static readonly IReadOnlyList<AttrOrTypeParameterModel> EmptyParameters = new AttrOrTypeParameterModel[0];
}

