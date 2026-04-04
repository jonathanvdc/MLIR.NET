namespace MLIR.ODS.Model;

/// <summary>
/// Describes the supported parser/binder behavior for an attribute constraint.
/// </summary>
public enum AttributeConstraintKind
{
    /// <summary>
    /// No specialized parsing behavior is known.
    /// </summary>
    None = 0,

    /// <summary>
    /// The constraint parses a primitive boolean literal.
    /// </summary>
    BooleanLiteral = 1,

    /// <summary>
    /// The constraint parses a primitive integer literal.
    /// </summary>
    IntegerLiteral = 2,

    /// <summary>
    /// The constraint parses a primitive floating-point literal.
    /// </summary>
    FloatingPointLiteral = 3,

    /// <summary>
    /// The constraint parses a primitive string literal.
    /// </summary>
    StringLiteral = 4,

    /// <summary>
    /// The constraint is recognized but preserved as opaque attribute syntax.
    /// </summary>
    OpaqueAttribute = 5,

    /// <summary>
    /// The constraint parses a dense array attribute.
    /// </summary>
    DenseArrayAttribute = 6,

    /// <summary>
    /// The constraint parses an elements attribute.
    /// </summary>
    ElementsAttribute = 7,

    /// <summary>
    /// The constraint parses a dictionary attribute.
    /// </summary>
    DictionaryAttribute = 8,

    /// <summary>
    /// The constraint parses a type attribute whose payload is a nested type.
    /// </summary>
    TypeAttribute = 9,

    /// <summary>
    /// The constraint parses a unit attribute literal.
    /// </summary>
    UnitAttribute = 10,

    /// <summary>
    /// The constraint parses a dense array of boolean values (<c>array&lt;i1: ...&gt;</c>).
    /// </summary>
    DenseBooleanArrayAttribute = 11,

    /// <summary>
    /// The constraint parses a dense array of integer values (<c>array&lt;i32: ...&gt;</c>, etc.).
    /// </summary>
    DenseIntegerArrayAttribute = 12,

    /// <summary>
    /// The constraint parses a dense array of single-precision floating-point values (<c>array&lt;f32: ...&gt;</c>).
    /// </summary>
    DenseF32ArrayAttribute = 13,

    /// <summary>
    /// The constraint parses a dense array of double-precision floating-point values (<c>array&lt;f64: ...&gt;</c>).
    /// </summary>
    DenseF64ArrayAttribute = 14,

    /// <summary>
    /// The constraint parses an enum keyword (or a combination of enum keywords for bit enums).
    /// </summary>
    EnumAttribute = 15,

    /// <summary>
    /// The constraint parses an array of typed attribute values.
    /// </summary>
    TypedArrayAttribute = 16,
}
