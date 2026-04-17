namespace MLIR.Generators;

using System;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal enum AttributeConstraintEmissionKind
{
    StaticDefinition,
}

internal enum AttributeValueConversionKind
{
    Identity,
    Template,
}

internal enum OptionalValueKind
{
    Reference,
    NullableValueType,
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
        OptionalValueKind optionalValueKind,
        string? defaultValueExpression)
    {
        StorageTypeName = storageTypeName;
        StorageToPublic = storageToPublic;
        PublicToStorage = publicToStorage;
        OptionalValueKind = optionalValueKind;
        DefaultValueExpression = defaultValueExpression;
    }

    public string StorageTypeName { get; }

    public AttributeValueConversion StorageToPublic { get; }

    public AttributeValueConversion PublicToStorage { get; }

    public OptionalValueKind OptionalValueKind { get; }

    public string? DefaultValueExpression { get; }
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

    /// <summary>
    /// Gets a value indicating whether this constraint is a unit attribute.
    /// Required unit attributes are typed <c>UnitAttr</c>; optional ones are
    /// exposed as <c>bool</c> (present/absent) rather than <c>UnitAttr?</c>.
    /// </summary>
    public virtual bool IsUnit => false;

    /// <summary>
    /// Gets a value indicating whether this constraint, when used as an element type inside
    /// a <c>TypedArrayAttrBase</c>-derived attribute, should fall back to the generic
    /// <c>AttributeValue</c> element type rather than a specialised C# type.
    /// </summary>
    public virtual bool IsGenericTypedArrayElement => false;

    /// <summary>
    /// Gets a value indicating whether this constraint, when used as an element type inside
    /// a typed-array attribute, should have its payload extracted via a named property on
    /// the generated constraint class (see <see cref="GetTypedArrayElementPayloadPropertyName"/>).
    /// When <see langword="false"/> the decoder falls back to
    /// <c>StructuredAttributeSemanticDecoder.DecodeValue</c>.
    /// </summary>
    public virtual bool UsesTypedArrayElementPayload => false;

    /// <summary>
    /// Gets the shape of generated code this constraint requires.
    /// </summary>
    public virtual AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;

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
            GetOptionalValueKind(PublicTypeName),
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
    public virtual string GetOperationPropertyTypeName(bool isRequired)
    {
        var defaultValue = CreateStoragePlan().DefaultValueExpression;
        if (isRequired || !string.IsNullOrEmpty(defaultValue))
        {
            return PublicTypeName;
        }

        return PublicTypeName.EndsWith("?", StringComparison.Ordinal) ? PublicTypeName : PublicTypeName + "?";
    }

    // -------------------------------------------------------------------------
    // Typed-array element payload
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the name of the property on the generated constraint class that holds the
    /// element's typed payload when this constraint is used as an element inside a
    /// typed-array attribute (e.g. <c>"Value"</c>, <c>"TypedValue"</c>, <c>"Items"</c>).
    /// Only called when <see cref="UsesTypedArrayElementPayload"/> is <see langword="true"/>.
    /// </summary>
    public virtual string GetTypedArrayElementPayloadPropertyName() => "Value";

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

    // -------------------------------------------------------------------------
    // Typed-array decode/encode
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a C# expression that extracts the typed element value directly from a
    /// decoded <c>AttributeValue</c> when the constraint is used as a typed-array element
    /// type. The placeholder <c>{itemSyntax}</c> in the returned expression is replaced by
    /// the name of the <c>AttributeValueSyntax</c> variable available at the call site.
    /// Returns <see langword="null"/> when the old constraint-class instance path should be
    /// used instead.
    /// </summary>
    public virtual string? GetTypedArrayElementDecodeExpression() => null;

    /// <summary>
    /// Returns a C# expression that converts the typed element value back to an
    /// <c>AttributeValueSyntax</c> for the typed-array assembly format.  The placeholder
    /// <c>{element}</c> in the returned expression is replaced by the name of the element
    /// variable, and <c>{context}</c> by the <c>ConcreteSyntaxBuilderContext</c> variable.
    /// Returns <see langword="null"/> when the old constraint-class instance path should be
    /// used instead.
    /// </summary>
    public virtual string? GetTypedArrayElementToSyntaxExpression() => null;

    protected static OptionalValueKind GetOptionalValueKind(string typeName)
    {
        var trimmedTypeName = typeName.TrimEnd('?');
        return string.Equals(trimmedTypeName, "bool", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "byte", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "sbyte", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "short", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ushort", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "int", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "uint", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "long", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ulong", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "BigInteger", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "global::MLIR.Numerics.ApInt", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ApInt", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "global::MLIR.Numerics.ApFloat", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ApFloat", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "float", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "double", StringComparison.Ordinal)
            ? OptionalValueKind.NullableValueType
            : OptionalValueKind.Reference;
    }

}

