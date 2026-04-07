namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents a type description extracted from ODS.
/// </summary>
public sealed class TypeModel(
    string name,
    string recordName,
    string? className = null,
    IReadOnlyList<AttrOrTypeParameterModel>? parameters = null)
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
    /// Gets the ordered list of parameters declared in the <c>parameters</c> dag of this type definition.
    /// Empty when the type has no parameters.
    /// </summary>
    public IReadOnlyList<AttrOrTypeParameterModel> Parameters { get; } = parameters ?? EmptyParameters;

    private static readonly IReadOnlyList<AttrOrTypeParameterModel> EmptyParameters = new AttrOrTypeParameterModel[0];
}
