namespace MLIR.Generators;

using System;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal enum AttributeValueConversionKind
{
    Identity,
    Template,
}

internal readonly struct AttributeValueConversion
{
    private AttributeValueConversion(AttributeValueConversionKind kind, CodeTemplate? template)
    {
        Kind = kind;
        Template = template;
    }

    public AttributeValueConversionKind Kind { get; }

    public CodeTemplate? Template { get; }

    public static AttributeValueConversion Identity { get; } =
        new(AttributeValueConversionKind.Identity, null);

    public static AttributeValueConversion FromTemplate(CodeTemplate template) =>
        new(AttributeValueConversionKind.Template, template);

    public static AttributeValueConversion FromExpression(string expression) =>
        FromTemplate(new CodeTemplate(expression, CodeTemplateKind.Expression));

    public string Render(string valueExpression)
    {
        return Kind == AttributeValueConversionKind.Identity
            ? valueExpression
            : Template!.Render(("value", valueExpression), ("self", valueExpression));
    }
}

internal sealed class AttributeStoragePlan
{
    public AttributeStoragePlan(
        string storageTypeName,
        AttributeValueConversion storageToPublic,
        AttributeValueConversion publicToStorage,
        OptionalValueAccessKind optionalValueAccessKind,
        OptionalAttributeRepresentation optionalRepresentation,
        string? presenceAttributeValueExpression,
        CodeTemplate? presenceSyntaxTemplate,
        string? defaultValueExpression)
    {
        StorageTypeName = storageTypeName;
        StorageToPublic = storageToPublic;
        PublicToStorage = publicToStorage;
        OptionalValueAccessKind = optionalValueAccessKind;
        OptionalRepresentation = optionalRepresentation;
        PresenceAttributeValueExpression = presenceAttributeValueExpression;
        PresenceSyntaxTemplate = presenceSyntaxTemplate;
        DefaultValueExpression = defaultValueExpression;
    }

    public string StorageTypeName { get; }

    public AttributeValueConversion StorageToPublic { get; }

    public AttributeValueConversion PublicToStorage { get; }

    public OptionalValueAccessKind OptionalValueAccessKind { get; }

    public OptionalAttributeRepresentation OptionalRepresentation { get; }

    public string? PresenceAttributeValueExpression { get; }

    public CodeTemplate? PresenceSyntaxTemplate { get; }

    public string? DefaultValueExpression { get; }

    public string GetPresenceAttributeValueExpression()
    {
        return PresenceAttributeValueExpression ?? PublicToStorage.Render("true");
    }
}

/// <summary>
/// Encapsulates the code-generation behavior associated with a specific kind of attribute
/// constraint. Concrete subclasses replace the per-kind switch expressions that previously
/// appeared across <see cref="Emitters.AttributeTypeResolver"/>,
/// <see cref="global::MLIR.Generators.Emitters.Operation.OperationMemberPlanner"/>,
/// <see cref="global::MLIR.Generators.Emitters.Operation.OperationAttributeValueHelpers"/>,
/// <see cref="Emitters.AttributeConstraintEmitter"/>, and related emitters.
/// </summary>
/// <remarks>
/// <para>
/// Strategies are immutable and bound to one ODS record. They may capture ODS model data
/// privately, but callers only receive code-generation decisions such as public type, storage
/// type, conversion templates, and assembly-format requirements.
/// </para>
/// </remarks>
internal abstract class AttributeConstraintCodeStrategy
{
    // -------------------------------------------------------------------------
    // Classification properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets a value indicating whether this constraint is a typed-array attribute
    /// (<c>TypedArrayAttrBase</c>-derived).
    /// </summary>
    public virtual bool IsTypedArray => false;

    // -------------------------------------------------------------------------
    // Type name resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets the C# type name that represents the public operation property value for this
    /// constraint. Unknown constraints still have an explicit public type:
    /// <c>AttributeValue</c>.
    /// This type is used for typed-array element types and for primitive property types.
    /// </summary>
    public abstract string PublicTypeName { get; }

    /// <summary>
    /// Gets the unwrapped element type to use when this constraint appears as the element
    /// constraint of a typed-array attribute.
    /// </summary>
    public virtual string TypedArrayElementTypeName => PublicTypeName;

