namespace MLIR.Generators;

using MLIR.ODS.Model;

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
/// Every supported <see cref="AttributeConstraintKind"/> is represented by exactly one
/// concrete subclass.  Instances are stateless singletons – all record-specific information
/// is passed as method parameters so that a single instance can serve any number of
/// identically-kinded constraints.
/// </para>
/// <para>
/// <see cref="AttributeConstraintCodeStrategyFactory"/> maps an
/// <see cref="AttributeConstraintKind"/> (and, when needed, a record name) to the correct
/// singleton.
/// </para>
/// </remarks>
internal abstract class AttributeConstraintCodeStrategy
{
    // -------------------------------------------------------------------------
    // Classification properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets a value indicating whether this constraint is a primitive attribute (boolean,
    /// integer, string, floating-point, or enum). Primitive attributes have a simple C#
    /// value type and use wrapper-class constructors in generated setters.
    /// </summary>
    public virtual bool IsPrimitive => false;

    /// <summary>
    /// Gets a value indicating whether this constraint is a dense collection attribute
    /// (e.g. <c>array&lt;i32: …&gt;</c>).
    /// </summary>
    public virtual bool IsDenseCollection => false;

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
    /// Gets a value indicating whether this constraint is an enum attribute.
    /// Enum attributes require a dedicated enum-wrapper setter path in generated code.
    /// </summary>
    public virtual bool IsEnum => false;

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

    // -------------------------------------------------------------------------
    // Primitive value access
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the C# member-access expression suffix used to extract the primitive value
    /// from an already-cast constraint instance (e.g. <c>.Value</c> or <c>.TypedValue</c>).
    /// Only called when <see cref="IsPrimitive"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="typeName">The C# type name of the member, including any trailing <c>?</c>.</param>
    public virtual string GetPrimitiveValueAccess(string typeName) => ".Value";

