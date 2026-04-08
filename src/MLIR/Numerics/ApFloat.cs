using System;
using System.Globalization;
using System.Numerics;

namespace MLIR.Numerics;

/// <summary>
/// Represents an immutable floating-point value interpreted under a specific <see cref="FloatSemantics"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ApFloat"/> carries its semantic format explicitly. The same conceptual numeric
/// value may have different encodings, ranges, and rounding behavior under different semantics.
/// </para>
///
/// <para>
/// Unlike <see cref="ApInt"/>, floating-point values do have intrinsic semantics that must travel
/// with the value. A binary32 value and a binary64 value are not the same kind of object.
/// </para>
/// </remarks>
public readonly struct ApFloat : IEquatable<ApFloat>
{
    private readonly ApInt bits;

    private ApFloat(FloatSemantics semantics, ApInt bits)
    {
        Semantics = semantics ?? throw new ArgumentNullException(nameof(semantics));
        if (bits.BitWidth != semantics.BitWidth)
        {
            throw new ArgumentException("The encoded bit pattern does not match the floating-point semantics.", nameof(bits));
        }

        this.bits = bits;
    }

    /// <summary>
    /// Gets the semantic format under which this value is interpreted.
    /// </summary>
    public FloatSemantics Semantics { get; }

    /// <summary>
    /// Gets the high-level classification of this value.
    /// </summary>
    public FloatCategory Category => Classify();

    /// <summary>
    /// Gets whether the sign bit is set.
    /// </summary>
    ///
    /// <remarks>
    /// The sign bit may be set for zero, infinity, and NaN values as well as finite nonzero values.
    /// </remarks>
    public bool Sign => bits.BitWidth > 0 && bits.TestBit(bits.BitWidth - 1);

    /// <summary>
    /// Gets whether this value is zero.
    /// </summary>
    public bool IsZero => Category == FloatCategory.Zero;

    /// <summary>
    /// Gets whether this value is finite.
    /// </summary>
    public bool IsFinite => Category == FloatCategory.Zero
        || Category == FloatCategory.Normal
        || Category == FloatCategory.Subnormal;

    /// <summary>
    /// Gets whether this value is infinite.
    /// </summary>
    public bool IsInfinity => Category == FloatCategory.Infinity;

    /// <summary>
    /// Gets whether this value is NaN.
    /// </summary>
    public bool IsNaN => Category == FloatCategory.NaN;

    /// <summary>
    /// Gets whether this value is a normal finite number.
    /// </summary>
    public bool IsNormal => Category == FloatCategory.Normal;

    /// <summary>
    /// Gets whether this value is a subnormal finite number.
    /// </summary>
    public bool IsSubnormal => Category == FloatCategory.Subnormal;

    /// <summary>
    /// Gets whether this value is negative zero.
    /// </summary>
    public bool IsNegativeZero => IsZero && Sign;

    /// <summary>
    /// Creates a zero value under the specified semantics.
    /// </summary>
    /// <param name="semantics">The floating-point semantics of the result.</param>
    /// <param name="negative">
    /// <see langword="true"/> to create negative zero; otherwise, positive zero.
    /// </param>
    /// <returns>A zero value in the specified format.</returns>
    public static ApFloat Zero(FloatSemantics semantics, bool negative = false)
    {
        return CreateFromRawBits(semantics, negative ? SignMask(semantics) : BigInteger.Zero);
    }

    /// <summary>
    /// Creates an infinity value under the specified semantics.
    /// </summary>
    /// <param name="semantics">The floating-point semantics of the result.</param>
    /// <param name="negative">
    /// <see langword="true"/> to create negative infinity; otherwise, positive infinity.
    /// </param>
    /// <returns>An infinity value in the specified format.</returns>
    ///
    /// <exception cref="NotSupportedException">
    /// Thrown if <paramref name="semantics"/> does not support infinity values.
    /// </exception>
    public static ApFloat Infinity(FloatSemantics semantics, bool negative = false)
    {
        if (!semantics.HasInfinity)
        {
            throw new NotSupportedException("The requested semantics do not support infinity values.");
        }

        BigInteger rawBits = ExponentMask(semantics) << semantics.FractionBits;
        if (negative)
        {
            rawBits |= SignMask(semantics);
        }

        return CreateFromRawBits(semantics, rawBits);
    }

    /// <summary>
    /// Creates a quiet NaN value under the specified semantics.
    /// </summary>
    /// <param name="semantics">The floating-point semantics of the result.</param>
    /// <param name="negative">Whether to set the sign bit of the NaN.</param>
    /// <returns>A quiet NaN value in the specified format.</returns>
    ///
    /// <exception cref="NotSupportedException">
    /// Thrown if <paramref name="semantics"/> does not support NaN values.
    /// </exception>
    public static ApFloat QuietNaN(FloatSemantics semantics, bool negative = false)
    {
        if (!semantics.HasNaN)
        {
            throw new NotSupportedException("The requested semantics do not support NaN values.");
        }

        BigInteger rawBits = ExponentMask(semantics) << semantics.FractionBits;
        if (semantics.FractionBits > 0)
        {
            rawBits |= BigInteger.One << (semantics.FractionBits - 1);
        }

        if (negative)
        {
            rawBits |= SignMask(semantics);
        }

        return CreateFromRawBits(semantics, rawBits);
    }

    /// <summary>
    /// Creates a value from a <see cref="float"/>, rounding if necessary to the requested semantics.
    /// </summary>
    /// <param name="semantics">The semantics of the resulting value.</param>
    /// <param name="value">The source floating-point value.</param>
    /// <param name="roundingMode">The rounding mode used if conversion is inexact.</param>
    public static ApFloat FromSingle(
        FloatSemantics semantics,
        float value,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        return FromDouble(semantics, value, roundingMode);
    }

    /// <summary>
    /// Creates a value from a <see cref="double"/>, rounding if necessary to the requested semantics.
    /// </summary>
    /// <param name="semantics">The semantics of the resulting value.</param>
    /// <param name="value">The source floating-point value.</param>
    /// <param name="roundingMode">The rounding mode used if conversion is inexact.</param>
    public static ApFloat FromDouble(
        FloatSemantics semantics,
        double value,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        return RoundFromExactDouble(semantics, value, roundingMode);
    }

    /// <summary>
    /// Creates a value from an integer bitvector, interpreting it as a signed integer.
    /// </summary>
    /// <param name="semantics">The semantics of the resulting floating-point value.</param>
    /// <param name="value">The integer value to convert.</param>
    /// <param name="roundingMode">The rounding mode used if conversion is inexact.</param>
    public static ApFloat FromSignedInteger(
        FloatSemantics semantics,
        ApInt value,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        return FromDouble(semantics, (double)value.ToBigIntegerSigned(), roundingMode);
    }

    /// <summary>
    /// Creates a value from an integer bitvector, interpreting it as an unsigned integer.
    /// </summary>
    /// <param name="semantics">The semantics of the resulting floating-point value.</param>
    /// <param name="value">The integer value to convert.</param>
    /// <param name="roundingMode">The rounding mode used if conversion is inexact.</param>
    public static ApFloat FromUnsignedInteger(
        FloatSemantics semantics,
        ApInt value,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        return FromDouble(semantics, (double)value.ToBigIntegerUnsigned(), roundingMode);
    }

    /// <summary>
    /// Parses a floating-point literal using the specified semantics.
    /// </summary>
    /// <param name="semantics">The semantics of the resulting value.</param>
    /// <param name="text">The textual representation to parse.</param>
    /// <param name="roundingMode">The rounding mode used if parsing is inexact.</param>
    /// <returns>The parsed floating-point value.</returns>
    public static ApFloat Parse(
        FloatSemantics semantics,
        string text,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        if (semantics is null)
        {
            throw new ArgumentNullException(nameof(semantics));
        }

        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("The input string was empty.");
        }

        if (string.Equals(trimmed, "nan", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "+nan", StringComparison.OrdinalIgnoreCase))
        {
            return QuietNaN(semantics, negative: false);
        }

        if (string.Equals(trimmed, "-nan", StringComparison.OrdinalIgnoreCase))
        {
            return QuietNaN(semantics, negative: true);
        }

        if (string.Equals(trimmed, "inf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "+inf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "infinity", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "+infinity", StringComparison.OrdinalIgnoreCase))
        {
            return Infinity(semantics, negative: false);
        }

        if (string.Equals(trimmed, "-inf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "-infinity", StringComparison.OrdinalIgnoreCase))
        {
            return Infinity(semantics, negative: true);
        }

        // MLIR hex float literal: raw IEEE-754 bit pattern encoded as 0x<hex>.
        // Prepend a "0" digit so that BigInteger.TryParse always treats the value as
        // non-negative (without a leading sign, BigInteger's hex parser is sign-magnitude
        // but a leading '8' through 'F' nybble would still give a positive result when
        // the extra leading zero is present).
        if (trimmed.Length >= 2
            && trimmed[0] == '0'
            && (trimmed[1] == 'x' || trimmed[1] == 'X'))
        {
            string hexDigits = "0" + trimmed.Substring(2);
            if (!BigInteger.TryParse(hexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out BigInteger rawBits))
            {
                throw new FormatException($"The input string '{text}' was not recognized as a hexadecimal floating-point bit pattern.");
            }

            // Mask to the bit width so any excess high bits (from the leading "0" byte) are dropped.
            rawBits &= RawMask(semantics);
            return CreateFromRawBits(semantics, rawBits);
        }

        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            throw new FormatException($"The input string '{text}' was not recognized as a floating-point number.");
        }

        return FromDouble(semantics, parsed, roundingMode);
    }

    /// <summary>
    /// Converts this value to a <see cref="float"/>, rounding if necessary.
    /// </summary>
    /// <param name="roundingMode">The rounding mode used if conversion is inexact.</param>
    /// <returns>A <see cref="float"/> representation of this value.</returns>
    public float ToSingle(
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        return (float)ConvertTo(FloatSemantics.IEEESingle, roundingMode).DecodeToDouble();
    }

    /// <summary>
    /// Converts this value to a <see cref="double"/>, rounding if necessary.
    /// </summary>
    /// <param name="roundingMode">The rounding mode used if conversion is inexact.</param>
    /// <returns>A <see cref="double"/> representation of this value.</returns>
    public double ToDouble(
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        return ConvertTo(FloatSemantics.IEEEDouble, roundingMode).DecodeToDouble();
    }

    /// <summary>
    /// Converts this value to a signed integer bitvector.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting integer value.</param>
    /// <param name="roundingMode">The rounding mode used for the conversion.</param>
    /// <returns>A signed integer result encoded as an <see cref="ApInt"/>.</returns>
    ///
    /// <remarks>
    /// Floating-point to integer conversions typically use <see cref="FloatingRoundingMode.TowardZero"/>
    /// unless explicitly requested otherwise.
    /// </remarks>
    public ApInt ToSignedInteger(
        int bitWidth,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.TowardZero)
    {
        return RoundToInteger(bitWidth, roundingMode, signed: true);
    }

    /// <summary>
    /// Converts this value to an unsigned integer bitvector.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting integer value.</param>
    /// <param name="roundingMode">The rounding mode used for the conversion.</param>
    /// <returns>An unsigned integer result encoded as an <see cref="ApInt"/>.</returns>
    public ApInt ToUnsignedInteger(
        int bitWidth,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.TowardZero)
    {
        return RoundToInteger(bitWidth, roundingMode, signed: false);
    }

    /// <summary>
    /// Converts this value to a different floating-point semantics.
    /// </summary>
    /// <param name="semantics">The destination semantics.</param>
    /// <param name="roundingMode">The rounding mode used if the conversion is inexact.</param>
    /// <returns>A value represented under the destination semantics.</returns>
    public ApFloat ConvertTo(
        FloatSemantics semantics,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        if (semantics is null)
        {
            throw new ArgumentNullException(nameof(semantics));
        }

        if (Semantics.Equals(semantics))
        {
            return this;
        }

        return FromDouble(semantics, DecodeToDouble(), roundingMode);
    }

    /// <summary>
    /// Returns a value with the same magnitude and the specified sign.
    /// </summary>
    /// <param name="negative">
    /// <see langword="true"/> to set the sign bit; otherwise, clear it.
    /// </param>
    /// <returns>A value with the requested sign.</returns>
    public ApFloat CopySign(bool negative)
    {
        BigInteger rawBits = bits.ToBigIntegerUnsigned();
        if (negative)
        {
            rawBits |= SignMask(Semantics);
        }
        else
        {
            rawBits &= ~SignMask(Semantics);
        }

        return CreateFromRawBits(Semantics, rawBits);
    }

    /// <summary>
    /// Returns the absolute value of this floating-point number.
    /// </summary>
    public ApFloat Abs()
    {
        return CopySign(negative: false);
    }

    /// <summary>
    /// Returns the negation of this floating-point number.
    /// </summary>
    ///
    /// <remarks>
    /// Negation flips the sign bit, including for zero, infinity, and NaN values.
    /// </remarks>
    public ApFloat Negate()
    {
        return CopySign(!Sign);
    }

    /// <summary>
    /// Adds this value to <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The value to add.</param>
    /// <param name="roundingMode">The rounding mode used if the result is inexact.</param>
    /// <returns>The rounded sum.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown if the operands have different semantics.
    /// </exception>
    public ApFloat Add(
        ApFloat other,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        EnsureCompatibleSemantics(other);
        return FromDouble(Semantics, DecodeToDouble() + other.DecodeToDouble(), roundingMode);
    }

    /// <summary>
    /// Subtracts <paramref name="other"/> from this value.
    /// </summary>
    /// <param name="other">The value to subtract.</param>
    /// <param name="roundingMode">The rounding mode used if the result is inexact.</param>
    /// <returns>The rounded difference.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown if the operands have different semantics.
    /// </exception>
    public ApFloat Subtract(
        ApFloat other,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        EnsureCompatibleSemantics(other);
        return FromDouble(Semantics, DecodeToDouble() - other.DecodeToDouble(), roundingMode);
    }

    /// <summary>
    /// Multiplies this value by <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The value to multiply by.</param>
    /// <param name="roundingMode">The rounding mode used if the result is inexact.</param>
    /// <returns>The rounded product.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown if the operands have different semantics.
    /// </exception>
    public ApFloat Multiply(
        ApFloat other,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        EnsureCompatibleSemantics(other);
        return FromDouble(Semantics, DecodeToDouble() * other.DecodeToDouble(), roundingMode);
    }

    /// <summary>
    /// Divides this value by <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The divisor.</param>
    /// <param name="roundingMode">The rounding mode used if the result is inexact.</param>
    /// <returns>The rounded quotient.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown if the operands have different semantics.
    /// </exception>
    public ApFloat Divide(
        ApFloat other,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        EnsureCompatibleSemantics(other);
        return FromDouble(Semantics, DecodeToDouble() / other.DecodeToDouble(), roundingMode);
    }

    /// <summary>
    /// Computes the fused multiply-add of this value and two other operands.
    /// </summary>
    /// <param name="multiplicand">The value to multiply by this one.</param>
    /// <param name="addend">The value to add after multiplication.</param>
    /// <param name="roundingMode">The rounding mode used if the result is inexact.</param>
    /// <returns>The rounded fused multiply-add result.</returns>
    ///
    /// <remarks>
    /// This method models LLVM-style fused multiply-add semantics at the value level.
    /// </remarks>
    public ApFloat FusedMultiplyAdd(
        ApFloat multiplicand,
        ApFloat addend,
        FloatingRoundingMode roundingMode = FloatingRoundingMode.NearestTiesToEven)
    {
        EnsureCompatibleSemantics(multiplicand);
        EnsureCompatibleSemantics(addend);

        double result = DecodeToDouble() * multiplicand.DecodeToDouble() + addend.DecodeToDouble();
        ApFloat rounded = FromDouble(Semantics, result, roundingMode);

        if (rounded.IsZero && roundingMode == FloatingRoundingMode.TowardNegative && !rounded.Sign)
        {
            return Zero(Semantics, negative: true);
        }

        return rounded;
    }

    /// <summary>
    /// Returns the next representable value in the requested direction.
    /// </summary>
    /// <param name="towardNegative">
    /// <see langword="true"/> to move toward negative infinity; otherwise, move toward positive infinity.
    /// </param>
    /// <returns>The adjacent representable value, or this value when no adjacent value exists.</returns>
    public ApFloat Next(bool towardNegative)
    {
        if (IsNaN)
        {
            return this;
        }

        if (IsInfinity)
        {
            if (Sign)
            {
                return towardNegative ? this : MaxFinite(Semantics, negative: true);
            }

            return towardNegative ? MaxFinite(Semantics, negative: false) : this;
        }

        if (IsZero)
        {
            return towardNegative
                ? NegativeSmallestValue(Semantics)
                : PositiveSmallestValue(Semantics);
        }

        BigInteger rawBits = RawBits;
        BigInteger rawMask = RawMask(Semantics);

        if (Sign)
        {
            if (towardNegative)
            {
                if (!Semantics.HasInfinity && rawBits == MaxFiniteRawBits(Semantics, negative: true))
                {
                    return this;
                }

                if (Semantics.HasInfinity && rawBits == MaxFiniteRawBits(Semantics, negative: true))
                {
                    return Infinity(Semantics, negative: true);
                }

                return rawBits == rawMask ? this : CreateFromRawBits(Semantics, rawBits + BigInteger.One);
            }

            return rawBits == SignMask(Semantics) ? PositiveSmallestValue(Semantics) : CreateFromRawBits(Semantics, rawBits - BigInteger.One);
        }

        if (towardNegative)
        {
            return rawBits == BigInteger.Zero ? NegativeSmallestValue(Semantics) : CreateFromRawBits(Semantics, rawBits - BigInteger.One);
        }

        if (!Semantics.HasInfinity && rawBits == MaxFiniteRawBits(Semantics, negative: false))
        {
            return this;
        }

        if (rawBits == rawMask)
        {
            return this;
        }

        if (Semantics.HasInfinity && rawBits == MaxFiniteRawBits(Semantics, negative: false))
        {
            return Infinity(Semantics, negative: false);
        }

        return CreateFromRawBits(Semantics, rawBits + BigInteger.One);
    }

    /// <summary>
    /// Returns the next representable value toward positive infinity.
    /// </summary>
    public ApFloat NextUp() => Next(towardNegative: false);

    /// <summary>
    /// Returns the next representable value toward negative infinity.
    /// </summary>
    public ApFloat NextDown() => Next(towardNegative: true);

    /// <summary>
    /// Compares this value with <paramref name="other"/> using floating-point comparison rules.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>
    /// A comparison result indicating whether this value is less than, equal to, greater than,
    /// or unordered with respect to <paramref name="other"/>.
    /// </returns>
    ///
    /// <remarks>
    /// Comparisons involving NaN are typically unordered.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown if the operands have different semantics.
    /// </exception>
    public FloatComparisonResult Compare(ApFloat other)
    {
        EnsureCompatibleSemantics(other);

        double left = DecodeToDouble();
        double right = other.DecodeToDouble();
        if (double.IsNaN(left) || double.IsNaN(right))
        {
            return FloatComparisonResult.Unordered;
        }

        if (left < right)
        {
            return FloatComparisonResult.LessThan;
        }

        if (left > right)
        {
            return FloatComparisonResult.GreaterThan;
        }

        return FloatComparisonResult.Equal;
    }

    /// <summary>
    /// Determines whether this value is bitwise identical to <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>
    /// <see langword="true"/> if both values have the same semantics and the same encoded bit pattern;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool BitwiseEquals(ApFloat other)
    {
        return Semantics.Equals(other.Semantics) && bits.Equals(other.bits);
    }

    /// <summary>
    /// Determines whether this value is numerically equal to <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>
    /// <see langword="true"/> if the two values are numerically equal under floating-point comparison rules;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    ///
    /// <remarks>
    /// This method is intentionally distinct from <see cref="BitwiseEquals(ApFloat)"/>.
    /// For example, positive zero and negative zero may be numerically equal while not being
    /// bitwise identical.
    /// </remarks>
    public bool NumericEquals(ApFloat other)
    {
        return Compare(other) == FloatComparisonResult.Equal;
    }

    /// <summary>
    /// Returns the raw bit pattern of this value encoded according to <see cref="Semantics"/>.
    /// </summary>
    /// <returns>An <see cref="ApInt"/> whose width matches <see cref="FloatSemantics.BitWidth"/>.</returns>
    public ApInt ToBits()
    {
        return bits;
    }

    /// <summary>
    /// Creates a floating-point value from a raw encoded bit pattern.
    /// </summary>
    /// <param name="semantics">The semantics used to decode the bits.</param>
    /// <param name="bits">The raw bit pattern.</param>
    /// <returns>A floating-point value decoded from <paramref name="bits"/>.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="bits"/> does not have the expected width for <paramref name="semantics"/>.
    /// </exception>
    public static ApFloat FromBits(FloatSemantics semantics, ApInt bits)
    {
        if (semantics is null)
        {
            throw new ArgumentNullException(nameof(semantics));
        }

        if (bits.BitWidth != semantics.BitWidth)
        {
            throw new ArgumentException("The encoded bit pattern does not match the floating-point semantics.", nameof(bits));
        }

        ApFloat value = new ApFloat(semantics, bits);
        switch (value.Category)
        {
            case FloatCategory.Subnormal:
                if (!semantics.SupportsSubnormals)
                {
                    throw new NotSupportedException("The requested semantics do not support subnormal values.");
                }

                break;
            case FloatCategory.Infinity:
                if (!semantics.HasInfinity)
                {
                    throw new NotSupportedException("The requested semantics do not support infinity values.");
                }

                break;
            case FloatCategory.NaN:
                if (!semantics.HasNaN)
                {
                    throw new NotSupportedException("The requested semantics do not support NaN values.");
                }

                break;
        }

        return value;
    }

    /// <summary>
    /// Determines whether this value is equal to another floating-point value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows bitwise equality, including semantic format and payload bits.
    /// In most cases, callers should prefer <see cref="BitwiseEquals(ApFloat)"/> or
    /// <see cref="NumericEquals(ApFloat)"/> when the distinction matters.
    /// </remarks>
    public bool Equals(ApFloat other)
    {
        return BitwiseEquals(other);
    }

    /// <summary>
    /// Determines whether the specified object is equal to this floating-point value.
    /// </summary>
    public override bool Equals(object? obj) => obj is ApFloat other && Equals(other);

    /// <summary>
    /// Returns a hash code for this floating-point value.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Semantics.GetHashCode() * 397) ^ bits.GetHashCode();
        }
    }

    /// <summary>
    /// Returns a diagnostic string representation of this value.
    /// </summary>
    ///
    /// <remarks>
    /// This method is intended for diagnostics and debugging. When a stable or explicitly
    /// rounded textual form is required, prefer a dedicated formatting API if one is later added.
    /// </remarks>
    public override string ToString()
    {
        switch (Category)
        {
            case FloatCategory.Zero:
                return Sign ? "-0" : "0";
            case FloatCategory.Infinity:
                return Sign ? "-Infinity" : "Infinity";
            case FloatCategory.NaN:
                return Sign ? "-NaN" : "NaN";
            default:
                return DecodeToDouble().ToString("R", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Formats this value as an MLIR-compatible floating-point literal string that can be
    /// parsed back by <see cref="Parse"/> and by the MLIR text parser.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="ToString"/>, this method guarantees the result is recognizable as a
    /// floating-point literal (not an integer literal) by ensuring finite values always contain
    /// a decimal point or exponent marker.
    /// </para>
    /// <para>
    /// Special values are formatted as lower-case MLIR keywords:
    /// <c>nan</c>, <c>-nan</c>, <c>inf</c>, <c>-inf</c>, <c>0.0</c>, <c>-0.0</c>.
    /// </para>
    /// </remarks>
    public string ToLiteralText()
    {
        switch (Category)
        {
            case FloatCategory.Zero:
                return Sign ? "-0.0" : "0.0";
            case FloatCategory.Infinity:
                return Sign ? "-inf" : "inf";
            case FloatCategory.NaN:
                return Sign ? "-nan" : "nan";
            default:
                var text = DecodeToDouble().ToString("R", CultureInfo.InvariantCulture);
                // Ensure the output contains a decimal point or exponent so it is
                // recognized as a floating-point literal rather than an integer literal.
                return text.IndexOfAny(['.', 'e', 'E']) >= 0 ? text : text + ".0";
        }
    }

    private static ApFloat CreateFromRawBits(FloatSemantics semantics, BigInteger rawBits)
    {
        return new ApFloat(semantics, ApInt.Parse(semantics.BitWidth, rawBits.ToString(CultureInfo.InvariantCulture)));
    }

    private static BigInteger SignMask(FloatSemantics semantics) => BigInteger.One << (semantics.BitWidth - 1);

    private static BigInteger RawMask(FloatSemantics semantics) => (BigInteger.One << semantics.BitWidth) - 1;

    private static BigInteger ExponentMask(FloatSemantics semantics) => (BigInteger.One << semantics.ExponentBits) - BigInteger.One;

    private BigInteger RawBits => bits.ToBigIntegerUnsigned();

    private BigInteger FractionBitsRaw => GetField(0, Semantics.FractionBits);

    private BigInteger ExponentBitsRaw => GetField(Semantics.FractionBits, Semantics.ExponentBits);

    private FloatCategory Classify()
    {
        if (ExponentBitsRaw.IsZero)
        {
            return FractionBitsRaw.IsZero ? FloatCategory.Zero : FloatCategory.Subnormal;
        }

        if (ExponentBitsRaw == ExponentMask(Semantics))
        {
            if (Semantics.HasInfinity && FractionBitsRaw.IsZero)
            {
                return FloatCategory.Infinity;
            }

            if (Semantics.HasNaN && !FractionBitsRaw.IsZero)
            {
                return FloatCategory.NaN;
            }
        }

        return FloatCategory.Normal;
    }

    private BigInteger GetField(int startBit, int bitCount)
    {
        if (bitCount == 0)
        {
            return BigInteger.Zero;
        }

        BigInteger mask = (BigInteger.One << bitCount) - BigInteger.One;
        return (RawBits >> startBit) & mask;
    }

    private double DecodeToDouble()
    {
        switch (Category)
        {
            case FloatCategory.Zero:
                return Sign ? -0.0d : 0.0d;
            case FloatCategory.Infinity:
                return Sign ? double.NegativeInfinity : double.PositiveInfinity;
            case FloatCategory.NaN:
                return double.NaN;
        }

        int fractionBits = Semantics.FractionBits;
        int bias = GetExponentBias(Semantics.ExponentBits);
        int exponent = (int)ExponentBitsRaw;
        double significand;

        if (Category == FloatCategory.Subnormal)
        {
            significand = (double)FractionBitsRaw / Math.Pow(2.0, fractionBits);
            exponent = 1 - bias;
        }
        else
        {
            significand = 1.0d + ((double)FractionBitsRaw / Math.Pow(2.0, fractionBits));
            exponent -= bias;
        }

        double value = significand * Math.Pow(2.0, exponent);
        return Sign ? -value : value;
    }

    private ApInt RoundToInteger(int bitWidth, FloatingRoundingMode roundingMode, bool signed)
    {
        if (bitWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitWidth));
        }

        if (IsNaN)
        {
            throw new InvalidOperationException("NaN cannot be converted to an integer.");
        }

        if (IsInfinity)
        {
            throw new OverflowException("Infinity cannot be converted to an integer.");
        }

        double value = DecodeToDouble();
        BigInteger rounded = RoundDoubleToBigInteger(value, roundingMode);
        if (!signed && rounded.Sign < 0)
        {
            throw new OverflowException("The value cannot be represented as an unsigned integer.");
        }

        return ApInt.Parse(
            bitWidth,
            rounded.ToString(CultureInfo.InvariantCulture),
            isSigned: signed);
    }

    private static BigInteger RoundDoubleToBigInteger(double value, FloatingRoundingMode roundingMode)
    {
        if (double.IsNaN(value))
        {
            throw new InvalidOperationException("NaN cannot be converted to an integer.");
        }

        if (double.IsInfinity(value))
        {
            throw new OverflowException("Infinity cannot be converted to an integer.");
        }

        bool negative = BitConverter.DoubleToInt64Bits(value) < 0;
        double magnitude = Math.Abs(value);
        BigInteger truncated = new BigInteger(Math.Truncate(magnitude));
        double fractional = magnitude - (double)truncated;

        if (fractional == 0.0d)
        {
            return negative ? -truncated : truncated;
        }

        bool roundUp = false;
        switch (roundingMode)
        {
            case FloatingRoundingMode.TowardZero:
                roundUp = false;
                break;
            case FloatingRoundingMode.TowardPositive:
                roundUp = !negative;
                break;
            case FloatingRoundingMode.TowardNegative:
                roundUp = negative;
                break;
            case FloatingRoundingMode.NearestTiesToEven:
                if (fractional > 0.5d)
                {
                    roundUp = true;
                }
                else if (fractional == 0.5d)
                {
                    roundUp = !truncated.IsEven;
                }

                break;
        }

        if (roundUp)
        {
            truncated += BigInteger.One;
        }

        return negative ? -truncated : truncated;
    }

    private static ApFloat RoundFromExactDouble(FloatSemantics semantics, double value, FloatingRoundingMode roundingMode)
    {
        if (semantics is null)
        {
            throw new ArgumentNullException(nameof(semantics));
        }

        if (double.IsNaN(value))
        {
            if (!semantics.HasNaN)
            {
                throw new NotSupportedException("The requested semantics do not support NaN values.");
            }

            return QuietNaN(semantics, negative: BitConverter.DoubleToInt64Bits(value) < 0);
        }

        if (double.IsPositiveInfinity(value))
        {
            return semantics.HasInfinity ? Infinity(semantics) : MaxFinite(semantics, negative: false);
        }

        if (double.IsNegativeInfinity(value))
        {
            return semantics.HasInfinity ? Infinity(semantics, negative: true) : MaxFinite(semantics, negative: true);
        }

        bool negative = BitConverter.DoubleToInt64Bits(value) < 0;
        double magnitude = Math.Abs(value);
        if (magnitude == 0.0d)
        {
            return Zero(semantics, negative);
        }

        (BigInteger mantissa, int exponent2) = DecomposeDouble(magnitude);
        int fractionBits = semantics.FractionBits;
        int bias = GetExponentBias(semantics.ExponentBits);
        int minimumNormalExponent = 1 - bias;
        int maxExponentField = (int)GetMaxExponentField(semantics);
        int maximumNormalExponent = maxExponentField - bias;
        int actualExponent = GetBitLength(mantissa) - 1 + exponent2;

        if (actualExponent > maximumNormalExponent)
        {
            return OverflowResult(semantics, negative, roundingMode);
        }

        if (actualExponent >= minimumNormalExponent)
        {
            int shift = exponent2 + fractionBits - actualExponent;
            BigInteger roundedSignificand = RoundScaledMagnitude(mantissa, shift, roundingMode, negative);
            BigInteger implicitOne = BigInteger.One << fractionBits;
            BigInteger exclusiveUpperBound = BigInteger.One << (fractionBits + 1);

            if (roundedSignificand >= exclusiveUpperBound)
            {
                roundedSignificand >>= 1;
                actualExponent++;
                if (actualExponent > maximumNormalExponent)
                {
                    return OverflowResult(semantics, negative, roundingMode);
                }
            }

            if (roundedSignificand < implicitOne)
            {
                return RoundSubnormal(semantics, negative, mantissa, exponent2, roundingMode);
            }

            BigInteger exponentField = new BigInteger(actualExponent + bias);
            BigInteger fractionField = roundedSignificand - implicitOne;
            return CreateFromRawBits(semantics, ComposeRawBits(semantics, negative, exponentField, fractionField));
        }

        return RoundSubnormal(semantics, negative, mantissa, exponent2, roundingMode);
    }

    private static ApFloat RoundSubnormal(
        FloatSemantics semantics,
        bool negative,
        BigInteger mantissa,
        int exponent2,
        FloatingRoundingMode roundingMode)
    {
        int fractionBits = semantics.FractionBits;
        int bias = GetExponentBias(semantics.ExponentBits);
        int shift = exponent2 + fractionBits - (1 - bias);
        BigInteger rounded = RoundScaledMagnitude(mantissa, shift, roundingMode, negative);

        if (rounded.IsZero)
        {
            return Zero(semantics, negative);
        }

        BigInteger implicitOne = BigInteger.One << fractionBits;
        if (rounded >= implicitOne)
        {
            return CreateFromRawBits(
                semantics,
                ComposeRawBits(semantics, negative, BigInteger.One, rounded - implicitOne));
        }

        if (!semantics.SupportsSubnormals)
        {
            return Zero(semantics, negative);
        }

        return CreateFromRawBits(
            semantics,
            ComposeRawBits(semantics, negative, BigInteger.Zero, rounded));
    }

    private static BigInteger RoundScaledMagnitude(
        BigInteger magnitude,
        int shift,
        FloatingRoundingMode roundingMode,
        bool negative)
    {
        if (shift >= 0)
        {
            return magnitude << shift;
        }

        int divisorBits = -shift;
        BigInteger divisor = BigInteger.One << divisorBits;
        BigInteger quotient = BigInteger.DivRem(magnitude, divisor, out BigInteger remainder);
        if (remainder.IsZero)
        {
            return quotient;
        }

        switch (roundingMode)
        {
            case FloatingRoundingMode.TowardZero:
                return quotient;
            case FloatingRoundingMode.TowardPositive:
                return negative ? quotient : quotient + BigInteger.One;
            case FloatingRoundingMode.TowardNegative:
                return negative ? quotient + BigInteger.One : quotient;
            case FloatingRoundingMode.NearestTiesToEven:
                BigInteger doubledRemainder = remainder << 1;
                if (doubledRemainder > divisor)
                {
                    return quotient + BigInteger.One;
                }

                if (doubledRemainder < divisor)
                {
                    return quotient;
                }

                return quotient.IsEven ? quotient : quotient + BigInteger.One;
            default:
                return quotient;
        }
    }

    private static ApFloat OverflowResult(FloatSemantics semantics, bool negative, FloatingRoundingMode roundingMode)
    {
        if (!semantics.HasInfinity)
        {
            return MaxFinite(semantics, negative);
        }

        switch (roundingMode)
        {
            case FloatingRoundingMode.TowardZero:
                return MaxFinite(semantics, negative);
            case FloatingRoundingMode.TowardPositive:
                return negative ? MaxFinite(semantics, negative) : Infinity(semantics, negative: false);
            case FloatingRoundingMode.TowardNegative:
                return negative ? Infinity(semantics, negative: true) : MaxFinite(semantics, negative: false);
            default:
                return Infinity(semantics, negative);
        }
    }

    private static ApFloat MaxFinite(FloatSemantics semantics, bool negative)
    {
        int fractionBits = semantics.FractionBits;
        BigInteger exponentField = GetMaxExponentField(semantics);
        BigInteger fractionField = (BigInteger.One << fractionBits) - 1;
        return CreateFromRawBits(semantics, ComposeRawBits(semantics, negative, exponentField, fractionField));
    }

    private static BigInteger ComposeRawBits(
        FloatSemantics semantics,
        bool negative,
        BigInteger exponentField,
        BigInteger fractionField)
    {
        BigInteger rawBits = fractionField;
        rawBits |= exponentField << semantics.FractionBits;
        if (negative)
        {
            rawBits |= SignMask(semantics);
        }

        return rawBits;
    }

    private static int GetExponentBias(int exponentBits)
    {
        if (exponentBits == 0)
        {
            return 0;
        }

        return (1 << (exponentBits - 1)) - 1;
    }

    private static BigInteger GetMaxExponentField(FloatSemantics semantics)
    {
        if (semantics.ExponentBits == 0)
        {
            return BigInteger.Zero;
        }

        BigInteger allOnes = (BigInteger.One << semantics.ExponentBits) - 1;
        return semantics.HasInfinity || semantics.HasNaN ? allOnes - BigInteger.One : allOnes;
    }

    private static BigInteger MaxFiniteRawBits(FloatSemantics semantics, bool negative)
    {
        BigInteger rawBits = ComposeRawBits(
            semantics,
            negative,
            GetMaxExponentField(semantics),
            (BigInteger.One << semantics.FractionBits) - 1);
        return rawBits;
    }

    private static ApFloat PositiveSmallestValue(FloatSemantics semantics)
    {
        return CreateFromRawBits(semantics, BigInteger.One);
    }

    private static ApFloat NegativeSmallestValue(FloatSemantics semantics)
    {
        return CreateFromRawBits(semantics, SignMask(semantics) | BigInteger.One);
    }

    private static int GetBitLength(BigInteger value)
    {
        if (value.IsZero)
        {
            return 0;
        }

        byte[] bytes = value.ToByteArray();
        byte mostSignificantByte = bytes[bytes.Length - 1];
        int leadingBits = 0;
        for (int bit = 7; bit >= 0; bit--)
        {
            if (((mostSignificantByte >> bit) & 1) != 0)
            {
                leadingBits = bit + 1;
                break;
            }
        }

        return (bytes.Length - 1) * 8 + leadingBits;
    }

    private static (BigInteger Mantissa, int Exponent2) DecomposeDouble(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        int exponentField = (int)((bits >> 52) & 0x7FF);
        long fractionField = bits & 0x000F_FFFF_FFFF_FFFFL;

        if (exponentField == 0)
        {
            return (new BigInteger(fractionField), -1074);
        }

        BigInteger mantissa = new BigInteger((1L << 52) | fractionField);
        return (mantissa, exponentField - 1075);
    }

    private static void EnsureCompatibleSemantics(ApFloat left, ApFloat right)
    {
        if (!left.Semantics.Equals(right.Semantics))
        {
            throw new ArgumentException("ApFloat values must have the same semantics.", nameof(right));
        }
    }

    private void EnsureCompatibleSemantics(ApFloat other) => EnsureCompatibleSemantics(this, other);
}