    /// <summary>
    /// Creates the storage conversion plan used by operation property getters, setters, and
    /// constructor named-attribute generation.
    /// </summary>
    public virtual AttributeStoragePlan CreateStoragePlan()
    {
        return new AttributeStoragePlan(
            PublicTypeName,
            AttributeValueConversion.Identity,
            AttributeValueConversion.Identity,
            OptionalValueAccessKind.NullCheck,
            OptionalAttributeRepresentation.NullableValue,
            null,
            null,
            null);
    }

    /// <summary>
    /// Returns the C# type name used for an operation's generated property that holds an
    /// attribute of this constraint kind. The default implementation wraps the public type
    /// with a nullable suffix when the attribute is optional and has no default value.
    /// </summary>
    /// <param name="isRequired">
    /// Whether the attribute is mandatory (appears in the assembly format, so always present).
    /// </param>
    public string GetOperationPropertyTypeName(bool isRequired)
    {
        var storagePlan = CreateStoragePlan();
        if (!isRequired && storagePlan.OptionalRepresentation == OptionalAttributeRepresentation.PresenceBoolean)
        {
            return "bool";
        }

        if (isRequired || !string.IsNullOrEmpty(storagePlan.DefaultValueExpression))
        {
            return PublicTypeName;
        }

        return PublicTypeName.EndsWith("?", StringComparison.Ordinal) ? PublicTypeName : PublicTypeName + "?";
    }

    // -------------------------------------------------------------------------
    // Typed-array element payload
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the name of the assembly-format type to register with
    /// <c>AttributeConstraintDefinition</c>, or <see langword="null"/> when no custom
    /// assembly format is needed.
    /// </summary>
    public virtual string? GetAssemblyFormatType() => null;

    /// <summary>
    /// Returns the full C# expression used to instantiate the assembly-format object
    /// when registration needs constructor arguments or a custom factory expression.
    /// Returns <see langword="null"/> when the default <c>new {GetAssemblyFormatType}()</c>
    /// shape should be used.
    /// </summary>
    public virtual string? GetAssemblyFormatConstructionExpression() => null;

    /// <summary>
    /// Emits any helper definitions required by this strategy after the static constraint
    /// definition. Most constraints do not need extra definitions.
    /// </summary>
    public virtual void EmitAdditionalDefinitions(StringBuilder builder) { }

}

internal sealed class ModelBackedAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly AttrModel attrModel;

    public ModelBackedAttributeConstraintCodeStrategy(AttrModel attrModel)
    {
        this.attrModel = attrModel;
    }

    public override string PublicTypeName => HasSpecializedAttrReturnType(attrModel)
        ? attrModel.CsharpReturnType!
        : "AttributeValue";

    public override string? GetAssemblyFormatConstructionExpression() => attrModel.CsharpAssemblyFormat;

    public override AttributeStoragePlan CreateStoragePlan()
    {
        var storageTypeName = !string.IsNullOrEmpty(attrModel.CsharpStorageType)
            ? attrModel.CsharpStorageType!
            : PublicTypeName;
        var storageToPublic = attrModel.CsharpConvertFromStorageTemplate is CodeTemplate convertTemplate
            ? AttributeValueConversion.FromTemplate(convertTemplate)
            : AttributeValueConversion.Identity;
        var publicToStorage = GetPublicToStorageConversion(storageTypeName);
        return new AttributeStoragePlan(
            storageTypeName,
            storageToPublic,
            publicToStorage,
            GetOptionalValueAccessKind(),
            GetOptionalRepresentation(),
            attrModel.CsharpPresenceAttributeValueTemplate?.Render(("value", "true"), ("self", "true")),
            attrModel.CsharpPresenceSyntaxTemplate,
            attrModel.CsharpDefaultValue);
    }

    private OptionalValueAccessKind GetOptionalValueAccessKind()
    {
        if (!HasSpecializedAttrReturnType(attrModel))
        {
            return OptionalValueAccessKind.NullCheck;
        }

        if (attrModel.CsharpOptionalValueAccessKind is OptionalValueAccessKind kind)
        {
            return kind;
        }

        throw new InvalidOperationException(
            "Attr record '" + attrModel.RecordName + "' declares csharpReturnType but no csharpOptionalValueAccess.");
    }

    private OptionalAttributeRepresentation GetOptionalRepresentation()
    {
        if (!HasSpecializedAttrReturnType(attrModel))
        {
            return OptionalAttributeRepresentation.NullableValue;
        }

        return attrModel.CsharpOptionalAttributeRepresentation
            ?? OptionalAttributeRepresentation.NullableValue;
    }

    private AttributeValueConversion GetPublicToStorageConversion(string storageTypeName)
    {
        if (attrModel.CsharpConstBuilderCallTemplate is CodeTemplate constBuilderTemplate)
        {
            return AttributeValueConversion.FromTemplate(constBuilderTemplate);
        }

        return string.Equals(storageTypeName, PublicTypeName, StringComparison.Ordinal)
            ? AttributeValueConversion.Identity
            : AttributeValueConversion.FromExpression("new " + storageTypeName + "(${value})");
    }

    private static bool HasSpecializedAttrReturnType(AttrModel? attrModel)
    {
        var returnType = attrModel?.CsharpReturnType;
        return !string.IsNullOrEmpty(returnType)
            && !string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            && !string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal);
    }
}


