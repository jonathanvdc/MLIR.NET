namespace MLIR.Generators;

using System;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.ODS.Model;


internal readonly struct AttributeValueConversion
{
    private readonly CodeTemplate? template;

    private AttributeValueConversion(CodeTemplate? template)
    {
        this.template = template;
    }

    public static AttributeValueConversion Identity { get; } = new(null);

    public static AttributeValueConversion FromTemplate(CodeTemplate template) => new(template);

    public static AttributeValueConversion FromExpression(string expression) =>
        FromTemplate(new CodeTemplate(expression, CodeTemplateKind.Expression));

    public string Render(string valueExpression)
    {
        return template is null
            ? valueExpression
            : template.Render(("value", valueExpression), ("self", valueExpression));
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
/// appeared across <see cref="AttributeTypeResolver"/>,
/// <see cref="Emitters.Operation.OperationMemberPlanner"/>,
/// <see cref="Emitters.Operation.OperationAttributeValueHelpers"/>,
/// <see cref="AttributeConstraintEmitter"/>, and related emitters.
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

internal static class AttributeModelCodeStrategySupport
{
    public static bool HasSpecializedAttrReturnType(AttrModel? attrModel)
    {
        var returnType = attrModel?.CsharpReturnType;
        return !string.IsNullOrEmpty(returnType)
            && !string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            && !string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal);
    }

    public static AttributeStoragePlan CreateStoragePlan(AttrModel attrModel, string publicTypeName)
    {
        var storageTypeName = !string.IsNullOrEmpty(attrModel.CsharpStorageType)
            ? attrModel.CsharpStorageType!
            : publicTypeName;
        var storageToPublic = attrModel.CsharpConvertFromStorageTemplate is CodeTemplate convertTemplate
            ? AttributeValueConversion.FromTemplate(convertTemplate)
            : AttributeValueConversion.Identity;
        var publicToStorage = GetPublicToStorageConversion(attrModel, storageTypeName, publicTypeName);
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

    private static OptionalValueAccessKind GetOptionalValueAccessKind(AttrModel attrModel)
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

    private static OptionalAttributeRepresentation GetOptionalRepresentation(AttrModel attrModel)
    {
        if (!HasSpecializedAttrReturnType(attrModel))
        {
            return OptionalAttributeRepresentation.NullableValue;
        }

        return attrModel.CsharpOptionalAttributeRepresentation
            ?? OptionalAttributeRepresentation.NullableValue;
    }

    private static AttributeValueConversion GetPublicToStorageConversion(
        AttrModel attrModel,
        string storageTypeName,
        string publicTypeName)
    {
        if (attrModel.CsharpConstBuilderCallTemplate is CodeTemplate constBuilderTemplate)
        {
            return AttributeValueConversion.FromTemplate(constBuilderTemplate);
        }

        return string.Equals(storageTypeName, publicTypeName, StringComparison.Ordinal)
            ? AttributeValueConversion.Identity
            : AttributeValueConversion.FromExpression("new " + storageTypeName + "(${value})");
    }
}

internal sealed class ModelBackedAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly AttrModel attrModel;

    public ModelBackedAttributeConstraintCodeStrategy(AttrModel attrModel)
    {
        this.attrModel = attrModel;
    }

    public override string PublicTypeName => AttributeModelCodeStrategySupport.HasSpecializedAttrReturnType(attrModel)
        ? attrModel.CsharpReturnType!
        : "AttributeValue";

    public override string? GetAssemblyFormatConstructionExpression() => attrModel.CsharpAssemblyFormat;

    public override AttributeStoragePlan CreateStoragePlan() =>
        AttributeModelCodeStrategySupport.CreateStoragePlan(attrModel, PublicTypeName);
}


/// <summary>
/// Strategy for attribute constraints whose code-generation behavior is fully described by
/// constant type names and, optionally, a custom assembly-format type.
/// </summary>
internal sealed class FixedAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public FixedAttributeConstraintCodeStrategy(
        string publicTypeName,
        string? typedArrayElementTypeName = null,
        string? assemblyFormatTypeName = null)
    {
        PublicTypeName = publicTypeName;
        TypedArrayElementTypeName = typedArrayElementTypeName ?? publicTypeName;
        this.assemblyFormatTypeName = assemblyFormatTypeName;
    }

    private readonly string? assemblyFormatTypeName;

    public override string PublicTypeName { get; }

    public override string TypedArrayElementTypeName { get; }

    public override string? GetAssemblyFormatType() => assemblyFormatTypeName;
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

    public TypedArrayConstraintCodeStrategy(AttrModel? attrModel)
    {
        this.attrModel = attrModel;
    }

    public override string PublicTypeName => AttributeModelCodeStrategySupport.HasSpecializedAttrReturnType(attrModel)
        ? attrModel!.CsharpReturnType!
        : "IReadOnlyList<AttributeValue>";

    public override bool IsTypedArray => true;
    public override string? GetAssemblyFormatConstructionExpression() =>
        "new global::MLIR.Dialects.Attributes.Collections.ArrayAttributeAssemblyFormat()";

    public override AttributeStoragePlan CreateStoragePlan()
    {
        return AttributeModelCodeStrategySupport.HasSpecializedAttrReturnType(attrModel)
            ? AttributeModelCodeStrategySupport.CreateStoragePlan(attrModel!, PublicTypeName)
            : base.CreateStoragePlan();
    }
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
    private static readonly AttributeConstraintCodeStrategy OpaqueAttributeStrategy =
        new FixedAttributeConstraintCodeStrategy("AttributeValue");

    private static readonly AttributeConstraintCodeStrategy FallbackAttributeStrategy =
        new FixedAttributeConstraintCodeStrategy("AttributeValue");

    private static readonly AttributeConstraintCodeStrategy ElementsAttributeStrategy =
        new FixedAttributeConstraintCodeStrategy(
            "global::MLIR.Dialects.Builtin.DenseTypedElementsAttr",
            assemblyFormatTypeName: "ElementsAttributeAssemblyFormat");

    private static readonly AttributeConstraintCodeStrategy DictionaryAttributeStrategy =
        new FixedAttributeConstraintCodeStrategy(
            "DictionaryAttr",
            typedArrayElementTypeName: "NamedAttributeCollection",
            assemblyFormatTypeName: "DictionaryAttributeAssemblyFormat");

    private static readonly AttributeConstraintCodeStrategy TypeAttributeStrategy =
        new FixedAttributeConstraintCodeStrategy(
            "TypeAttr",
            typedArrayElementTypeName: "TypeReference",
            assemblyFormatTypeName: "TypeAttributeAssemblyFormat");

    /// <summary>
    /// Returns the model-bound strategy for the given attribute constraint. Returns
    /// the fallback <c>AttributeValue</c> strategy for unrecognised
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
            AttributeConstraintKind.OpaqueAttribute => OpaqueAttributeStrategy,
            AttributeConstraintKind.ElementsAttribute => ElementsAttributeStrategy,
            AttributeConstraintKind.DictionaryAttribute => DictionaryAttributeStrategy,
            AttributeConstraintKind.TypeAttribute => TypeAttributeStrategy,
            AttributeConstraintKind.EnumAttribute when constraint.EnumModel != null && enumTypeName != null =>
                CreateEnumConstraintStrategy(constraint.RecordName, constraint.EnumModel, enumTypeName),
            AttributeConstraintKind.TypedArrayAttribute => new TypedArrayConstraintCodeStrategy(attrModel),
            _ when attrModel != null => new ModelBackedAttributeConstraintCodeStrategy(attrModel),
            _ => FallbackAttributeStrategy,
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
