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
    string? csharpPresenceAttributeValue = null,
    string? csharpAssemblyFormat = null,
    string? csharpDefaultValue = null,
    string? csharpValueType = null,
    OptionalValueAccessKind? csharpOptionalValueAccessKind = null,
    OptionalAttributeRepresentation? csharpOptionalAttributeRepresentation = null,
    string? csharpPresenceSyntax = null,
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
    /// <c>$_self</c> is substituted with the storage value expression.
    /// </summary>
    /// <seealso cref="CsharpConvertFromStorageTemplate"/>
    public string? CsharpConvertFromStorage { get; } = csharpConvertFromStorage;

    /// <summary>
    /// Gets the C# expression that constructs a constant builder call for this attr, if known.
    /// <c>$0</c> is substituted with the typed value expression.
    /// </summary>
    /// <seealso cref="CsharpConstBuilderCallTemplate"/>
    public string? CsharpConstBuilderCall { get; } = csharpConstBuilderCall;

    /// <summary>
    /// Gets the C# expression that constructs the stored attribute value for a present
    /// presence-based optional attribute.
    /// </summary>
    public string? CsharpPresenceAttributeValue { get; } = csharpPresenceAttributeValue;

    /// <summary>
    /// Gets the C# expression that instantiates the assembly format for this constraint.
    /// </summary>
    public string? CsharpAssemblyFormat { get; } = csharpAssemblyFormat;

    /// <summary>
    /// Gets the <see cref="CsharpConvertFromStorage"/> value as a normalized
    /// <see cref="CodeTemplate"/> with canonical <c>${self}</c> placeholder syntax, or
    /// <see langword="null"/> when <see cref="CsharpConvertFromStorage"/> is not set.
    /// </summary>
    /// <remarks>
    /// Legacy <c>$_self</c> spellings are automatically normalized to <c>${self}</c>.
    /// Use <c>Render(new Dictionary&lt;string, string&gt; {{ "self", storageExpr }})</c>
    /// to substitute the storage value expression.
    /// </remarks>
    public CodeTemplate? CsharpConvertFromStorageTemplate =>
        CodeTemplate.From(CsharpConvertFromStorage, CodeTemplateKind.Expression);

    /// <summary>
    /// Gets the <see cref="CsharpConstBuilderCall"/> value as a normalized
    /// <see cref="CodeTemplate"/> with canonical <c>${value}</c> placeholder syntax, or
    /// <see langword="null"/> when <see cref="CsharpConstBuilderCall"/> is not set.
    /// </summary>
    /// <remarks>
    /// Legacy <c>$0</c> spellings are automatically normalized to <c>${value}</c>.
    /// Use <c>Render(new Dictionary&lt;string, string&gt; {{ "value", valueExpr }})</c>
    /// to substitute the typed value expression.
    /// </remarks>
    public CodeTemplate? CsharpConstBuilderCallTemplate =>
        CodeTemplate.From(CsharpConstBuilderCall, CodeTemplateKind.Expression, new Dictionary<string, string>(StringComparer.Ordinal) { ["0"] = "value" });

    /// <summary>
    /// Gets <see cref="CsharpPresenceAttributeValue"/> as a normalized expression template.
    /// </summary>
    public CodeTemplate? CsharpPresenceAttributeValueTemplate =>
        CodeTemplate.From(CsharpPresenceAttributeValue, CodeTemplateKind.Expression);

    /// <summary>
    /// Gets the C# default value expression for this attr, if one is known.
    /// </summary>
    public string? CsharpDefaultValue { get; } = csharpDefaultValue;

    /// <summary>
    /// Gets the C# value-type description associated with this attr, if one is known.
    /// </summary>
    public string? CsharpValueType { get; } = csharpValueType;

    /// <summary>
    /// Gets the declared optional-value access pattern for generated operation bindings.
    /// </summary>
    public OptionalValueAccessKind? CsharpOptionalValueAccessKind { get; } = csharpOptionalValueAccessKind;

    /// <summary>
    /// Gets the declared public representation for optional generated operation attributes.
    /// </summary>
    public OptionalAttributeRepresentation? CsharpOptionalAttributeRepresentation { get; } = csharpOptionalAttributeRepresentation;

    /// <summary>
    /// Gets the C# expression that builds syntax for a present presence-based optional attribute.
    /// </summary>
    /// <remarks>
    /// The expression may use <c>${token}</c> for the parsed or synthesized presence keyword token.
    /// </remarks>
    public string? CsharpPresenceSyntax { get; } = csharpPresenceSyntax;

    /// <summary>
    /// Gets <see cref="CsharpPresenceSyntax"/> as a normalized expression template.
    /// </summary>
    public CodeTemplate? CsharpPresenceSyntaxTemplate =>
        CodeTemplate.From(CsharpPresenceSyntax, CodeTemplateKind.Expression);

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