internal abstract class ModelBackedAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly AttrModel? attrModel;
    private readonly string fallbackPublicTypeName;
    private readonly string fallbackStorageTypeName;
    private readonly string? assemblyFormatType;
    private readonly string? assemblyFormatConstructionExpression;
    private readonly string typedArrayElementPayloadPropertyName;
    private readonly string? typedArrayElementDecodeExpression;
    private readonly string? typedArrayElementToSyntaxExpression;

    protected ModelBackedAttributeConstraintCodeStrategy(
        AttrModel? attrModel,
        string fallbackPublicTypeName,
        string? fallbackStorageTypeName = null,
        string? assemblyFormatType = null,
        string? assemblyFormatConstructionExpression = null,
        string typedArrayElementPayloadPropertyName = "Value",
        string? typedArrayElementDecodeExpression = null,
        string? typedArrayElementToSyntaxExpression = null)
    {
        this.attrModel = attrModel;
        this.fallbackPublicTypeName = fallbackPublicTypeName;
        this.fallbackStorageTypeName = fallbackStorageTypeName ?? fallbackPublicTypeName;
        this.assemblyFormatType = assemblyFormatType;
        this.assemblyFormatConstructionExpression = assemblyFormatConstructionExpression;
        this.typedArrayElementPayloadPropertyName = typedArrayElementPayloadPropertyName;
        this.typedArrayElementDecodeExpression = typedArrayElementDecodeExpression;
        this.typedArrayElementToSyntaxExpression = typedArrayElementToSyntaxExpression;
    }

    public override string PublicTypeName => HasSpecializedAttrReturnType(attrModel)
        ? attrModel!.CsharpReturnType!
        : fallbackPublicTypeName;

    public override bool UsesTypedArrayElementPayload => typedArrayElementDecodeExpression == null;

    public override string GetTypedArrayElementPayloadPropertyName() => typedArrayElementPayloadPropertyName;

    public override string? GetAssemblyFormatType() => assemblyFormatType;

    public override string? GetAssemblyFormatConstructionExpression() => assemblyFormatConstructionExpression;

    public override string? GetTypedArrayElementDecodeExpression() => typedArrayElementDecodeExpression;

    public override string? GetTypedArrayElementToSyntaxExpression() => typedArrayElementToSyntaxExpression;

    public override AttributeStoragePlan CreateStoragePlan()
    {
        var storageTypeName = !string.IsNullOrEmpty(attrModel?.CsharpStorageType)
            ? attrModel!.CsharpStorageType!
            : fallbackStorageTypeName;
        var storageToPublic = attrModel?.CsharpConvertFromStorageTemplate is CodeTemplate convertTemplate
            ? AttributeValueConversion.FromTemplate(convertTemplate)
            : AttributeValueConversion.Identity;
        var publicToStorage = GetPublicToStorageConversion(storageTypeName);
        return new AttributeStoragePlan(
            storageTypeName,
            storageToPublic,
            publicToStorage,
            GetOptionalValueKind(PublicTypeName),
            attrModel?.CsharpDefaultValue);
    }

    private AttributeValueConversion GetPublicToStorageConversion(string storageTypeName)
    {
        if (attrModel?.CsharpConstBuilderCallTemplate is CodeTemplate constBuilderTemplate)
        {
            return AttributeValueConversion.FromTemplate(constBuilderTemplate);
        }

        return string.Equals(storageTypeName, PublicTypeName, StringComparison.Ordinal)
            ? AttributeValueConversion.Identity
            : AttributeValueConversion.FromExpression("new " + storageTypeName + "(${value})");
    }

    protected static bool HasSpecializedAttrReturnType(AttrModel? attrModel)
    {
        var returnType = attrModel?.CsharpReturnType;
        return !string.IsNullOrEmpty(returnType)
            && !string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            && !string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal);
    }
}

