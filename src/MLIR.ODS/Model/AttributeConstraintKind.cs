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
    /// The constraint parses a primitive integer literal.
    /// </summary>
    IntegerLiteral = 1,
}