    // -------------------------------------------------------------------------
    // Type name resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the C# type name that represents the unwrapped attribute value for this
    /// constraint (e.g. <c>"bool"</c>, <c>"global::MLIR.Numerics.ApInt"</c>, <c>"TypeSyntax"</c>), or
    /// <see langword="null"/> when no specialised type is available.
    /// This type is used for typed-array element types and for primitive property types.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    /// <param name="resolver">The resolver for cross-dialect symbol lookup.</param>
    public abstract string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver);

    /// <summary>
    /// Returns the C# type name used for an operation's generated property that holds an
    /// attribute of this constraint kind.  The default implementation wraps
    /// <see cref="GetAttributeValueTypeName"/> with a nullable suffix when
    /// <paramref name="isRequired"/> is <see langword="false"/>, and falls back to
    /// <c>NamedAttribute</c> when no specialised type is known.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    /// <param name="isRequired">
    /// Whether the attribute is mandatory (appears in the assembly format, so always present).
    /// </param>
    /// <param name="resolver">The resolver for cross-dialect symbol lookup.</param>
    public virtual string GetOperationPropertyTypeName(string constraintRecordName, bool isRequired, DialectSymbolResolver resolver)
    {
        var typeName = GetAttributeValueTypeName(constraintRecordName, resolver);
        if (typeName is null)
        {
            return isRequired ? "NamedAttribute" : "NamedAttribute?";
        }

        return isRequired ? typeName : typeName + "?";
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

    // -------------------------------------------------------------------------
    // Constraint class emission helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the C# base class for the generated attribute constraint class
    /// (e.g. <c>"BooleanAttributeValue"</c>). Defaults to <c>"AttributeValue"</c>.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string GetBaseType(string constraintRecordName) => "AttributeValue";

    /// <summary>
    /// Returns the name of the assembly-format type to register with
    /// <c>AttributeConstraintDefinition</c>, or <see langword="null"/> when no custom
    /// assembly format is needed.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string? GetAssemblyFormatType(string constraintRecordName) => null;

    /// <summary>
    /// Returns the full C# expression used to instantiate the assembly-format object
    /// when registration needs constructor arguments or a custom factory expression.
    /// Returns <see langword="null"/> when the default <c>new {GetAssemblyFormatType}()</c>
    /// shape should be used.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string? GetAssemblyFormatConstructionExpression(string constraintRecordName) => null;

    /// <summary>
    /// Returns the argument list to pass to the base-class constructor from the
    /// <c>AttributeValueConstructionContext</c> constructor, or <see langword="null"/>
    /// to use the default <c>(context.Syntax, context.Location)</c> call.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string? GetPrimitiveBaseConstructor(string constraintRecordName) => null;

    /// <summary>
    /// Returns the C# type of the single parameter for the generated "value" convenience
    /// constructor, or <see langword="null"/> when no value constructor should be emitted.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string? GetValueConstructorParameter(string constraintRecordName) => null;

    /// <summary>
    /// Emits additional private helper members (e.g. <c>DecodeTypeSyntax</c> or
    /// <c>DecodeAttributes</c>) inside the generated constraint class body.
    /// Called by <c>AttributeConstraintEmitter.EmitStandardConstraint</c> after the
    /// constructor declarations. The default implementation emits nothing.
    /// </summary>
    /// <param name="builder">The string builder to append to.</param>
    /// <param name="className">The generated class name for context.</param>
    public virtual void EmitInnerHelpers(System.Text.StringBuilder builder, string className) { }

    // -------------------------------------------------------------------------
    // Primitive attr factory and typed-array decode/encode
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a C# lambda expression that the <c>AttributeConstraintDefinition</c> factory
    /// should use when no constraint wrapper class is generated. The expression receives a
    /// single <c>context</c> parameter of type
    /// <c>AttributeValueConstructionContext</c> and returns an <c>AttributeValue</c>.
    /// Returns <see langword="null"/> when the old constraint-class factory path should be
    /// used instead.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string? GetFactoryExpression(string constraintRecordName) => null;

    /// <summary>
    /// Returns a C# expression that extracts the typed element value directly from a
    /// decoded <c>AttributeValue</c> when the constraint is used as a typed-array element
    /// type. The placeholder <c>{itemSyntax}</c> in the returned expression is replaced by
    /// the name of the <c>AttributeValueSyntax</c> variable available at the call site.
    /// Returns <see langword="null"/> when the old constraint-class instance path should be
    /// used instead.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string? GetTypedArrayElementDecodeExpression(string constraintRecordName) => null;

    /// <summary>
    /// Returns a C# expression that converts the typed element value back to an
    /// <c>AttributeValueSyntax</c> for the typed-array assembly format.  The placeholder
    /// <c>{element}</c> in the returned expression is replaced by the name of the element
    /// variable, and <c>{context}</c> by the <c>ConcreteSyntaxBuilderContext</c> variable.
    /// Returns <see langword="null"/> when the old constraint-class instance path should be
    /// used instead.
    /// </summary>
    /// <param name="constraintRecordName">The ODS record name of the constraint.</param>
    public virtual string? GetTypedArrayElementToSyntaxExpression(string constraintRecordName) => null;

}