internal sealed class PrimitiveAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly ModelBackedPrimitiveAttributeConstraintCodeStrategy implementation;

    public PrimitiveAttributeConstraintCodeStrategy(
        AttrModel? attrModel,
        string attributeValueTypeName,
        string? assemblyFormatType,
        string? assemblyFormatConstructionExpression,
        string typedArrayElementPayloadPropertyName = "Value",
        string? typedArrayElementDecodeExpression = null,
        string? typedArrayElementToSyntaxExpression = null)
    {
        implementation = new ModelBackedPrimitiveAttributeConstraintCodeStrategy(
            attrModel,
            attributeValueTypeName,
            assemblyFormatType,
            assemblyFormatConstructionExpression,
            typedArrayElementPayloadPropertyName,
            typedArrayElementDecodeExpression,
            typedArrayElementToSyntaxExpression);
    }

    public override string PublicTypeName => implementation.PublicTypeName;
    public override bool UsesTypedArrayElementPayload => implementation.UsesTypedArrayElementPayload;
    public override AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;
    public override AttributeStoragePlan CreateStoragePlan() => implementation.CreateStoragePlan();
    public override string GetTypedArrayElementPayloadPropertyName() => implementation.GetTypedArrayElementPayloadPropertyName();
    public override string? GetAssemblyFormatType() => implementation.GetAssemblyFormatType();
    public override string? GetAssemblyFormatConstructionExpression() => implementation.GetAssemblyFormatConstructionExpression();
    public override string? GetTypedArrayElementDecodeExpression() => implementation.GetTypedArrayElementDecodeExpression();
    public override string? GetTypedArrayElementToSyntaxExpression() => implementation.GetTypedArrayElementToSyntaxExpression();

    private sealed class ModelBackedPrimitiveAttributeConstraintCodeStrategy : ModelBackedAttributeConstraintCodeStrategy
    {
        public ModelBackedPrimitiveAttributeConstraintCodeStrategy(
            AttrModel? attrModel,
            string fallbackPublicTypeName,
            string? assemblyFormatType,
            string? assemblyFormatConstructionExpression,
            string typedArrayElementPayloadPropertyName,
            string? typedArrayElementDecodeExpression,
            string? typedArrayElementToSyntaxExpression)
            : base(
                attrModel,
                fallbackPublicTypeName,
                assemblyFormatType: assemblyFormatType,
                assemblyFormatConstructionExpression: assemblyFormatConstructionExpression,
                typedArrayElementPayloadPropertyName: typedArrayElementPayloadPropertyName,
                typedArrayElementDecodeExpression: typedArrayElementDecodeExpression,
                typedArrayElementToSyntaxExpression: typedArrayElementToSyntaxExpression)
        {
        }
    }
}

