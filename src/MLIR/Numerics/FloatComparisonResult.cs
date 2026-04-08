namespace MLIR.Numerics;

/// <summary>
/// Represents the result of comparing two floating-point values.
/// </summary>
public enum FloatComparisonResult
{
    /// <summary>
    /// The left operand is less than the right operand.
    /// </summary>
    LessThan,

    /// <summary>
    /// The two operands compare equal.
    /// </summary>
    Equal,

    /// <summary>
    /// The left operand is greater than the right operand.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// The operands are unordered, typically because at least one operand is NaN.
    /// </summary>
    Unordered
}