internal sealed class PrimitiveAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly string attributeValueTypeName;
    private readonly string baseType;
    private readonly string? assemblyFormatType;
    private readonly string? assemblyFormatConstructionExpression;
    private readonly string? primitiveBaseConstructor;
    private readonly string? valueConstructorParameter;
    private readonly string primitiveValueAccess;
    private readonly string typedArrayElementPayloadPropertyName;
    private readonly string? factoryExpression;
    private readonly string? typedArrayElementDecodeExpression;
    private readonly string? typedArrayElementToSyntaxExpression;

    public PrimitiveAttributeConstraintCodeStrategy(
        string attributeValueTypeName,
        string baseType,
        string? assemblyFormatType,
        string? assemblyFormatConstructionExpression,
        string? primitiveBaseConstructor,
        string? valueConstructorParameter,
        string primitiveValueAccess = ".Value",
        string typedArrayElementPayloadPropertyName = "Value",
        string? factoryExpression = null,
        string? typedArrayElementDecodeExpression = null,
        string? typedArrayElementToSyntaxExpression = null)
    {
        this.attributeValueTypeName = attributeValueTypeName;
        this.baseType = baseType;
        this.assemblyFormatType = assemblyFormatType;
        this.assemblyFormatConstructionExpression = assemblyFormatConstructionExpression;
        this.primitiveBaseConstructor = primitiveBaseConstructor;
        this.valueConstructorParameter = valueConstructorParameter;
        this.primitiveValueAccess = primitiveValueAccess;
        this.typedArrayElementPayloadPropertyName = typedArrayElementPayloadPropertyName;
        this.factoryExpression = factoryExpression;
        this.typedArrayElementDecodeExpression = typedArrayElementDecodeExpression;
        this.typedArrayElementToSyntaxExpression = typedArrayElementToSyntaxExpression;
    }

    public override bool IsPrimitive => true;
    public override bool UsesTypedArrayElementPayload => typedArrayElementDecodeExpression == null;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => attributeValueTypeName;
    public override string GetPrimitiveValueAccess(string typeName) => primitiveValueAccess;
    public override string GetTypedArrayElementPayloadPropertyName() => typedArrayElementPayloadPropertyName;
    public override string GetBaseType(string constraintRecordName) => baseType;
    public override string? GetAssemblyFormatType(string constraintRecordName) => assemblyFormatType;
    public override string? GetAssemblyFormatConstructionExpression(string constraintRecordName) => assemblyFormatConstructionExpression;
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => primitiveBaseConstructor;
    public override string? GetValueConstructorParameter(string constraintRecordName) => valueConstructorParameter;
    public override string? GetFactoryExpression(string constraintRecordName) => factoryExpression;
    public override string? GetTypedArrayElementDecodeExpression(string constraintRecordName) => typedArrayElementDecodeExpression;
    public override string? GetTypedArrayElementToSyntaxExpression(string constraintRecordName) => typedArrayElementToSyntaxExpression;
}

internal sealed class DensePrimitiveArrayAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    private readonly string attributeValueTypeName;
    private readonly string baseType;
    private readonly string? assemblyFormatType;
    private readonly string? assemblyFormatConstructionExpression;
    private readonly string? primitiveBaseConstructor;
    private readonly string? valueConstructorParameter;
    private readonly string typedArrayElementPayloadPropertyName;

    public DensePrimitiveArrayAttributeConstraintCodeStrategy(
        string attributeValueTypeName,
        string baseType,
        string? assemblyFormatType,
        string? assemblyFormatConstructionExpression,
        string? primitiveBaseConstructor,
        string? valueConstructorParameter,
        string typedArrayElementPayloadPropertyName = "Items")
    {
        this.attributeValueTypeName = attributeValueTypeName;
        this.baseType = baseType;
        this.assemblyFormatType = assemblyFormatType;
        this.assemblyFormatConstructionExpression = assemblyFormatConstructionExpression;
        this.primitiveBaseConstructor = primitiveBaseConstructor;
        this.valueConstructorParameter = valueConstructorParameter;
        this.typedArrayElementPayloadPropertyName = typedArrayElementPayloadPropertyName;
    }

    public override bool IsDenseCollection => true;
    public override bool UsesTypedArrayElementPayload => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => attributeValueTypeName;
    public override string GetTypedArrayElementPayloadPropertyName() => typedArrayElementPayloadPropertyName;
    public override string GetBaseType(string constraintRecordName) => baseType;
    public override string? GetAssemblyFormatType(string constraintRecordName) => assemblyFormatType;
    public override string? GetAssemblyFormatConstructionExpression(string constraintRecordName) => assemblyFormatConstructionExpression;
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => primitiveBaseConstructor;
    public override string? GetValueConstructorParameter(string constraintRecordName) => valueConstructorParameter;
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

    public override bool IsGenericTypedArrayElement => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "AttributeValue";
    public override string GetBaseType(string constraintRecordName) => "AttributeValue";
}

