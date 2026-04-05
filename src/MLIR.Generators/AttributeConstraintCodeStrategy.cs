namespace MLIR.Generators;

using MLIR.ODS.Model;

/// <summary>
/// Encapsulates the code-generation behavior associated with a specific kind of attribute
/// constraint. Concrete subclasses replace the per-kind switch expressions that previously
/// appeared across <see cref="Emitters.AttributeTypeResolver"/>,
/// <see cref="Emitters.OperationMemberPlanner"/>,
/// <see cref="Emitters.OperationAttributeValueHelpers"/>,
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
    /// Required unit attributes are typed <c>UnitAttributeValue</c>; optional ones are
    /// exposed as <c>bool</c> (present/absent) rather than <c>UnitAttributeValue?</c>.
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
    /// from an already-cast constraint instance (e.g. <c>.Value</c>, <c>.TypedValue</c>,
    /// or <c>.LiteralText</c>). Only called when <see cref="IsPrimitive"/> is
    /// <see langword="true"/>.
    /// </summary>
    /// <param name="typeName">The C# type name of the member, including any trailing <c>?</c>.</param>
    public virtual string GetPrimitiveValueAccess(string typeName) => ".Value";

    // -------------------------------------------------------------------------
    // Type name resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the C# type name that represents the unwrapped attribute value for this
    /// constraint (e.g. <c>"bool"</c>, <c>"BigInteger"</c>, <c>"TypeSyntax"</c>), or
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
}

// =============================================================================
// Concrete strategy implementations
// =============================================================================

/// <summary>Boolean literal attribute (e.g. <c>BoolAttr</c>).</summary>
internal sealed class BooleanLiteralConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly BooleanLiteralConstraintCodeStrategy Instance = new();
    private BooleanLiteralConstraintCodeStrategy() { }

    public override bool IsPrimitive => true;
    public override bool UsesTypedArrayElementPayload => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "bool";
    public override string GetBaseType(string constraintRecordName) => "BooleanAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "BooleanLiteralAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, ((BooleanAttributeValueSyntax)context.Syntax).Value";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "bool";
}

/// <summary>Integer literal attribute (e.g. <c>I32Attr</c>, <c>I64Attr</c>).</summary>
internal sealed class IntegerLiteralConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly IntegerLiteralConstraintCodeStrategy Instance = new();
    private IntegerLiteralConstraintCodeStrategy() { }

    public override bool IsPrimitive => true;
    public override bool UsesTypedArrayElementPayload => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "BigInteger";
    public override string GetBaseType(string constraintRecordName) => "IntegerAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "IntegerLiteralAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, ((IntegerAttributeValueSyntax)context.Syntax).Value";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "global::System.Numerics.BigInteger";
}

/// <summary>
/// Generic floating-point literal attribute (e.g. <c>BF16Attr</c> and other non-F32/F64
/// float attributes). Exposes the literal text as a <c>string</c>.
/// </summary>
internal sealed class GenericFloatingPointLiteralConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly GenericFloatingPointLiteralConstraintCodeStrategy Instance = new();
    private GenericFloatingPointLiteralConstraintCodeStrategy() { }

    public override bool IsPrimitive => true;
    public override bool UsesTypedArrayElementPayload => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "string";
    public override string GetPrimitiveValueAccess(string typeName) => ".LiteralText";
    public override string GetBaseType(string constraintRecordName) => "FloatingPointAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "FloatingPointLiteralAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, ((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "string";
}

/// <summary>Single-precision floating-point attribute (<c>F32Attr</c>).</summary>
internal sealed class F32ConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly F32ConstraintCodeStrategy Instance = new();
    private F32ConstraintCodeStrategy() { }

    public override bool IsPrimitive => true;
    public override bool UsesTypedArrayElementPayload => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "float";
    public override string GetBaseType(string constraintRecordName) => "F32AttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "F32AttributeAssemblyFormat";

    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) =>
        "context, global::MLIR.Semantics.Attributes.Primitives.FloatingPointLiteralParser.ParseSingle(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText)";

    public override string? GetValueConstructorParameter(string constraintRecordName) => "float";
}