internal sealed class DensePrimitiveArrayAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly ModelBackedDensePrimitiveArrayAttributeConstraintCodeStrategy implementation;

    public DensePrimitiveArrayAttributeConstraintCodeStrategy(
        AttrModel? attrModel,
        string attributeValueTypeName,
        string? assemblyFormatType,
        string? assemblyFormatConstructionExpression,
        string typedArrayElementPayloadPropertyName = "Items")
    {
        implementation = new ModelBackedDensePrimitiveArrayAttributeConstraintCodeStrategy(
            attrModel,
            attributeValueTypeName,
            assemblyFormatType,
            assemblyFormatConstructionExpression,
            typedArrayElementPayloadPropertyName);
    }

    public override string PublicTypeName => implementation.PublicTypeName;
    public override bool UsesTypedArrayElementPayload => true;
    public override AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;
    public override AttributeStoragePlan CreateStoragePlan() => implementation.CreateStoragePlan();
    public override string GetTypedArrayElementPayloadPropertyName() => implementation.GetTypedArrayElementPayloadPropertyName();
    public override string? GetAssemblyFormatType() => implementation.GetAssemblyFormatType();
    public override string? GetAssemblyFormatConstructionExpression() => implementation.GetAssemblyFormatConstructionExpression();

    private sealed class ModelBackedDensePrimitiveArrayAttributeConstraintCodeStrategy : ModelBackedAttributeConstraintCodeStrategy
    {
        public ModelBackedDensePrimitiveArrayAttributeConstraintCodeStrategy(
            AttrModel? attrModel,
            string fallbackPublicTypeName,
            string? assemblyFormatType,
            string? assemblyFormatConstructionExpression,
            string typedArrayElementPayloadPropertyName)
            : base(
                attrModel,
                fallbackPublicTypeName,
                assemblyFormatType: assemblyFormatType,
                assemblyFormatConstructionExpression: assemblyFormatConstructionExpression,
                typedArrayElementPayloadPropertyName: typedArrayElementPayloadPropertyName)
        {
        }
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
    public override bool IsGenericTypedArrayElement => true;
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
    public override bool IsGenericTypedArrayElement => true;
    public override AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;

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
    public override bool UsesTypedArrayElementPayload => false;
    public override AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;

    /// <summary>
    /// Returns <c>"NamedAttributeCollection"</c> – the unwrapped value type used for
    /// typed-array element extraction. Note that this is the unwrapped type regardless of
    /// whether the constraint is classified as primitive (it is not).
    /// </summary>
    public override string? GetAssemblyFormatType() => "DictionaryAttributeAssemblyFormat";
    public override string? GetTypedArrayElementDecodeExpression() =>
        "{itemSyntax} is global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax dictionarySyntax " +
        "? global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes(dictionarySyntax.Attributes.Items) " +
        ": global::MLIR.Semantics.NamedAttributeCollection.Empty";
    public override string? GetTypedArrayElementToSyntaxExpression() =>
        "global::MLIR.Dialects.Attributes.Collections.DictionaryAttributeAssemblyFormat.BuildSyntax({element}, {context})";

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
    public override bool UsesTypedArrayElementPayload => false;
    public override AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;

    /// <summary>
    /// Returns <c>"TypeReference"</c> – the unwrapped value type used for typed-array
    /// element extraction.
    /// </summary>
    public override string? GetAssemblyFormatType() => "TypeAttributeAssemblyFormat";
    public override string? GetTypedArrayElementDecodeExpression() =>
        "{itemSyntax} is global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax typeSyntax " +
        "? new global::MLIR.Semantics.UnknownTypeReference(typeSyntax.TypeSyntax, null, null, typeSyntax.TypeSyntax.Location) " +
        ": throw new global::System.InvalidOperationException(\"Unexpected syntax for type attribute. Expected a type attribute literal such as 'i32'.\")";
    public override string? GetTypedArrayElementToSyntaxExpression() =>
        "new global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax({context}.BuildTypeSyntax({element}))";
}