/// <summary>
/// Elements attribute (e.g. <c>ElementsAttr</c>). Falls back to <c>AttributeValue</c>
/// when used as a typed-array element (decoder uses the generic path).
/// </summary>
internal sealed class ElementsAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly ElementsAttributeConstraintCodeStrategy Instance = new();
    private ElementsAttributeConstraintCodeStrategy() { }

    public override bool IsGenericTypedArrayElement => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "ElementsAttributeValue";

    public override string GetBaseType(string constraintRecordName) => "ElementsAttributeValue";

    public override string? GetAssemblyFormatType(string constraintRecordName) => "ElementsAttributeAssemblyFormat";

    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) =>
        "context, StructuredAttributeSemanticDecoder.DecodeValue(((ElementsAttributeValueSyntax)context.Syntax).Payload), ((ElementsAttributeValueSyntax)context.Syntax).TypeSyntax";
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

    public override bool UsesTypedArrayElementPayload => false;

    /// <summary>
    /// Returns <c>"NamedAttributeCollection"</c> – the unwrapped value type used for
    /// typed-array element extraction. Note that this is the unwrapped type regardless of
    /// whether the constraint is classified as primitive (it is not).
    /// </summary>
    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "NamedAttributeCollection";

    /// <summary>
    /// Returns <c>"DictionaryAttr"</c> (required) or
    /// <c>"DictionaryAttr?"</c> (optional) – the type used for operation
    /// member properties, which wraps the attribute in its constraint class.
    /// </summary>
    public override string GetOperationPropertyTypeName(string constraintRecordName, bool isRequired, DialectSymbolResolver resolver) =>
        isRequired ? "DictionaryAttr" : "DictionaryAttr?";

    public override string? GetAssemblyFormatType(string constraintRecordName) => "DictionaryAttributeAssemblyFormat";
    public override string? GetFactoryExpression(string constraintRecordName) =>
        "static context => new global::MLIR.DictionaryAttr(" +
        "(context.Syntax is global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax dictionarySyntax " +
        "? global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes(dictionarySyntax.Attributes.Items) " +
        ": global::MLIR.Semantics.NamedAttributeCollection.Empty), context.Syntax!)";
    public override string? GetTypedArrayElementDecodeExpression(string constraintRecordName) =>
        "{itemSyntax} is global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax dictionarySyntax " +
        "? global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes(dictionarySyntax.Attributes.Items) " +
        ": global::MLIR.Semantics.NamedAttributeCollection.Empty";
    public override string? GetTypedArrayElementToSyntaxExpression(string constraintRecordName) =>
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

    public override bool UsesTypedArrayElementPayload => false;

    /// <summary>
    /// Returns <c>"TypeReference"</c> – the unwrapped value type used for typed-array
    /// element extraction.
    /// </summary>
    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "TypeReference";

    /// <summary>
    /// Returns <c>"TypeAttr"</c> (required) or <c>"TypeAttr?"</c>
    /// (optional) – the type used for operation member properties.
    /// </summary>
    public override string GetOperationPropertyTypeName(string constraintRecordName, bool isRequired, DialectSymbolResolver resolver) =>
        isRequired ? "TypeAttr" : "TypeAttr?";

    public override string GetBaseType(string constraintRecordName) => "AttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "TypeAttributeAssemblyFormat";
    public override string? GetFactoryExpression(string constraintRecordName) =>
        "static context => context.Syntax is global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax typeSyntax " +
        "? new global::MLIR.TypeAttr(new global::MLIR.Semantics.UnknownTypeReference(typeSyntax.TypeSyntax, null, null, typeSyntax.TypeSyntax.Location), context.Syntax) " +
        ": throw new global::System.InvalidOperationException(\"Unexpected syntax for type attribute. Expected a type attribute literal such as 'i32'.\")";
    public override string? GetTypedArrayElementDecodeExpression(string constraintRecordName) =>
        "{itemSyntax} is global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax typeSyntax " +
        "? new global::MLIR.Semantics.UnknownTypeReference(typeSyntax.TypeSyntax, null, null, typeSyntax.TypeSyntax.Location) " +
        ": throw new global::System.InvalidOperationException(\"Unexpected syntax for type attribute. Expected a type attribute literal such as 'i32'.\")";
    public override string? GetTypedArrayElementToSyntaxExpression(string constraintRecordName) =>
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

    public override bool IsUnit => true;
    public override bool IsGenericTypedArrayElement => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "UnitAttr";

    /// <summary>
    /// Required unit attributes are typed <c>UnitAttr</c>; optional ones are
    /// exposed as <c>bool</c> (present/absent) rather than <c>UnitAttr?</c>.
    /// </summary>
    public override string GetOperationPropertyTypeName(string constraintRecordName, bool isRequired, DialectSymbolResolver resolver) =>
        isRequired ? "UnitAttr" : "bool";

    public override string GetBaseType(string constraintRecordName) => "AttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "UnitAttributeAssemblyFormat";
    public override string? GetFactoryExpression(string constraintRecordName) =>
        "static context => new global::MLIR.UnitAttr(context.Syntax)";
}