/// <summary>
/// Opaque attribute (e.g. <c>AnyAttr</c>, <c>LocationAttr</c>). Preserved as a generic
/// <c>AttributeValue</c>; falls back to <c>AttributeValue</c> when used as a
/// typed-array element.
/// </summary>
internal sealed class OpaqueAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly OpaqueAttributeConstraintCodeStrategy Instance = new();
    private OpaqueAttributeConstraintCodeStrategy() { }

    public override string PublicTypeName => "AttributeValue";
}

/// <summary>
/// Elements attribute (e.g. <c>ElementsAttr</c>). Dense elements literals bind to the
/// generated builtin <c>DenseTypedElementsAttr</c> class rather than to a handwritten
/// constraint wrapper.
/// </summary>
internal sealed class ElementsAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly ElementsAttributeConstraintCodeStrategy Instance = new();
    private ElementsAttributeConstraintCodeStrategy() { }

    public override string PublicTypeName => "global::MLIR.Dialects.Builtin.DenseTypedElementsAttr";

    public override string? GetAssemblyFormatType() => "ElementsAttributeAssemblyFormat";
}

/// <summary>
/// Dictionary attribute (<c>DictionaryAttr</c>). Properties are exposed as
/// <c>DictionaryAttr</c>; the unwrapped value type for typed-array elements
/// is <c>NamedAttributeCollection</c>.
/// </summary>
internal sealed class DictionaryAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly DictionaryAttributeConstraintCodeStrategy Instance = new();
    private DictionaryAttributeConstraintCodeStrategy() { }

    public override string PublicTypeName => "DictionaryAttr";
    public override string TypedArrayElementTypeName => "NamedAttributeCollection";

    /// <summary>
    /// Returns <c>"NamedAttributeCollection"</c> – the unwrapped value type used for
    /// typed-array element extraction. Note that this is the unwrapped type regardless of
    /// whether the constraint is classified as primitive (it is not).
    /// </summary>
    public override string? GetAssemblyFormatType() => "DictionaryAttributeAssemblyFormat";

}

/// <summary>
/// Type attribute (<c>TypeAttr</c>). Properties are exposed as
/// <c>TypeAttr</c>; the unwrapped value for typed-array elements is
/// <c>TypeReference</c>.
/// </summary>
internal sealed class TypeAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly TypeAttributeConstraintCodeStrategy Instance = new();
    private TypeAttributeConstraintCodeStrategy() { }

    public override string PublicTypeName => "TypeAttr";
    public override string TypedArrayElementTypeName => "TypeReference";

    /// <summary>
    /// Returns <c>"TypeReference"</c> – the unwrapped value type used for typed-array
    /// element extraction.
    /// </summary>
    public override string? GetAssemblyFormatType() => "TypeAttributeAssemblyFormat";
}