/// <summary>
/// Unit attribute (<c>UnitAttr</c>). Optional properties are exposed as <c>bool</c>
/// rather than <c>UnitAttr?</c>. Falls back to <c>AttributeValue</c> when
/// used as a typed-array element.
/// </summary>
internal sealed class UnitAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly UnitAttributeConstraintCodeStrategy Instance = new();
    private UnitAttributeConstraintCodeStrategy() { }

    public override string PublicTypeName => "UnitAttr";
    public override bool IsUnit => true;
    public override bool IsGenericTypedArrayElement => true;
    public override AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;

    /// <summary>
    /// Required unit attributes are typed <c>UnitAttr</c>; optional ones are
    /// exposed as <c>bool</c> (present/absent) rather than <c>UnitAttr?</c>.
    /// </summary>
    public override string GetOperationPropertyTypeName(bool isRequired) =>
        isRequired ? "UnitAttr" : "bool";

    public override string? GetAssemblyFormatType() => "UnitAttributeAssemblyFormat";
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
    public override AttributeConstraintEmissionKind EmissionKind => AttributeConstraintEmissionKind.StaticDefinition;
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
            OptionalValueKind.NullableValueType,
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
        var localEnumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        builder.AppendLine("internal sealed class " + EnumEmitter.GetEnumConstraintAssemblyFormatTypeName(recordName) + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
        EnumEmitter.EmitParseEnumValueHelperMethod(
            builder,
            enumModel,
            localEnumTypeName,
            "    ",
            "private static",
            includeIntegerLiteralSyntaxFallback: true,
            allowBitEnumAngleBrackets: false);
        EnumEmitter.EmitAssemblyFormatTryParseMethod(
            builder,
            enumModel,
            "    ",
            allowBitEnumAngleBrackets: false);
        builder.AppendLine("    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return " + EnumEmitter.GetEnumToIntegerAttrExpression(enumModel, "ParseEnumValue(syntax)", "syntax") + ";");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (attribute is global::MLIR.Dialects.Builtin.IntegerAttr integerAttr");
        builder.AppendLine("            && " + EnumEmitter.GetEnumInfoClassName(enumModel) + ".TryFromInteger(integerAttr.Value, out var enumValue))");
        builder.AppendLine("        {");
        builder.AppendLine("            var text = " + EnumEmitter.GetEnumInfoClassName(enumModel) + ".Format(enumValue);");
        builder.AppendLine("            return new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(text));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (attribute.Syntax != null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return attribute.Syntax;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (attribute is global::MLIR.Dialects.Builtin.IntegerAttr fallbackIntegerAttr)");
        builder.AppendLine("        {");
        builder.AppendLine("            return context.BuildAttributeValueSyntax(fallbackIntegerAttr);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        throw new global::System.InvalidOperationException(\"Enum constraints require IntegerAttr storage for custom assembly emission, but received \" + attribute.GetType().FullName + \".\");");
        builder.AppendLine("    }");
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
    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "Items";
    public override string? GetAssemblyFormatConstructionExpression() =>
        "new global::MLIR.Dialects.Attributes.Collections.TypedArrayAttributeAssemblyFormat()";

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
                GetOptionalValueKind(PublicTypeName),
                attrModel.CsharpDefaultValue);
        }

        return base.CreateStoragePlan();
    }

    /// <summary>
    /// Returns the C# typed-array value type (e.g. <c>"IReadOnlyList&lt;string&gt;"</c>),
    /// resolved by looking up the element constraint record name and using the element
    /// constraint's own <see cref="AttributeConstraintCodeStrategy.PublicTypeName"/>.
    /// Returns <c>"IReadOnlyList&lt;AttributeValue&gt;"</c> when no element strategy is
    /// available or the element type falls back to a generic type.
    /// </summary>
    public string GetRecursivePublicTypeName(DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "IReadOnlyList<AttributeValue>";
        }

        var elementStrategy = resolver.TryResolveAttributeConstraintStrategy(elementRecordName!);
        if (elementStrategy.IsGenericTypedArrayElement)
        {
            return "IReadOnlyList<AttributeValue>";
        }

        var elementTypeName = elementStrategy.TypedArrayElementTypeName;
        if (IsTypedArrayFallbackElementType(elementTypeName))
        {
            return "IReadOnlyList<AttributeValue>";
        }

        return "IReadOnlyList<" + elementTypeName + ">";
    }

    /// <summary>
    /// Returns <see langword="true"/> for element type names that should fall back to the
    /// generic <c>IReadOnlyList&lt;AttributeValue&gt;</c> typed-array representation.
    /// </summary>
    private static bool IsTypedArrayFallbackElementType(string elementTypeName) =>
        elementTypeName == "UnitAttr"
        || elementTypeName == "OpaqueAttr"
        || elementTypeName == "global::MLIR.Dialects.Builtin.DenseTypedElementsAttr";

    private static bool HasSpecializedAttrReturnType(AttrModel? attrModel)
    {
        var returnType = attrModel?.CsharpReturnType;
        return !string.IsNullOrEmpty(returnType)
            && !string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            && !string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal);
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
/// When used as a typed-array element type the generic
/// <c>IReadOnlyList&lt;AttributeValue&gt;</c> fallback is applied (via
/// <see cref="AttributeConstraintCodeStrategy.IsGenericTypedArrayElement"/>).
/// </remarks>
internal sealed class FallbackAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly FallbackAttributeConstraintCodeStrategy Instance = new();
    private FallbackAttributeConstraintCodeStrategy() { }

    public override string PublicTypeName => "AttributeValue";

    /// <summary>
    /// Marks this as a generic typed-array element so that
    /// <see cref="TypedArrayConstraintCodeStrategy"/> falls back to
    /// <c>IReadOnlyList&lt;AttributeValue&gt;</c> for arrays whose element type is
    /// unknown.
    /// </summary>
    public override bool IsGenericTypedArrayElement => true;

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
    private static PrimitiveAttributeConstraintCodeStrategy CreateBooleanLiteralStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "bool",
        assemblyFormatType: "BooleanLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.IntegerAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value.ToUInt64() != 0",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I1, global::MLIR.Numerics.ApInt.FromInt64(1, {element} ? 1 : 0), null))");

    private static PrimitiveAttributeConstraintCodeStrategy CreateIntegerLiteralStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "global::MLIR.Numerics.ApInt",
        assemblyFormatType: "IntegerLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.IntegerAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I({element}.BitWidth), {element}, null))");

    private static PrimitiveAttributeConstraintCodeStrategy CreateGenericFloatingPointLiteralStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F64, {element}, null))");

    private static PrimitiveAttributeConstraintCodeStrategy CreateF16Strategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEEHalf)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F16, {element}, null))");

    private static PrimitiveAttributeConstraintCodeStrategy CreateF32Strategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEESingle)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F32, {element}, null))");

    private static PrimitiveAttributeConstraintCodeStrategy CreateBF16Strategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.BFloat16)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.BF16, {element}, null))");

    private static PrimitiveAttributeConstraintCodeStrategy CreateF64Strategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEEDouble)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.Dialects.Builtin.FloatAttr(global::MLIR.Semantics.TypeFactory.F64, {element}, null))");

    private static PrimitiveAttributeConstraintCodeStrategy CreateStringLiteralStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "string",
        assemblyFormatType: "StringLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        typedArrayElementDecodeExpression:
            "((global::MLIR.Dialects.Builtin.StringAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(global::MLIR.Semantics.ConstantAttributeFactory.String({element}))");

    private static DensePrimitiveArrayAttributeConstraintCodeStrategy CreateDenseBooleanArrayStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "IReadOnlyList<bool>",
        assemblyFormatType: "DenseBooleanArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null);

    private static DensePrimitiveArrayAttributeConstraintCodeStrategy CreateDenseIntegerArrayStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "IReadOnlyList<global::MLIR.Numerics.ApInt>",
        assemblyFormatType: "DenseIntegerArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null);

    private static DensePrimitiveArrayAttributeConstraintCodeStrategy CreateDenseF32ArrayStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "IReadOnlyList<global::MLIR.Numerics.ApFloat>",
        assemblyFormatType: "DenseFloatingPointArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new DenseFloatingPointArrayAttributeAssemblyFormat(\"f32\")");

    private static DensePrimitiveArrayAttributeConstraintCodeStrategy CreateDenseF64ArrayStrategy(AttrModel? attrModel) => new(
        attrModel,
        attributeValueTypeName: "IReadOnlyList<global::MLIR.Numerics.ApFloat>",
        assemblyFormatType: "DenseFloatingPointArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new DenseFloatingPointArrayAttributeAssemblyFormat(\"f64\")");

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
            AttributeConstraintKind.BooleanLiteral => CreateBooleanLiteralStrategy(attrModel),
            AttributeConstraintKind.IntegerLiteral => CreateIntegerLiteralStrategy(attrModel),
            AttributeConstraintKind.FloatingPointLiteral => GetFloatingPointStrategy(constraint.RecordName, attrModel),
            AttributeConstraintKind.StringLiteral => CreateStringLiteralStrategy(attrModel),
            AttributeConstraintKind.OpaqueAttribute => OpaqueAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.ElementsAttribute => ElementsAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DictionaryAttribute => DictionaryAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.TypeAttribute => TypeAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.UnitAttribute => UnitAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DenseBooleanArrayAttribute => CreateDenseBooleanArrayStrategy(attrModel),
            AttributeConstraintKind.DenseIntegerArrayAttribute => CreateDenseIntegerArrayStrategy(attrModel),
            AttributeConstraintKind.DenseF32ArrayAttribute => CreateDenseF32ArrayStrategy(attrModel),
            AttributeConstraintKind.DenseF64ArrayAttribute => CreateDenseF64ArrayStrategy(attrModel),
            AttributeConstraintKind.EnumAttribute when constraint.EnumModel != null && enumTypeName != null =>
                CreateEnumConstraintStrategy(constraint.RecordName, constraint.EnumModel, enumTypeName),
            AttributeConstraintKind.TypedArrayAttribute => new TypedArrayConstraintCodeStrategy(attrModel, constraint.ElementConstraintRecordName),
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
            AttributeValueConversion.FromExpression(EnumEmitter.GetIntegerToEnumExpression(enumModel, "${self}.Value", "default")),
            AttributeValueConversion.FromExpression(EnumEmitter.GetEnumToIntegerAttrExpression(enumModel, "${value}", "null")),
            emitConstraintAssemblyFormat: true);
    }

    private static AttributeConstraintCodeStrategy GetFloatingPointStrategy(string recordName, AttrModel? attrModel)
    {
        return recordName switch
        {
            "Builtin_FloatAttr" => CreateGenericFloatingPointLiteralStrategy(attrModel),
            "F16Attr" => CreateF16Strategy(attrModel),
            "F32Attr" => CreateF32Strategy(attrModel),
            "BF16Attr" => CreateBF16Strategy(attrModel),
            "F64Attr" => CreateF64Strategy(attrModel),
            _ => throw new System.NotSupportedException($"Unsupported floating-point attribute constraint '{recordName}'."),
        };
    }
}