/// <summary>
/// Enum attribute (e.g. <c>I32EnumAttr</c>-backed attrs). The C# type for the value
/// is the generated enum type, resolved via the <see cref="DialectSymbolResolver"/>.
/// </summary>
internal sealed class EnumAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly EnumAttributeConstraintCodeStrategy Instance = new();
    private EnumAttributeConstraintCodeStrategy() { }

    public override bool IsPrimitive => true;
    public override bool IsEnum => true;
    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "TypedValue";
    public override string GetPrimitiveValueAccess(string typeName) => ".TypedValue";

    /// <summary>
    /// Returns the fully-qualified generated enum type name for this constraint, looked
    /// up from the resolver, or <see langword="null"/> when no enum model is registered.
    /// </summary>
    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) =>
        resolver.TryResolveEnumTypeName(constraintRecordName);

    // Emission helpers are not used for enum constraints – they are handled by
    // AttributeConstraintEmitter.EmitEnumConstraint, which has its own specialised code.
    public override string GetBaseType(string constraintRecordName) => "IntegerAttributeValue";
}

/// <summary>
/// Typed-array attribute (<c>TypedArrayAttrBase</c>-derived). The C# element type and
/// typed-array value type are resolved recursively from the element constraint record.
/// </summary>
internal sealed class TypedArrayConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly TypedArrayConstraintCodeStrategy Instance = new();
    private TypedArrayConstraintCodeStrategy() { }

    public override bool IsTypedArray => true;
    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "Items";

    /// <summary>
    /// Returns the C# typed-array value type (e.g. <c>"IReadOnlyList&lt;string&gt;"</c>),
    /// resolved by looking up the element constraint record name and delegating to the
    /// element constraint's own <see cref="AttributeConstraintCodeStrategy.GetAttributeValueTypeName"/>.
    /// Returns <c>"IReadOnlyList&lt;AttributeValue&gt;"</c> when no element strategy is
    /// available or the element type falls back to a generic type.
    /// </summary>
    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver)
    {
        var elementRecordName = resolver.TryResolveAttributeConstraintElementRecordName(constraintRecordName);
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "IReadOnlyList<AttributeValue>";
        }

        var elementStrategy = resolver.TryResolveAttributeConstraintStrategy(elementRecordName!);
        if (elementStrategy.IsGenericTypedArrayElement)
        {
            return "IReadOnlyList<AttributeValue>";
        }

        var elementTypeName = elementStrategy.GetAttributeValueTypeName(elementRecordName!, resolver);
        if (elementTypeName is null || IsTypedArrayFallbackElementType(elementTypeName))
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
        || elementTypeName == "ElementsAttributeValue";

    // Emission is handled by AttributeConstraintEmitter.EmitTypedArrayConstraint.
    public override string GetBaseType(string constraintRecordName) => "AttributeValue";
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

    /// <summary>
    /// Marks this as a generic typed-array element so that
    /// <see cref="TypedArrayConstraintCodeStrategy"/> falls back to
    /// <c>IReadOnlyList&lt;AttributeValue&gt;</c> for arrays whose element type is
    /// unknown.
    /// </summary>
    public override bool IsGenericTypedArrayElement => true;

    /// <summary>
    /// Returns <c>"AttributeValue"</c> — the most general typed representation for an
    /// attribute whose concrete constraint kind is not statically known.
    /// </summary>
    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "AttributeValue";
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
    private static readonly PrimitiveAttributeConstraintCodeStrategy BooleanLiteralStrategy = new(
        attributeValueTypeName: "bool",
        baseType: "BooleanAttributeValue",
        assemblyFormatType: "BooleanLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        primitiveBaseConstructor: "context, ((BooleanAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "bool",
        factoryExpression:
            "static context => new global::MLIR.IntegerAttr(" +
            "global::MLIR.Semantics.TypeFactory.I1, " +
            "global::MLIR.Numerics.ApInt.FromInt64(1, ((global::MLIR.Syntax.Attributes.Primitives.BooleanAttributeValueSyntax)context.Syntax).Value ? 1 : 0), " +
            "context.Syntax)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.IntegerAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value.ToUInt64() != 0",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.IntegerAttr(global::MLIR.Semantics.TypeFactory.I1, global::MLIR.Numerics.ApInt.FromInt64(1, {element} ? 1 : 0), null))");

    private static readonly PrimitiveAttributeConstraintCodeStrategy IntegerLiteralStrategy = new(
        attributeValueTypeName: "global::MLIR.Numerics.ApInt",
        baseType: "IntegerAttributeValue",
        assemblyFormatType: "IntegerLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        primitiveBaseConstructor: "context, ((IntegerAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "global::MLIR.Numerics.ApInt",
        factoryExpression:
            "static context => { var __intSyntax = (global::MLIR.Syntax.Attributes.Primitives.IntegerAttributeValueSyntax)context.Syntax; " +
            "return new global::MLIR.IntegerAttr(global::MLIR.Semantics.TypeFactory.I(__intSyntax.Value.BitWidth), __intSyntax.Value, __intSyntax); }",
        typedArrayElementDecodeExpression:
            "((global::MLIR.IntegerAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.IntegerAttr(global::MLIR.Semantics.TypeFactory.I({element}.BitWidth), {element}, null))");

    private static readonly PrimitiveAttributeConstraintCodeStrategy GenericFloatingPointLiteralStrategy = new(
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        baseType: "FloatingPointAttributeValue",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        primitiveBaseConstructor: "context, ((FloatingPointAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "global::MLIR.Numerics.ApFloat",
        factoryExpression:
            "static context => new global::MLIR.FloatAttr(" +
            "global::MLIR.Semantics.TypeFactory.F64, " +
            "((global::MLIR.Syntax.Attributes.Primitives.FloatingPointAttributeValueSyntax)context.Syntax).Value, " +
            "context.Syntax)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.FloatAttr(global::MLIR.Semantics.TypeFactory.F64, {element}, null))");

    private static readonly PrimitiveAttributeConstraintCodeStrategy F16Strategy = new(
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        baseType: "FloatingPointAttributeValue",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEEHalf)",
        primitiveBaseConstructor: "context, ((FloatingPointAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "global::MLIR.Numerics.ApFloat",
        factoryExpression:
            "static context => new global::MLIR.FloatAttr(" +
            "global::MLIR.Semantics.TypeFactory.F16, " +
            "((global::MLIR.Syntax.Attributes.Primitives.FloatingPointAttributeValueSyntax)context.Syntax).Value, " +
            "context.Syntax)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.FloatAttr(global::MLIR.Semantics.TypeFactory.F16, {element}, null))");

    private static readonly PrimitiveAttributeConstraintCodeStrategy F32Strategy = new(
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        baseType: "FloatingPointAttributeValue",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEESingle)",
        primitiveBaseConstructor: "context, ((FloatingPointAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "global::MLIR.Numerics.ApFloat",
        factoryExpression:
            "static context => new global::MLIR.FloatAttr(" +
            "global::MLIR.Semantics.TypeFactory.F32, " +
            "((global::MLIR.Syntax.Attributes.Primitives.FloatingPointAttributeValueSyntax)context.Syntax).Value, " +
            "context.Syntax)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.FloatAttr(global::MLIR.Semantics.TypeFactory.F32, {element}, null))");

    private static readonly PrimitiveAttributeConstraintCodeStrategy BF16Strategy = new(
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        baseType: "FloatingPointAttributeValue",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.BFloat16)",
        primitiveBaseConstructor: "context, ((FloatingPointAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "global::MLIR.Numerics.ApFloat",
        factoryExpression:
            "static context => new global::MLIR.FloatAttr(" +
            "global::MLIR.Semantics.TypeFactory.BF16, " +
            "((global::MLIR.Syntax.Attributes.Primitives.FloatingPointAttributeValueSyntax)context.Syntax).Value, " +
            "context.Syntax)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.FloatAttr(global::MLIR.Semantics.TypeFactory.BF16, {element}, null))");

    private static readonly PrimitiveAttributeConstraintCodeStrategy F64Strategy = new(
        attributeValueTypeName: "global::MLIR.Numerics.ApFloat",
        baseType: "FloatingPointAttributeValue",
        assemblyFormatType: "FloatingPointLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new FloatingPointLiteralAttributeAssemblyFormat(global::MLIR.Numerics.FloatSemantics.IEEEDouble)",
        primitiveBaseConstructor: "context, ((FloatingPointAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "global::MLIR.Numerics.ApFloat",
        factoryExpression:
            "static context => new global::MLIR.FloatAttr(" +
            "global::MLIR.Semantics.TypeFactory.F64, " +
            "((global::MLIR.Syntax.Attributes.Primitives.FloatingPointAttributeValueSyntax)context.Syntax).Value, " +
            "context.Syntax)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.FloatAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(new global::MLIR.FloatAttr(global::MLIR.Semantics.TypeFactory.F64, {element}, null))");

    private static readonly PrimitiveAttributeConstraintCodeStrategy StringLiteralStrategy = new(
        attributeValueTypeName: "string",
        baseType: "StringAttributeValue",
        assemblyFormatType: "StringLiteralAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        primitiveBaseConstructor: "context, ((StringAttributeValueSyntax)context.Syntax).Value",
        valueConstructorParameter: "string",
        factoryExpression:
            "static context => new global::MLIR.StringAttr(" +
            "((global::MLIR.Syntax.Attributes.Primitives.StringAttributeValueSyntax)context.Syntax).Value, " +
            "global::MLIR.Semantics.TypeFactory.None, " +
            "context.Syntax)",
        typedArrayElementDecodeExpression:
            "((global::MLIR.StringAttr)global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue({itemSyntax})).Value",
        typedArrayElementToSyntaxExpression:
            "{context}.BuildAttributeValueSyntax(global::MLIR.Semantics.ConstantAttributeFactory.String({element}))");

    private static readonly DensePrimitiveArrayAttributeConstraintCodeStrategy DenseBooleanArrayStrategy = new(
        attributeValueTypeName: "IReadOnlyList<bool>",
        baseType: "global::MLIR.DenseArrayAttr",
        assemblyFormatType: "DenseBooleanArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        primitiveBaseConstructor: "context, StructuredAttributeSemanticDecoder.DecodeBooleanItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)",
        valueConstructorParameter: "global::System.Collections.Generic.IReadOnlyList<bool>");

    private static readonly DensePrimitiveArrayAttributeConstraintCodeStrategy DenseIntegerArrayStrategy = new(
        attributeValueTypeName: "IReadOnlyList<global::MLIR.Numerics.ApInt>",
        baseType: "global::MLIR.DenseArrayAttr",
        assemblyFormatType: "DenseIntegerArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: null,
        primitiveBaseConstructor: "context, StructuredAttributeSemanticDecoder.DecodeIntegerItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)",
        valueConstructorParameter: "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Numerics.ApInt>");

    private static readonly DensePrimitiveArrayAttributeConstraintCodeStrategy DenseF32ArrayStrategy = new(
        attributeValueTypeName: "IReadOnlyList<global::MLIR.Numerics.ApFloat>",
        baseType: "global::MLIR.DenseArrayAttr",
        assemblyFormatType: "DenseFloatingPointArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new DenseFloatingPointArrayAttributeAssemblyFormat(\"f32\")",
        primitiveBaseConstructor: "context, StructuredAttributeSemanticDecoder.DecodeFloatingPointItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items, global::MLIR.Numerics.FloatSemantics.IEEESingle)",
        valueConstructorParameter: "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Numerics.ApFloat>");

    private static readonly DensePrimitiveArrayAttributeConstraintCodeStrategy DenseF64ArrayStrategy = new(
        attributeValueTypeName: "IReadOnlyList<global::MLIR.Numerics.ApFloat>",
        baseType: "global::MLIR.DenseArrayAttr",
        assemblyFormatType: "DenseFloatingPointArrayAttributeAssemblyFormat",
        assemblyFormatConstructionExpression: "new DenseFloatingPointArrayAttributeAssemblyFormat(\"f64\")",
        primitiveBaseConstructor: "context, StructuredAttributeSemanticDecoder.DecodeFloatingPointItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items, global::MLIR.Numerics.FloatSemantics.IEEEDouble)",
        valueConstructorParameter: "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Numerics.ApFloat>");

    /// <summary>
    /// Returns the strategy singleton for the given <paramref name="kind"/> and
    /// <paramref name="recordName"/>.  Returns <see cref="FallbackAttributeConstraintCodeStrategy.Instance"/>
    /// for unrecognised kinds (including <see cref="AttributeConstraintKind.None"/> and
    /// <see cref="AttributeConstraintKind.DenseArrayAttribute"/>).
    /// </summary>
    /// <param name="kind">The constraint kind from the ODS model.</param>
    /// <param name="recordName">
    /// The ODS record name; used to distinguish F32/F64 from generic floating-point.
    /// </param>
    public static AttributeConstraintCodeStrategy GetStrategy(AttributeConstraintKind kind, string recordName)
    {
        return kind switch
        {
            AttributeConstraintKind.BooleanLiteral => BooleanLiteralStrategy,
            AttributeConstraintKind.IntegerLiteral => IntegerLiteralStrategy,
            AttributeConstraintKind.FloatingPointLiteral => GetFloatingPointStrategy(recordName),
            AttributeConstraintKind.StringLiteral => StringLiteralStrategy,
            AttributeConstraintKind.OpaqueAttribute => OpaqueAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.ElementsAttribute => ElementsAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DictionaryAttribute => DictionaryAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.TypeAttribute => TypeAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.UnitAttribute => UnitAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DenseBooleanArrayAttribute => DenseBooleanArrayStrategy,
            AttributeConstraintKind.DenseIntegerArrayAttribute => DenseIntegerArrayStrategy,
            AttributeConstraintKind.DenseF32ArrayAttribute => DenseF32ArrayStrategy,
            AttributeConstraintKind.DenseF64ArrayAttribute => DenseF64ArrayStrategy,
            AttributeConstraintKind.EnumAttribute => EnumAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.TypedArrayAttribute => TypedArrayConstraintCodeStrategy.Instance,
            _ => FallbackAttributeConstraintCodeStrategy.Instance,
        };
    }

    private static AttributeConstraintCodeStrategy GetFloatingPointStrategy(string recordName)
    {
        return recordName switch
        {
            "Builtin_FloatAttr" => GenericFloatingPointLiteralStrategy,
            "F16Attr" => F16Strategy,
            "F32Attr" => F32Strategy,
            "BF16Attr" => BF16Strategy,
            "F64Attr" => F64Strategy,
            _ => throw new System.NotSupportedException($"Unsupported floating-point attribute constraint '{recordName}'."),
        };
    }
}