/// <summary>Double-precision floating-point attribute (<c>F64Attr</c>).</summary>
internal sealed class F64ConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly F64ConstraintCodeStrategy Instance = new();
    private F64ConstraintCodeStrategy() { }

    public override bool IsPrimitive => true;
    public override bool UsesTypedArrayElementPayload => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "double";
    public override string GetBaseType(string constraintRecordName) => "F64AttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "F64AttributeAssemblyFormat";

    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) =>
        "context, global::MLIR.Semantics.Attributes.Primitives.FloatingPointLiteralParser.ParseDouble(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText)";

    public override string? GetValueConstructorParameter(string constraintRecordName) => "double";
}

/// <summary>String literal attribute (e.g. <c>StrAttr</c>).</summary>
internal sealed class StringLiteralConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly StringLiteralConstraintCodeStrategy Instance = new();
    private StringLiteralConstraintCodeStrategy() { }

    public override bool IsPrimitive => true;
    public override bool UsesTypedArrayElementPayload => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "string";
    public override string GetBaseType(string constraintRecordName) => "StringAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "StringLiteralAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, ((StringAttributeValueSyntax)context.Syntax).Value";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "string";
}

/// <summary>
/// Opaque attribute (e.g. <c>AnyAttr</c>, <c>LocationAttr</c>). Preserved as a generic
/// <c>OpaqueAttributeValue</c>; falls back to <c>AttributeValue</c> when used as a
/// typed-array element.
/// </summary>
internal sealed class OpaqueAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly OpaqueAttributeConstraintCodeStrategy Instance = new();
    private OpaqueAttributeConstraintCodeStrategy() { }

    public override bool IsGenericTypedArrayElement => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "OpaqueAttributeValue";
    public override string GetBaseType(string constraintRecordName) => "OpaqueAttributeValue";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context";
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
/// <c>DictionaryAttributeValue</c>; the unwrapped value type for typed-array elements
/// is <c>NamedAttributeCollection</c>.
/// </summary>
internal sealed class DictionaryAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly DictionaryAttributeConstraintCodeStrategy Instance = new();
    private DictionaryAttributeConstraintCodeStrategy() { }

    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "Attributes";

    /// <summary>
    /// Returns <c>"NamedAttributeCollection"</c> – the unwrapped value type used for
    /// typed-array element extraction. Note that this is the unwrapped type regardless of
    /// whether the constraint is classified as primitive (it is not).
    /// </summary>
    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "NamedAttributeCollection";

    /// <summary>
    /// Returns <c>"DictionaryAttributeValue"</c> (required) or
    /// <c>"DictionaryAttributeValue?"</c> (optional) – the type used for operation
    /// member properties, which wraps the attribute in its constraint class.
    /// </summary>
    public override string GetOperationPropertyTypeName(string constraintRecordName, bool isRequired, DialectSymbolResolver resolver) =>
        isRequired ? "DictionaryAttributeValue" : "DictionaryAttributeValue?";

    public override string GetBaseType(string constraintRecordName) => "DictionaryAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "DictionaryAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, DecodeAttributes(context.Syntax)";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "global::MLIR.Semantics.NamedAttributeCollection";

    public override void EmitInnerHelpers(System.Text.StringBuilder builder, string className)
    {
        builder.AppendLine();
        builder.AppendLine("    private static global::MLIR.Semantics.NamedAttributeCollection DecodeAttributes(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return syntax is global::MLIR.Syntax.Attributes.Collections.DictionaryAttributeValueSyntax dictionarySyntax");
        builder.AppendLine("            ? global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes(dictionarySyntax.Attributes.Items)");
        builder.AppendLine("            : global::MLIR.Semantics.NamedAttributeCollection.Empty;");
        builder.AppendLine("    }");
    }
}

