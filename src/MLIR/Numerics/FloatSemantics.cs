using System;

namespace MLIR.Numerics;

/// <summary>
/// Describes the semantic format of a floating-point value.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FloatSemantics"/> defines how an <see cref="ApFloat"/> value is interpreted,
/// including exponent width, significand precision, and whether the format supports
/// IEEE-style infinities, NaNs, and subnormals.
/// </para>
///
/// <para>
/// Two <see cref="ApFloat"/> values with different semantics are not interchangeable
/// without an explicit conversion.
/// </para>
/// </remarks>
public sealed class FloatSemantics : IEquatable<FloatSemantics>
{
    /// <summary>
    /// Gets the number of bits in the exponent field.
    /// </summary>
    public int ExponentBits { get; }

    /// <summary>
    /// Gets the precision of the significand in bits.
    /// </summary>
    ///
    /// <remarks>
    /// This is the total precision of the significand, including any implicit leading bit
    /// when the format uses one.
    /// </remarks>
    public int Precision { get; }

    /// <summary>
    /// Gets whether normal finite values use an implicit leading significand bit.
    /// </summary>
    public bool HasImplicitLeadingBit { get; }

    /// <summary>
    /// Gets whether the format supports infinities.
    /// </summary>
    public bool HasInfinity { get; }

    /// <summary>
    /// Gets whether the format supports NaN values.
    /// </summary>
    public bool HasNaN { get; }

    /// <summary>
    /// Gets whether the format supports subnormal values.
    /// </summary>
    public bool SupportsSubnormals { get; }

    /// <summary>
    /// Gets the total number of bits in the encoded representation, including sign, exponent, and significand fields.
    /// </summary>
    public int BitWidth { get; }

    /// <summary>
    /// Gets a predefined semantics instance corresponding to bfloat16.
    /// </summary>
    public static FloatSemantics BFloat16 { get; } = new FloatSemantics(8, 8, true, true, true, true);

    /// <summary>
    /// Gets a predefined semantics instance corresponding to TF32.
    /// </summary>
    public static FloatSemantics TF32 { get; } = new FloatSemantics(8, 11, true, true, true, true);

    /// <summary>
    /// Gets a predefined semantics instance corresponding to IEEE binary32.
    /// </summary>
    public static FloatSemantics IEEESingle { get; } = new FloatSemantics(8, 24, true, true, true, true);

    /// <summary>
    /// Gets a predefined semantics instance corresponding to IEEE binary64.
    /// </summary>
    public static FloatSemantics IEEEDouble { get; } = new FloatSemantics(11, 53, true, true, true, true);

    /// <summary>
    /// Gets a predefined semantics instance corresponding to IEEE binary16.
    /// </summary>
    public static FloatSemantics IEEEHalf { get; } = new FloatSemantics(5, 11, true, true, true, true);

    /// <summary>
    /// Gets a predefined semantics instance corresponding to x87 80-bit extended precision.
    /// </summary>
    public static FloatSemantics IEEEExtended80 { get; } = new FloatSemantics(15, 64, false, true, true, true);

    /// <summary>
    /// Gets a predefined semantics instance corresponding to IEEE binary128.
    /// </summary>
    public static FloatSemantics IEEEQuadruple { get; } = new FloatSemantics(15, 113, true, true, true, true);

    /// <summary>
    /// Creates a floating-point semantics descriptor.
    /// </summary>
    /// <param name="exponentBits">The number of exponent bits.</param>
    /// <param name="precision">The significand precision in bits.</param>
    /// <param name="hasImplicitLeadingBit">
    /// <see langword="true"/> if normal values use an implicit leading significand bit.
    /// </param>
    /// <param name="hasInfinity"><see langword="true"/> if the format supports infinity values.</param>
    /// <param name="hasNaN"><see langword="true"/> if the format supports NaN values.</param>
    /// <param name="supportsSubnormals"><see langword="true"/> if the format supports subnormals.</param>
    public FloatSemantics(
        int exponentBits,
        int precision,
        bool hasImplicitLeadingBit,
        bool hasInfinity,
        bool hasNaN,
        bool supportsSubnormals)
    {
        if (exponentBits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponentBits));
        }

        if (precision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(precision));
        }

        ExponentBits = exponentBits;
        Precision = precision;
        HasImplicitLeadingBit = hasImplicitLeadingBit;
        HasInfinity = hasInfinity;
        HasNaN = hasNaN;
        SupportsSubnormals = supportsSubnormals;
        BitWidth = 1 + exponentBits + FractionBits;
    }

    /// <summary>
    /// Determines whether this semantics descriptor is equal to another.
    /// </summary>
    public bool Equals(FloatSemantics? other)
    {
        return other is not null
            && ExponentBits == other.ExponentBits
            && Precision == other.Precision
            && HasImplicitLeadingBit == other.HasImplicitLeadingBit
            && HasInfinity == other.HasInfinity
            && HasNaN == other.HasNaN
            && SupportsSubnormals == other.SupportsSubnormals;
    }

    /// <summary>
    /// Determines whether the specified object is equal to this semantics descriptor.
    /// </summary>
    public override bool Equals(object? obj) => obj is FloatSemantics other && Equals(other);

    /// <summary>
    /// Returns a hash code for this semantics descriptor.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ExponentBits;
            hash = (hash * 397) ^ Precision;
            hash = (hash * 397) ^ HasImplicitLeadingBit.GetHashCode();
            hash = (hash * 397) ^ HasInfinity.GetHashCode();
            hash = (hash * 397) ^ HasNaN.GetHashCode();
            hash = (hash * 397) ^ SupportsSubnormals.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Returns a diagnostic string representation of this semantics descriptor.
    /// </summary>
    public override string ToString()
    {
        if (Equals(IEEEHalf))
        {
            return "binary16";
        }

        if (Equals(BFloat16))
        {
            return "bfloat16";
        }

        if (Equals(TF32))
        {
            return "tf32";
        }

        if (Equals(IEEESingle))
        {
            return "binary32";
        }

        if (Equals(IEEEDouble))
        {
            return "binary64";
        }

        if (Equals(IEEEExtended80)) return "x87extended80";

        if (Equals(IEEEQuadruple)) return "binary128";

        return $"FloatSemantics(expBits={ExponentBits}, precision={Precision}, implicitLeadingBit={HasImplicitLeadingBit}, infinity={HasInfinity}, nan={HasNaN}, subnormals={SupportsSubnormals})";
    }

    internal int FractionBits => HasImplicitLeadingBit ? Precision - 1 : Precision;
}