/// <summary>
/// Enum attribute (e.g. <c>I32EnumAttr</c>-backed attrs). The C# type for the value
/// is the generated enum type, resolved via the <see cref="DialectSymbolResolver"/>.
/// </summary>
internal sealed class EnumAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly string recordName;
    private readonly EnumModel enumModel;
    private readonly string enumTypeName;
    private readonly string storageTypeName;
    private readonly AttributeValueConversion storageToPublic;
    private readonly AttributeValueConversion publicToStorage;
    private readonly bool emitConstraintAssemblyFormat;

    public EnumAttributeConstraintCodeStrategy(
        string recordName,
        EnumModel enumModel,
        string enumTypeName,
        string storageTypeName,
        AttributeValueConversion storageToPublic,
        AttributeValueConversion publicToStorage,
        bool emitConstraintAssemblyFormat)
    {
        this.recordName = recordName;
        this.enumModel = enumModel;
        this.enumTypeName = enumTypeName;
        this.storageTypeName = storageTypeName;
        this.storageToPublic = storageToPublic;
        this.publicToStorage = publicToStorage;
        this.emitConstraintAssemblyFormat = emitConstraintAssemblyFormat;
    }

    public override string PublicTypeName => enumTypeName;
    public override string? GetAssemblyFormatConstructionExpression() =>
        emitConstraintAssemblyFormat
            ? "new " + EnumEmitter.GetEnumConstraintAssemblyFormatTypeName(recordName) + "()"
            : null;

    public override AttributeStoragePlan CreateStoragePlan()
    {
        return new AttributeStoragePlan(
            storageTypeName,
            storageToPublic,
            publicToStorage,
            OptionalValueAccessKind.NullableValueType,
            OptionalAttributeRepresentation.NullableValue,
            null,
            null,
            null);
    }

    public override void EmitAdditionalDefinitions(StringBuilder builder)
    {
        if (!emitConstraintAssemblyFormat)
        {
            return;
        }

        builder.AppendLine();
        EmitEnumConstraintAssemblyFormat(builder);
    }

    private void EmitEnumConstraintAssemblyFormat(StringBuilder builder)
    {
        var formatTypeName = EnumEmitter.GetEnumConstraintAssemblyFormatTypeName(recordName);
        var infoClassName = EnumEmitter.GetEnumInfoClassName(enumModel);

        // Pure enum constraints store as IntegerAttr so the typed parameter for the runtime
        // base is IntegerAttr. Angle brackets are never used for inline operation-attribute
        // enum syntax (no angle brackets in declarative assembly format).
        var baseTypeName = enumModel.IsBitEnum
            ? "global::MLIR.Dialects.Attributes.FlagsEnumAttributeAssemblyFormat<global::MLIR.Dialects.Builtin.IntegerAttr>"
            : "global::MLIR.Dialects.Attributes.SimpleEnumAttributeAssemblyFormat<global::MLIR.Dialects.Builtin.IntegerAttr>";

        builder.AppendLine("internal sealed class " + formatTypeName);
        builder.AppendLine("    : " + baseTypeName);
        builder.AppendLine("{");
        builder.AppendLine("    public " + formatTypeName + "()");
        builder.AppendLine("        : base(" + infoClassName + ".NamesByInteger) { }");
        builder.AppendLine();
        builder.AppendLine("    public override int BitWidth => " + enumModel.Bitwidth + ";");
        builder.AppendLine("    public override global::MLIR.Dialects.Attributes.EnumAngleBracketRequirement AngleBracketRequirement");
        builder.AppendLine("        => global::MLIR.Dialects.Attributes.EnumAngleBracketRequirement.Prohibited;");
        builder.AppendLine();

        if (enumModel.IsBitEnum)
        {
            var sepKind = EnumEmitter.GetSeparatorTokenKind(enumModel);
            builder.AppendLine("    public override global::MLIR.Text.TokenKind SeparatorTokenKind");
            builder.AppendLine("        => global::MLIR.Text." + sepKind + ";");
            builder.AppendLine();
        }

        // EnumFromInt – wraps the parsed integer in an IntegerAttr with the correct storage type.
        builder.AppendLine("    public override global::MLIR.Dialects.Builtin.IntegerAttr EnumFromInt(global::MLIR.Numerics.ApInt value, global::MLIR.Syntax.AttributeValueSyntax syntax)");
        builder.AppendLine("        => new global::MLIR.Dialects.Builtin.IntegerAttr(" + EnumEmitter.GetIntegerTypeFactoryExpression(enumModel.Bitwidth) + ", value, syntax);");
        builder.AppendLine();

        // EnumToInt – reads the integer stored in the IntegerAttr for printing.
        builder.AppendLine("    public override global::MLIR.Numerics.ApInt EnumToInt(global::MLIR.Dialects.Builtin.IntegerAttr value)");
        builder.AppendLine("        => value.Value;");
        builder.AppendLine("}");
    }
}