/// <summary>
/// Type attribute (<c>TypeAttr</c>). Properties are exposed as
/// <c>TypeAttributeValue</c>; the unwrapped value for typed-array elements is
/// <c>TypeSyntax</c>.
/// </summary>
internal sealed class TypeAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly TypeAttributeConstraintCodeStrategy Instance = new();
    private TypeAttributeConstraintCodeStrategy() { }

    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "TypeSyntax";

    /// <summary>
    /// Returns <c>"TypeSyntax"</c> – the unwrapped value type used for typed-array
    /// element extraction.
    /// </summary>
    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "TypeSyntax";

    /// <summary>
    /// Returns <c>"TypeAttributeValue"</c> (required) or <c>"TypeAttributeValue?"</c>
    /// (optional) – the type used for operation member properties.
    /// </summary>
    public override string GetOperationPropertyTypeName(string constraintRecordName, bool isRequired, DialectSymbolResolver resolver) =>
        isRequired ? "TypeAttributeValue" : "TypeAttributeValue?";

    public override string GetBaseType(string constraintRecordName) => "TypeAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "TypeAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, DecodeTypeSyntax(context.Syntax)";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "global::MLIR.Syntax.TypeSyntax";

    public override void EmitInnerHelpers(System.Text.StringBuilder builder, string className)
    {
        builder.AppendLine();
        builder.AppendLine("    private static global::MLIR.Syntax.TypeSyntax DecodeTypeSyntax(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return syntax is global::MLIR.Syntax.Attributes.TypeAttributeValueSyntax typeSyntax");
        builder.AppendLine("            ? typeSyntax.TypeSyntax");
        builder.AppendLine("            : new global::MLIR.Syntax.RawTypeSyntax(syntax?.GetRawText() ?? new global::MLIR.Syntax.RawSyntaxText(string.Empty));");
        builder.AppendLine("    }");
    }
}

/// <summary>
/// Unit attribute (<c>UnitAttr</c>). Optional properties are exposed as <c>bool</c>
/// rather than <c>UnitAttributeValue?</c>. Falls back to <c>AttributeValue</c> when
/// used as a typed-array element.
/// </summary>
internal sealed class UnitAttributeConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly UnitAttributeConstraintCodeStrategy Instance = new();
    private UnitAttributeConstraintCodeStrategy() { }

    public override bool IsUnit => true;
    public override bool IsGenericTypedArrayElement => true;

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "UnitAttributeValue";

    /// <summary>
    /// Required unit attributes are typed <c>UnitAttributeValue</c>; optional ones are
    /// exposed as <c>bool</c> (present/absent) rather than <c>UnitAttributeValue?</c>.
    /// </summary>
    public override string GetOperationPropertyTypeName(string constraintRecordName, bool isRequired, DialectSymbolResolver resolver) =>
        isRequired ? "UnitAttributeValue" : "bool";

    public override string GetBaseType(string constraintRecordName) => "UnitAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "UnitAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context";
}

/// <summary>Dense boolean array attribute (<c>DenseBoolArrayAttr</c>).</summary>
internal sealed class DenseBooleanArrayConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly DenseBooleanArrayConstraintCodeStrategy Instance = new();
    private DenseBooleanArrayConstraintCodeStrategy() { }

    public override bool IsDenseCollection => true;
    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "Items";

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "IReadOnlyList<bool>";
    public override string GetBaseType(string constraintRecordName) => "DenseBooleanArrayAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "DenseBooleanArrayAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, StructuredAttributeSemanticDecoder.DecodeBooleanItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "global::System.Collections.Generic.IReadOnlyList<bool>";
}

/// <summary>Dense integer array attribute (<c>DenseI32ArrayAttr</c>, etc.).</summary>
internal sealed class DenseIntegerArrayConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly DenseIntegerArrayConstraintCodeStrategy Instance = new();
    private DenseIntegerArrayConstraintCodeStrategy() { }

    public override bool IsDenseCollection => true;
    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "Items";

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "IReadOnlyList<BigInteger>";
    public override string GetBaseType(string constraintRecordName) => "DenseIntegerArrayAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "DenseIntegerArrayAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, StructuredAttributeSemanticDecoder.DecodeIntegerItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "global::System.Collections.Generic.IReadOnlyList<global::System.Numerics.BigInteger>";
}

