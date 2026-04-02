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
    /// The constraint parses a type attribute whose payload is a nested type.
    /// </summary>
    TypeAttribute = 6,

    /// <summary>
    /// The constraint parses a unit attribute literal.
    /// </summary>
    UnitAttribute = 7,
}