/// <summary>
/// Typed-array attribute (<c>TypedArrayAttrBase</c>-derived). The C# element type and
/// typed-array value type are resolved recursively from the element constraint record.
/// </summary>
internal sealed class TypedArrayConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly AttrModel? attrModel;
    private readonly string? elementRecordName;

    public TypedArrayConstraintCodeStrategy(AttrModel? attrModel, string? elementRecordName)
    {
        this.attrModel = attrModel;
        this.elementRecordName = elementRecordName;
    }

    public override string PublicTypeName => HasSpecializedAttrReturnType(attrModel)
        ? attrModel!.CsharpReturnType!
        : "IReadOnlyList<AttributeValue>";
    public override bool IsTypedArray => true;
    public override string? GetAssemblyFormatConstructionExpression() =>
        "new global::MLIR.Dialects.Attributes.Collections.ArrayAttributeAssemblyFormat()";

    public override AttributeStoragePlan CreateStoragePlan()
    {
        if (HasSpecializedAttrReturnType(attrModel))
        {
            var storageTypeName = !string.IsNullOrEmpty(attrModel!.CsharpStorageType)
                ? attrModel.CsharpStorageType!
                : PublicTypeName;
            var storageToPublic = attrModel.CsharpConvertFromStorageTemplate is CodeTemplate convertTemplate
                ? AttributeValueConversion.FromTemplate(convertTemplate)
                : AttributeValueConversion.Identity;
            var publicToStorage = attrModel.CsharpConstBuilderCallTemplate is CodeTemplate constBuilderTemplate
                ? AttributeValueConversion.FromTemplate(constBuilderTemplate)
                : string.Equals(storageTypeName, PublicTypeName, StringComparison.Ordinal)
                    ? AttributeValueConversion.Identity
                    : AttributeValueConversion.FromExpression("new " + storageTypeName + "(${value})");
            return new AttributeStoragePlan(
                storageTypeName,
                storageToPublic,
                publicToStorage,
                GetOptionalValueAccessKind(attrModel),
                GetOptionalRepresentation(attrModel),
                attrModel.CsharpPresenceAttributeValueTemplate?.Render(("value", "true"), ("self", "true")),
                attrModel.CsharpPresenceSyntaxTemplate,
                attrModel.CsharpDefaultValue);
        }

        return base.CreateStoragePlan();
    }

    private static bool HasSpecializedAttrReturnType(AttrModel? attrModel)
    {
        var returnType = attrModel?.CsharpReturnType;
        return !string.IsNullOrEmpty(returnType)
            && !string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            && !string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal);
    }

    private static OptionalValueAccessKind GetOptionalValueAccessKind(AttrModel attrModel)
    {
        if (attrModel.CsharpOptionalValueAccessKind is OptionalValueAccessKind kind)
        {
            return kind;
        }

        if (HasSpecializedAttrReturnType(attrModel))
        {
            throw new InvalidOperationException(
                "Attr record '" + attrModel.RecordName + "' declares csharpReturnType but no csharpOptionalValueAccess.");
        }

        return OptionalValueAccessKind.NullCheck;
    }

    private static OptionalAttributeRepresentation GetOptionalRepresentation(AttrModel attrModel)
    {
        return attrModel.CsharpOptionalAttributeRepresentation
            ?? OptionalAttributeRepresentation.NullableValue;
    }
}

// =============================================================================
// Fallback strategy
// =============================================================================