/// <summary>Dense single-precision float array attribute (<c>DenseF32ArrayAttr</c>).</summary>
internal sealed class DenseF32ArrayConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly DenseF32ArrayConstraintCodeStrategy Instance = new();
    private DenseF32ArrayConstraintCodeStrategy() { }

    public override bool IsDenseCollection => true;
    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "Items";

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "IReadOnlyList<float>";
    public override string GetBaseType(string constraintRecordName) => "DenseF32ArrayAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "DenseF32ArrayAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, StructuredAttributeSemanticDecoder.DecodeSinglePrecisionItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "global::System.Collections.Generic.IReadOnlyList<float>";
}

/// <summary>Dense double-precision float array attribute (<c>DenseF64ArrayAttr</c>).</summary>
internal sealed class DenseF64ArrayConstraintCodeStrategy : AttributeConstraintCodeStrategy
{
    public static readonly DenseF64ArrayConstraintCodeStrategy Instance = new();
    private DenseF64ArrayConstraintCodeStrategy() { }

    public override bool IsDenseCollection => true;
    public override bool UsesTypedArrayElementPayload => true;
    public override string GetTypedArrayElementPayloadPropertyName() => "Items";

    public override string? GetAttributeValueTypeName(string constraintRecordName, DialectSymbolResolver resolver) => "IReadOnlyList<double>";
    public override string GetBaseType(string constraintRecordName) => "DenseF64ArrayAttributeValue";
    public override string? GetAssemblyFormatType(string constraintRecordName) => "DenseF64ArrayAttributeAssemblyFormat";
    public override string? GetPrimitiveBaseConstructor(string constraintRecordName) => "context, StructuredAttributeSemanticDecoder.DecodeDoublePrecisionItems(((DenseArrayAttributeValueSyntax)context.Syntax).Items.Items)";
    public override string? GetValueConstructorParameter(string constraintRecordName) => "global::System.Collections.Generic.IReadOnlyList<double>";
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
        elementTypeName == "UnitAttributeValue"
        || elementTypeName == "OpaqueAttributeValue"
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
            AttributeConstraintKind.BooleanLiteral => BooleanLiteralConstraintCodeStrategy.Instance,
            AttributeConstraintKind.IntegerLiteral => IntegerLiteralConstraintCodeStrategy.Instance,
            AttributeConstraintKind.FloatingPointLiteral => GetFloatingPointStrategy(recordName),
            AttributeConstraintKind.StringLiteral => StringLiteralConstraintCodeStrategy.Instance,
            AttributeConstraintKind.OpaqueAttribute => OpaqueAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.ElementsAttribute => ElementsAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DictionaryAttribute => DictionaryAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.TypeAttribute => TypeAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.UnitAttribute => UnitAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DenseBooleanArrayAttribute => DenseBooleanArrayConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DenseIntegerArrayAttribute => DenseIntegerArrayConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DenseF32ArrayAttribute => DenseF32ArrayConstraintCodeStrategy.Instance,
            AttributeConstraintKind.DenseF64ArrayAttribute => DenseF64ArrayConstraintCodeStrategy.Instance,
            AttributeConstraintKind.EnumAttribute => EnumAttributeConstraintCodeStrategy.Instance,
            AttributeConstraintKind.TypedArrayAttribute => TypedArrayConstraintCodeStrategy.Instance,
            _ => FallbackAttributeConstraintCodeStrategy.Instance,
        };
    }

    private static AttributeConstraintCodeStrategy GetFloatingPointStrategy(string recordName)
    {
        return recordName switch
        {
            "F32Attr" => F32ConstraintCodeStrategy.Instance,
            "F64Attr" => F64ConstraintCodeStrategy.Instance,
            _ => GenericFloatingPointLiteralConstraintCodeStrategy.Instance,
        };
    }
}
