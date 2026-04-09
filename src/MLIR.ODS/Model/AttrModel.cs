namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an upstream <c>Attr</c>-style record from ODS.
/// </summary>
/// <remarks>
/// Unlike <see cref="AttributeModel"/>, this model does not describe a concrete emitted
/// attribute definition. It captures the data that upstream MLIR uses to drive operation
/// attribute accessors, including distinct storage and accessor/result types.
/// </remarks>
public sealed class AttrModel(
    string name,
    string recordName,
    AttributeConstraintKind kind = AttributeConstraintKind.None,
    EnumModel? enumModel = null,
    string? elementConstraintRecordName = null,
    string? summary = null,
    string? csharpStorageType = null,
    string? csharpReturnType = null,
    string? csharpConvertFromStorage = null,
    string? csharpConstBuilderCall = null,
    string? csharpDefaultValue = null,
    string? csharpValueType = null,
    bool isOptional = false,
    string? baseAttrRecordName = null,
    string? cppNamespace = null,
    string? description = null)
{
    /// <summary>
    /// Gets the canonical attr name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the originating ODS record name.
    /// </summary>
    public string RecordName { get; } = recordName;

    /// <summary>
    /// Gets the supported parser/binder behavior for this attr record.
    /// </summary>
    public AttributeConstraintKind Kind { get; } = kind;

    /// <summary>
    /// Gets the enum model for this attr, when <see cref="Kind"/> is <see cref="AttributeConstraintKind.EnumAttribute"/>.
    /// </summary>
    public EnumModel? EnumModel { get; } = enumModel;

    /// <summary>
    /// Gets the originating ODS record name for the typed element constraint, if this
    /// attr is an array of typed attribute values.
    /// </summary>
    public string? ElementConstraintRecordName { get; } = elementConstraintRecordName;

    /// <summary>
    /// Gets the short human-readable summary for this attr, if known.
    /// </summary>
    public string? Summary { get; } = summary;

    /// <summary>
    /// Gets the C# storage type used by code generation for this attr, if known.
    /// </summary>
    public string? CsharpStorageType { get; } = csharpStorageType;

    /// <summary>
    /// Gets the C# return or accessor type exposed by code generation for this attr, if known.
    /// </summary>
    public string? CsharpReturnType { get; } = csharpReturnType;

    /// <summary>
    /// Gets the C# expression that converts from storage to the exposed result type, if known.
    /// </summary>
    public string? CsharpConvertFromStorage { get; } = csharpConvertFromStorage;

    /// <summary>
    /// Gets the C# expression that constructs a constant builder call for this attr, if known.
    /// </summary>
    public string? CsharpConstBuilderCall { get; } = csharpConstBuilderCall;

    /// <summary>
    /// Gets the C# default value expression for this attr, if one is known.
    /// </summary>
    public string? CsharpDefaultValue { get; } = csharpDefaultValue;

    /// <summary>
    /// Gets the C# value-type description associated with this attr, if one is known.
    /// </summary>
    public string? CsharpValueType { get; } = csharpValueType;

    /// <summary>
    /// Gets a value indicating whether this attr is optional.
    /// </summary>
    public bool IsOptional { get; } = isOptional;

    /// <summary>
    /// Gets the originating ODS record name for the base attr when this record is a wrapper.
    /// </summary>
    public string? BaseAttrRecordName { get; } = baseAttrRecordName;

    /// <summary>
    /// Gets the attr's C++ namespace, if known.
    /// </summary>
    public string? CppNamespace { get; } = cppNamespace;

    /// <summary>
    /// Gets the full description for this attr, if known.
    /// </summary>
    public string? Description { get; } = description;
}