/// <summary>
/// Used whenever no specialised strategy matches a constraint record (e.g.
/// <see cref="AttributeConstraintKind.None"/> or any unrecognised kind, as well as
/// when an attribute has no associated constraint record at all).
/// </summary>
/// <remarks>
/// Produces <c>AttributeValue</c> / <c>AttributeValue?</c> operation properties instead
/// of the old <c>NamedAttribute</c> / <c>NamedAttribute?</c> pair, so callers always
/// receive a typed value rather than a raw named-attribute wrapper.
/// </remarks>
internal sealed class FallbackAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly FallbackAttributeConstraintCodeStrategy Instance = new();
    private FallbackAttributeConstraintCodeStrategy() { }

    public override string PublicTypeName => "AttributeValue";
}

// =============================================================================
// Factory
// =============================================================================

/// <summary>
/// Maps an <see cref="AttributeConstraintKind"/> (and, when relevant, a record name) to
/// the appropriate <see cref="AttributeConstraintCodeStrategy"/> singleton.
/// </summary>
/// <remarks>
/// The factory is the only place in <c>MLIR.Generators</c> that branches on
/// <see cref="AttributeConstraintKind"/>. All code that previously switched on the kind
/// at use-sites should instead call
/// <see cref="AttributeConstraintCodeStrategyFactory.GetStrategy"/> once (during
/// <see cref="DialectSymbolResolver"/> initialisation) and store the resulting strategy
/// instance for later dispatch.
/// </remarks>
internal static class AttributeConstraintCodeStrategyFactory
{
    /// <summary>
    /// Returns the model-bound strategy for the given attribute constraint. Returns
    /// <see cref="FallbackAttributeConstraintCodeStrategy.Instance"/> for unrecognised
    /// kinds (including <see cref="AttributeConstraintKind.None"/> and
    /// <see cref="AttributeConstraintKind.DenseArrayAttribute"/>).
    /// </summary>
    /// <param name="constraint">The imported ODS constraint model.</param>
    /// <param name="attrModel">Optional Attr-model metadata for the same ODS record.</param>
    /// <param name="enumTypeName">The fully qualified generated C# enum type name for enum constraints.</param>
    public static AttributeConstraintCodeStrategy GetStrategy(AttributeConstraintModel constraint, AttrModel? attrModel, string? enumTypeName)
    {
        return constraint.Kind switch
        {
            AttributeConstraintKind.OpaqueAttribute => OpaqueAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.ElementsAttribute => ElementsAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DictionaryAttribute => DictionaryAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.TypeAttribute => TypeAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.EnumAttribute when constraint.EnumModel != null && enumTypeName != null =>
                CreateEnumConstraintStrategy(constraint.RecordName, constraint.EnumModel, enumTypeName),
            AttributeConstraintKind.TypedArrayAttribute => new TypedArrayConstraintCodeStrategy(attrModel, constraint.ElementConstraintRecordName),
            _ when attrModel != null => new ModelBackedAttributeConstraintCodeStrategy(attrModel),
            _ => FallbackAttributeConstraintCodeStrategy.Instance,
        };
    }

    public static AttributeConstraintCodeStrategy GetEnumAttributeStrategy(
        string recordName,
        EnumModel enumModel,
        string enumTypeName,
        string attributeClassName)
    {
        return new EnumAttributeConstraintCodeStrategy(
            recordName,
            enumModel,
            enumTypeName,
            attributeClassName,
            AttributeValueConversion.FromExpression("${self}.TypedValue"),
            AttributeValueConversion.FromExpression("new " + attributeClassName + "(${value})"),
            emitConstraintAssemblyFormat: false);
    }

    private static AttributeConstraintCodeStrategy CreateEnumConstraintStrategy(
        string recordName,
        EnumModel enumModel,
        string enumTypeName)
    {
        return new EnumAttributeConstraintCodeStrategy(
            recordName,
            enumModel,
            enumTypeName,
            "global::MLIR.Dialects.Builtin.IntegerAttr",
            AttributeValueConversion.FromExpression(EnumEmitter.GetIntegerToEnumExpression(enumModel, "${self}.Value")),
            AttributeValueConversion.FromExpression(EnumEmitter.GetEnumToIntegerAttrExpression(enumModel, "${value}", "null")),
            emitConstraintAssemblyFormat: true);
    }
}
