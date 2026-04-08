namespace MLIR.Numerics;

/// <summary>
/// Identifies the high-level classification of a floating-point value.
/// </summary>
public enum FloatCategory
{
    /// <summary>
    /// The value is zero, either positive or negative.
    /// </summary>
    Zero,

    /// <summary>
    /// The value is a finite, normalized nonzero number.
    /// </summary>
    Normal,

    /// <summary>
    /// The value is a finite, subnormal nonzero number.
    /// </summary>
    Subnormal,

    /// <summary>
    /// The value is positive or negative infinity.
    /// </summary>
    Infinity,

    /// <summary>
    /// The value is not a number.
    /// </summary>
    NaN
}
