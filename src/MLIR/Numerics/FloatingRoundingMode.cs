namespace MLIR.Numerics;

/// <summary>
/// Specifies the rounding mode used for floating-point conversion and arithmetic.
/// </summary>
public enum FloatingRoundingMode
{
    /// <summary>
    /// Round to the nearest representable value, choosing the one with an even least significant bit on ties.
    /// </summary>
    NearestTiesToEven,

    /// <summary>
    /// Round toward zero.
    /// </summary>
    TowardZero,

    /// <summary>
    /// Round toward positive infinity.
    /// </summary>
    TowardPositive,

    /// <summary>
    /// Round toward negative infinity.
    /// </summary>
    TowardNegative
}
