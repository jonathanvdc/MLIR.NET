using System.Globalization;
using System.Numerics;
using System.Text;

namespace MLIR.Numerics;

/// <summary>
/// Represents an immutable fixed-width integer value.
///
/// <para>
/// <see cref="ApInt"/> models a bitvector of exactly <see cref="BitWidth"/> bits.
/// It does not have an intrinsic signedness: the same underlying bits may be
/// interpreted as either signed or unsigned depending on the operation being used.
/// </para>
///
/// <para>
/// For example, the 8-bit pattern <c>11111111</c> represents:
/// </para>
/// <list type="bullet">
/// <item><description><c>255</c> when interpreted as unsigned</description></item>
/// <item><description><c>-1</c> when interpreted as signed two's-complement</description></item>
/// </list>
///
/// <para>
/// This matches the way integer values are typically modeled in compiler IRs,
/// where signedness is part of an operation such as signed division or unsigned
/// comparison rather than part of the value itself.
/// </para>
/// </summary>
public readonly struct ApInt : IEquatable<ApInt>
{
    private readonly BigInteger value;

    /// <summary>
    /// Initializes a new normalized fixed-width integer value.
    /// </summary>
    private ApInt(int bitWidth, BigInteger value)
    {
        BitWidth = ValidateBitWidth(bitWidth);
        this.value = Normalize(bitWidth, value);
    }

    /// <summary>
    /// Gets the exact number of bits in this value.
    /// </summary>
    ///
    /// <remarks>
    /// Arithmetic and bitwise operations are defined over exactly this many bits.
    /// Arithmetic results wrap modulo <c>2^BitWidth</c>.
    /// </remarks>
    public int BitWidth { get; }

    /// <summary>
    /// Gets whether this value is equal to zero.
    /// </summary>
    public bool IsZero => value.IsZero;

    /// <summary>
    /// Gets whether this value is equal to one.
    /// </summary>
    public bool IsOne => BitWidth > 0 && value.IsOne;

    /// <summary>
    /// Gets whether all bits in this value are set.
    /// </summary>
    ///
    /// <remarks>
    /// For an N-bit integer, this is the value <c>2^N - 1</c>.
    /// Under signed two's-complement interpretation, this corresponds to <c>-1</c>.
    /// </remarks>
    public bool IsAllOnes => BitWidth == 0 || value == Mask(BitWidth);

    /// <summary>
    /// Gets the most significant bit.
    /// </summary>
    ///
    /// <remarks>
    /// This property does not imply that the value is signed. It simply exposes the
    /// highest-order bit, which would act as the sign bit under signed interpretation.
    /// </remarks>
    public bool SignBit => BitWidth > 0 && TestBitUnchecked(value, BitWidth - 1);

    /// <summary>
    /// Gets whether this value is negative under signed two's-complement interpretation.
    /// </summary>
    public bool IsNegative => SignBit;

    /// <summary>
    /// Gets whether this value is non-negative under signed two's-complement interpretation.
    /// </summary>
    public bool IsNonNegative => !SignBit;

    /// <summary>
    /// Gets whether this value is strictly greater than zero under signed interpretation.
    /// </summary>
    public bool IsStrictlyPositive => IsNonNegative && !IsZero;

    /// <summary>
    /// Gets whether this value is less than or equal to zero under signed interpretation.
    /// </summary>
    public bool IsNonPositive => !IsStrictlyPositive;

    /// <summary>
    /// Gets whether this value is the maximum unsigned value for its width.
    /// </summary>
    public bool IsMaxValue => IsAllOnes;

    /// <summary>
    /// Gets whether this value is the minimum unsigned value for its width.
    /// </summary>
    public bool IsMinValue => IsZero;

    /// <summary>
    /// Gets whether this value is the maximum signed value for its width.
    /// </summary>
    public bool IsMaxSignedValue => BitWidth > 0 && value == ((BigInteger.One << (BitWidth - 1)) - BigInteger.One);

    /// <summary>
    /// Gets whether this value is the minimum signed value for its width.
    /// </summary>
    public bool IsMinSignedValue => BitWidth > 0 && value == (BigInteger.One << (BitWidth - 1));

    /// <summary>
    /// Creates a fixed-width zero value.
    /// </summary>
    /// <param name="bitWidth">The number of bits in the result.</param>
    /// <returns>An <see cref="ApInt"/> of the specified width containing all zero bits.</returns>
    public static ApInt Zero(int bitWidth) => new(bitWidth, BigInteger.Zero);

    /// <summary>
    /// Creates a fixed-width value equal to one.
    /// </summary>
    /// <param name="bitWidth">The number of bits in the result.</param>
    /// <returns>An <see cref="ApInt"/> of the specified width representing the value one.</returns>
    public static ApInt One(int bitWidth) => new(bitWidth, BigInteger.One);

    /// <summary>
    /// Creates a fixed-width value whose bits are all set.
    /// </summary>
    /// <param name="bitWidth">The number of bits in the result.</param>
    /// <returns>An <see cref="ApInt"/> of the specified width containing all one bits.</returns>
    public static ApInt AllOnes(int bitWidth) => new(bitWidth, Mask(bitWidth));

    /// <summary>
    /// Creates a fixed-width value with a single bit set.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting value.</param>
    /// <param name="bitNo">The zero-based bit index to set.</param>
    public static ApInt GetOneBitSet(int bitWidth, int bitNo) => new(bitWidth, SingleBitMask(bitWidth, bitNo));

    /// <summary>
    /// Creates a zero-width value.
    /// </summary>
    public static ApInt GetZeroWidth() => Zero(0);

    /// <summary>
    /// Creates the maximum unsigned value for a bit width.
    /// </summary>
    public static ApInt GetMaxValue(int bitWidth) => AllOnes(bitWidth);

    /// <summary>
    /// Creates the maximum signed value for a bit width.
    /// </summary>
    public static ApInt GetSignedMaxValue(int bitWidth)
    {
        if (bitWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitWidth));
        }

        return bitWidth == 0 ? Zero(0) : new ApInt(bitWidth, (BigInteger.One << (bitWidth - 1)) - BigInteger.One);
    }

    /// <summary>
    /// Creates the minimum unsigned value for a bit width.
    /// </summary>
    public static ApInt GetMinValue(int bitWidth) => Zero(bitWidth);

    /// <summary>
    /// Creates the minimum signed value for a bit width.
    /// </summary>
    public static ApInt GetSignedMinValue(int bitWidth)
    {
        if (bitWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitWidth));
        }

        return bitWidth == 0 ? Zero(0) : new ApInt(bitWidth, BigInteger.One << (bitWidth - 1));
    }

    /// <summary>
    /// Creates the sign mask for a bit width.
    /// </summary>
    public static ApInt GetSignMask(int bitWidth) => GetSignedMinValue(bitWidth);

    /// <summary>
    /// Creates a fixed-width value with bits from <paramref name="loBit"/> to <paramref name="hiBit"/> set.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting value.</param>
    /// <param name="loBit">The first bit to set, inclusive.</param>
    /// <param name="hiBit">The bit after the last bit to set, exclusive.</param>
    public static ApInt GetBitsSet(int bitWidth, int loBit, int hiBit) => new ApInt(bitWidth, RangeMask(bitWidth, loBit, hiBit));

    /// <summary>
    /// Creates a fixed-width value with a wrapped bit range set.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting value.</param>
    /// <param name="loBit">The first bit to set, inclusive.</param>
    /// <param name="hiBit">The bit after the last bit to set, exclusive.</param>
    public static ApInt GetBitsSetWithWrap(int bitWidth, int loBit, int hiBit) => new ApInt(bitWidth, RangeMaskWithWrap(bitWidth, loBit, hiBit));

    /// <summary>
    /// Creates a fixed-width value with bits from <paramref name="loBit"/> to the top set.
    /// </summary>
    public static ApInt GetBitsSetFrom(int bitWidth, int loBit) => GetBitsSet(bitWidth, loBit, bitWidth);

    /// <summary>
    /// Creates a fixed-width value with the top <paramref name="hiBitsSet"/> bits set.
    /// </summary>
    public static ApInt GetHighBitsSet(int bitWidth, int hiBitsSet) => GetBitsSet(bitWidth, bitWidth - ValidateRangeCount(bitWidth, hiBitsSet), bitWidth);

    /// <summary>
    /// Creates a fixed-width value with the bottom <paramref name="loBitsSet"/> bits set.
    /// </summary>
    public static ApInt GetLowBitsSet(int bitWidth, int loBitsSet) => GetBitsSet(bitWidth, 0, ValidateRangeCount(bitWidth, loBitsSet));

    /// <summary>
    /// Creates a fixed-width value from an unsigned 64-bit integer.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting bitvector.</param>
    /// <param name="value">The source unsigned value.</param>
    /// <returns>
    /// An <see cref="ApInt"/> whose low bits are taken from <paramref name="value"/>,
    /// truncated to <paramref name="bitWidth"/> if necessary.
    /// </returns>
    public static ApInt FromUInt64(int bitWidth, ulong value) => new ApInt(bitWidth, new BigInteger(value));

    /// <summary>
    /// Creates a fixed-width value from a signed 64-bit integer.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting bitvector.</param>
    /// <param name="value">The source signed value.</param>
    /// <returns>
    /// An <see cref="ApInt"/> whose bit pattern is the two's-complement representation
    /// of <paramref name="value"/>, truncated to <paramref name="bitWidth"/> if necessary.
    /// </returns>
    public static ApInt FromInt64(int bitWidth, long value) => new ApInt(bitWidth, new BigInteger(value));

    /// <summary>
    /// Parses a textual integer literal into a fixed-width bitvector.
    /// </summary>
    /// <param name="bitWidth">The width of the resulting bitvector.</param>
    /// <param name="text">The textual representation to parse.</param>
    /// <param name="radix">The base of the textual representation, typically 2, 10, or 16.</param>
    /// <param name="isSigned">
    /// <see langword="true"/> to interpret the input text as a signed integer literal;
    /// otherwise, interpret it as unsigned.
    /// </param>
    /// <returns>The parsed value, encoded into exactly <paramref name="bitWidth"/> bits.</returns>
    ///
    /// <remarks>
    /// The <paramref name="isSigned"/> parameter affects parsing only. It does not attach
    /// permanent signedness to the resulting <see cref="ApInt"/>.
    /// </remarks>
    public static ApInt Parse(int bitWidth, string text, int radix = 10, bool isSigned = false)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        BigInteger parsed = ParseText(text, radix, isSigned);
        return new ApInt(bitWidth, parsed);
    }

    /// <summary>
    /// Converts this value to an unsigned 64-bit integer.
    /// </summary>
    /// <returns>The current bit pattern interpreted as an unsigned integer.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the value cannot be represented as an unsigned 64-bit integer without loss.
    /// </exception>
    public ulong ToUInt64() => (ulong)value;

    /// <summary>
    /// Converts this value to a signed 64-bit integer using two's-complement interpretation.
    /// </summary>
    /// <returns>The current bit pattern interpreted as a signed integer.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the value cannot be represented as a signed 64-bit integer without loss.
    /// </exception>
    public long ToInt64() => (long)ToBigIntegerSigned();

    /// <summary>
    /// Converts this value to an arbitrarily large unsigned integer.
    /// </summary>
    /// <returns>The current bit pattern interpreted as an unsigned integer.</returns>
    public BigInteger ToBigIntegerUnsigned() => value;

    /// <summary>
    /// Converts this value to an arbitrarily large signed integer using two's-complement interpretation.
    /// </summary>
    /// <returns>The current bit pattern interpreted as a signed integer.</returns>
    public BigInteger ToBigIntegerSigned()
    {
        if (!SignBit)
        {
            return value;
        }

        return value - (BigInteger.One << BitWidth);
    }

    /// <summary>
    /// Compares this value with <paramref name="other"/> using unsigned interpretation.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>
    /// A negative value if this value is less than <paramref name="other"/>,
    /// zero if they are equal, or a positive value if this value is greater.
    /// </returns>
    public int CompareUnsigned(ApInt other)
    {
        EnsureCompatibleWidth(other);
        return value.CompareTo(other.value);
    }

    /// <summary>
    /// Compares this value with <paramref name="other"/> using signed two's-complement interpretation.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>
    /// A negative value if this value is less than <paramref name="other"/>,
    /// zero if they are equal, or a positive value if this value is greater.
    /// </returns>
    public int CompareSigned(ApInt other)
    {
        EnsureCompatibleWidth(other);
        return ToBigIntegerSigned().CompareTo(other.ToBigIntegerSigned());
    }

    /// <summary>
    /// Gets a comparer that orders <see cref="ApInt"/> values using unsigned interpretation.
    /// </summary>
    public static IComparer<ApInt> UnsignedComparer { get; } = new UnsignedApIntComparer();

    /// <summary>
    /// Gets a comparer that orders <see cref="ApInt"/> values using signed two's-complement interpretation.
    /// </summary>
    public static IComparer<ApInt> SignedComparer { get; } = new SignedApIntComparer();

    /// <summary>
    /// Determines whether this value is less than <paramref name="other"/> under unsigned interpretation.
    /// </summary>
    public bool ULessThan(ApInt other)
    {
        return CompareUnsigned(other) < 0;
    }

    /// <summary>
    /// Determines whether this value is less than or equal to <paramref name="other"/> under unsigned interpretation.
    /// </summary>
    public bool ULessThanOrEqual(ApInt other)
    {
        return CompareUnsigned(other) <= 0;
    }

    /// <summary>
    /// Determines whether this value is greater than <paramref name="other"/> under unsigned interpretation.
    /// </summary>
    public bool UGreaterThan(ApInt other)
    {
        return CompareUnsigned(other) > 0;
    }

    /// <summary>
    /// Determines whether this value is greater than or equal to <paramref name="other"/> under unsigned interpretation.
    /// </summary>
    public bool UGreaterThanOrEqual(ApInt other)
    {
        return CompareUnsigned(other) >= 0;
    }

    /// <summary>
    /// Determines whether this value is less than <paramref name="other"/> under signed two's-complement interpretation.
    /// </summary>
    public bool SLessThan(ApInt other)
    {
        return CompareSigned(other) < 0;
    }

    /// <summary>
    /// Determines whether this value is less than or equal to <paramref name="other"/> under signed two's-complement interpretation.
    /// </summary>
    public bool SLessThanOrEqual(ApInt other)
    {
        return CompareSigned(other) <= 0;
    }

    /// <summary>
    /// Determines whether this value is greater than <paramref name="other"/> under signed two's-complement interpretation.
    /// </summary>
    public bool SGreaterThan(ApInt other)
    {
        return CompareSigned(other) > 0;
    }

    /// <summary>
    /// Determines whether this value is greater than or equal to <paramref name="other"/> under signed two's-complement interpretation.
    /// </summary>
    public bool SGreaterThanOrEqual(ApInt other)
    {
        return CompareSigned(other) >= 0;
    }

    /// <summary>
    /// Narrows this value to the specified bit width by discarding high-order bits.
    /// </summary>
    /// <param name="bitWidth">
    /// The width of the resulting value. Must be less than or equal to <see cref="BitWidth"/>.
    /// </param>
    /// <returns>A truncated value with the specified width.</returns>
    public ApInt Trunc(int bitWidth)
    {
        bitWidth = ValidateBitWidth(bitWidth);
        if (bitWidth > BitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "The requested width must not exceed the current bit width.");
        }

        return new ApInt(bitWidth, value);
    }

    /// <summary>
    /// Widens this value to the specified bit width by inserting zero bits on the left.
    /// </summary>
    /// <param name="bitWidth">
    /// The width of the resulting value. Must be greater than or equal to <see cref="BitWidth"/>.
    /// </param>
    /// <returns>A zero-extended value with the specified width.</returns>
    public ApInt ZeroExtend(int bitWidth)
    {
        bitWidth = ValidateBitWidth(bitWidth);
        if (bitWidth < BitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "The requested width must not be smaller than the current bit width.");
        }

        return new ApInt(bitWidth, value);
    }

    /// <summary>
    /// Widens this value to the specified bit width by replicating the most significant bit.
    /// </summary>
    /// <param name="bitWidth">
    /// The width of the resulting value. Must be greater than or equal to <see cref="BitWidth"/>.
    /// </param>
    /// <returns>A sign-extended value with the specified width.</returns>
    ///
    /// <remarks>
    /// This operation uses signed interpretation only to determine how the new high bits
    /// are filled. It does not attach permanent signedness to the result.
    /// </remarks>
    public ApInt SignExtend(int bitWidth)
    {
        bitWidth = ValidateBitWidth(bitWidth);
        if (bitWidth < BitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "The requested width must not be smaller than the current bit width.");
        }

        return new ApInt(bitWidth, ToBigIntegerSigned());
    }

    /// <summary>
    /// Shifts this value left by the specified number of bit positions.
    /// </summary>
    /// <param name="amount">The number of bits to shift.</param>
    /// <returns>The shifted value, truncated back to the original width.</returns>
    public ApInt Shl(int amount)
    {
        ValidateShiftAmount(amount);
        if (amount >= BitWidth)
        {
            return Zero(BitWidth);
        }

        return new ApInt(BitWidth, value << amount);
    }

    /// <summary>
    /// Shifts this value right by the specified number of bit positions, inserting zero bits from the left.
    /// </summary>
    /// <param name="amount">The number of bits to shift.</param>
    /// <returns>The logically shifted value.</returns>
    public ApInt LShr(int amount)
    {
        ValidateShiftAmount(amount);
        if (amount >= BitWidth)
        {
            return Zero(BitWidth);
        }

        return new ApInt(BitWidth, value >> amount);
    }

    /// <summary>
    /// Shifts this value right by the specified number of bit positions, replicating the most significant bit.
    /// </summary>
    /// <param name="amount">The number of bits to shift.</param>
    /// <returns>The arithmetically shifted value.</returns>
    public ApInt AShr(int amount)
    {
        ValidateShiftAmount(amount);
        if (amount >= BitWidth)
        {
            return SignBit ? AllOnes(BitWidth) : Zero(BitWidth);
        }

        return new ApInt(BitWidth, ToBigIntegerSigned() >> amount);
    }

    /// <summary>
    /// Counts the number of one bits in this value.
    /// </summary>
    public int PopCount()
    {
        int count = 0;
        byte[] bytes = value.ToByteArray();
        for (int i = 0; i < bytes.Length; i++)
        {
            count += PopCountByte(bytes[i]);
        }

        return count;
    }

    /// <summary>
    /// Counts the number of consecutive zero bits starting at the most significant bit.
    /// </summary>
    public int CountLeadingZeros()
    {
        if (BitWidth == 0)
        {
            return 0;
        }

        if (IsZero)
        {
            return BitWidth;
        }

        int bitLength = GetBitLength(value);
        return BitWidth - bitLength;
    }

    /// <summary>
    /// Counts the number of consecutive zero bits starting at the least significant bit.
    /// </summary>
    public int CountTrailingZeros()
    {
        if (BitWidth == 0)
        {
            return 0;
        }

        if (IsZero)
        {
            return BitWidth;
        }

        int count = 0;
        byte[] bytes = value.ToByteArray();
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            if (b == 0)
            {
                count += 8;
                continue;
            }

            count += TrailingZeroCountByte(b);
            return count;
        }

        return count;
    }

    /// <summary>
    /// Determines whether the bit at the specified index is set.
    /// </summary>
    /// <param name="bitIndex">The zero-based bit index, where 0 is the least significant bit.</param>
    public bool TestBit(int bitIndex)
    {
        ValidateBitIndex(bitIndex);
        return TestBitUnchecked(value, bitIndex);
    }

    /// <summary>
    /// Returns a value equal to this one except that the bit at <paramref name="bitIndex"/> is set.
    /// </summary>
    public ApInt SetBit(int bitIndex)
    {
        ValidateBitIndex(bitIndex);
        return new ApInt(BitWidth, value | (BigInteger.One << bitIndex));
    }

    /// <summary>
    /// Returns a value equal to this one except that the bit at <paramref name="bitIndex"/> is cleared.
    /// </summary>
    public ApInt ClearBit(int bitIndex)
    {
        ValidateBitIndex(bitIndex);
        BigInteger cleared = value & ~ (BigInteger.One << bitIndex);
        return new ApInt(BitWidth, cleared);
    }

    /// <summary>
    /// Determines whether this value has exactly one bit set at the specified index.
    /// </summary>
    public bool IsOneBitSet(int bitIndex)
    {
        ValidateBitIndex(bitIndex);
        return TestBit(bitIndex) && PopCount() == 1;
    }

    /// <summary>
    /// Determines whether this value is a power of two in its unsigned interpretation.
    /// </summary>
    public bool IsPowerOf2() => !IsZero && (value & (value - BigInteger.One)) == BigInteger.Zero;

    /// <summary>
    /// Determines whether the negated signed interpretation is a power of two.
    /// </summary>
    public bool IsNegatedPowerOf2()
    {
        if (BitWidth == 0 || !IsNegative)
        {
            return false;
        }

        BigInteger magnitude = BigInteger.Abs(ToBigIntegerSigned());
        return magnitude > BigInteger.Zero && (magnitude & (magnitude - BigInteger.One)) == BigInteger.Zero;
    }

    /// <summary>
    /// Returns a value equal to this one except that the bits in
    /// <paramref name="loBit"/>..<paramref name="hiBit"/> are set.
    /// </summary>
    public ApInt SetBits(int loBit, int hiBit)
    {
        ValidateBitRange(loBit, hiBit);
        return new ApInt(BitWidth, value | RangeMask(BitWidth, loBit, hiBit));
    }

    /// <summary>
    /// Returns a value equal to this one except that the bits in
    /// <paramref name="loBit"/>..<paramref name="hiBit"/> are cleared.
    /// </summary>
    public ApInt ClearBits(int loBit, int hiBit)
    {
        ValidateBitRange(loBit, hiBit);
        return new ApInt(BitWidth, value & ~RangeMask(BitWidth, loBit, hiBit));
    }

    /// <summary>
    /// Returns a value equal to this one except that the wrapped range of bits is set.
    /// </summary>
    public ApInt SetBitsWithWrap(int loBit, int hiBit)
    {
        ValidateBitIndexForWrap(loBit);
        ValidateBitIndexForWrap(hiBit);
        return new ApInt(BitWidth, value | RangeMaskWithWrap(BitWidth, loBit, hiBit));
    }

    /// <summary>
    /// Returns a value equal to this one except that the wrapped range of bits is cleared.
    /// </summary>
    public ApInt ClearBitsWithWrap(int loBit, int hiBit)
    {
        ValidateBitIndexForWrap(loBit);
        ValidateBitIndexForWrap(hiBit);
        return new ApInt(BitWidth, value & ~RangeMaskWithWrap(BitWidth, loBit, hiBit));
    }

    /// <summary>
    /// Returns a value equal to this one except that all bits from
    /// <paramref name="loBit"/> to the top are set.
    /// </summary>
    public ApInt SetBitsFrom(int loBit)
    {
        ValidateBitIndexForRangeStart(loBit);
        return SetBits(loBit, BitWidth);
    }

    /// <summary>
    /// Returns a value equal to this one except that all bits from
    /// <paramref name="loBit"/> to the top are cleared.
    /// </summary>
    public ApInt ClearBitsFrom(int loBit)
    {
        ValidateBitIndexForRangeStart(loBit);
        return ClearBits(loBit, BitWidth);
    }

    /// <summary>
    /// Returns a value equal to this one except that the top
    /// <paramref name="hiBitsSet"/> bits are set.
    /// </summary>
    public ApInt SetHighBits(int hiBitsSet)
    {
        int count = ValidateRangeCount(BitWidth, hiBitsSet);
        return SetBits(BitWidth - count, BitWidth);
    }

    /// <summary>
    /// Returns a value equal to this one except that the top
    /// <paramref name="hiBitsSet"/> bits are cleared.
    /// </summary>
    public ApInt ClearHighBits(int hiBitsSet)
    {
        int count = ValidateRangeCount(BitWidth, hiBitsSet);
        return ClearBits(BitWidth - count, BitWidth);
    }

    /// <summary>
    /// Returns a value equal to this one except that the bottom
    /// <paramref name="loBitsSet"/> bits are set.
    /// </summary>
    public ApInt SetLowBits(int loBitsSet)
    {
        int count = ValidateRangeCount(BitWidth, loBitsSet);
        return SetBits(0, count);
    }

    /// <summary>
    /// Returns a value equal to this one except that the bottom
    /// <paramref name="loBitsSet"/> bits are cleared.
    /// </summary>
    public ApInt ClearLowBits(int loBitsSet)
    {
        int count = ValidateRangeCount(BitWidth, loBitsSet);
        return ClearBits(0, count);
    }

    /// <summary>
    /// Formats this value as an unsigned integer string.
    /// </summary>
    /// <param name="radix">The output base, typically 2, 10, or 16.</param>
    /// <returns>A textual representation using unsigned interpretation.</returns>
    public string ToStringUnsigned(int radix = 10) => FormatBigInteger(value, radix);

    /// <summary>
    /// Formats this value as a signed integer string using two's-complement interpretation.
    /// </summary>
    /// <param name="radix">The output base, typically 2, 10, or 16.</param>
    /// <returns>A textual representation using signed interpretation.</returns>
    public string ToStringSigned(int radix = 10) => FormatBigInteger(ToBigIntegerSigned(), radix);

    /// <summary>
    /// Adds two values of the same bit width.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The sum modulo <c>2^BitWidth</c>.</returns>
    public static ApInt operator +(ApInt left, ApInt right)
    {
        EnsureCompatibleWidth(left, right);
        return new ApInt(left.BitWidth, left.value + right.value);
    }

    /// <summary>
    /// Subtracts one value from another.
    /// </summary>
    /// <returns>The difference modulo <c>2^BitWidth</c>.</returns>
    public static ApInt operator -(ApInt left, ApInt right)
    {
        EnsureCompatibleWidth(left, right);
        return new ApInt(left.BitWidth, left.value - right.value);
    }

    /// <summary>
    /// Multiplies two values of the same bit width.
    /// </summary>
    /// <returns>The product modulo <c>2^BitWidth</c>.</returns>
    public static ApInt operator *(ApInt left, ApInt right)
    {
        EnsureCompatibleWidth(left, right);
        return new ApInt(left.BitWidth, left.value * right.value);
    }

    /// <summary>
    /// Computes the bitwise AND of two values.
    /// </summary>
    public static ApInt operator &(ApInt left, ApInt right)
    {
        EnsureCompatibleWidth(left, right);
        return new ApInt(left.BitWidth, left.value & right.value);
    }

    /// <summary>
    /// Computes the bitwise OR of two values.
    /// </summary>
    public static ApInt operator |(ApInt left, ApInt right)
    {
        EnsureCompatibleWidth(left, right);
        return new ApInt(left.BitWidth, left.value | right.value);
    }

    /// <summary>
    /// Computes the bitwise exclusive OR of two values.
    /// </summary>
    public static ApInt operator ^(ApInt left, ApInt right)
    {
        EnsureCompatibleWidth(left, right);
        return new ApInt(left.BitWidth, left.value ^ right.value);
    }

    /// <summary>
    /// Computes the bitwise complement of a value.
    /// </summary>
    public static ApInt operator ~(ApInt value)
    {
        return new ApInt(value.BitWidth, Mask(value.BitWidth) ^ value.value);
    }

    /// <summary>
    /// Divides this value by <paramref name="other"/> using unsigned interpretation.
    /// </summary>
    /// <param name="other">The divisor.</param>
    /// <returns>The unsigned quotient.</returns>
    public ApInt UDiv(ApInt other)
    {
        EnsureCompatibleWidth(other);
        if (other.value.IsZero)
        {
            throw new DivideByZeroException();
        }

        return new ApInt(BitWidth, value / other.value);
    }

    /// <summary>
    /// Computes the remainder of unsigned division by <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The divisor.</param>
    /// <returns>The unsigned remainder.</returns>
    public ApInt URem(ApInt other)
    {
        EnsureCompatibleWidth(other);
        if (other.value.IsZero)
        {
            throw new DivideByZeroException();
        }

        return new ApInt(BitWidth, value % other.value);
    }

    /// <summary>
    /// Divides this value by <paramref name="other"/> using signed two's-complement interpretation.
    /// </summary>
    /// <param name="other">The divisor.</param>
    /// <returns>The signed quotient.</returns>
    public ApInt SDiv(ApInt other)
    {
        EnsureCompatibleWidth(other);
        BigInteger divisor = other.ToBigIntegerSigned();
        if (divisor.IsZero)
        {
            throw new DivideByZeroException();
        }

        return new ApInt(BitWidth, BigInteger.DivRem(ToBigIntegerSigned(), divisor, out _));
    }

    /// <summary>
    /// Computes the remainder of signed division by <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The divisor.</param>
    /// <returns>The signed remainder.</returns>
    public ApInt SRem(ApInt other)
    {
        EnsureCompatibleWidth(other);
        BigInteger divisor = other.ToBigIntegerSigned();
        if (divisor.IsZero)
        {
            throw new DivideByZeroException();
        }

        BigInteger.DivRem(ToBigIntegerSigned(), divisor, out BigInteger remainder);
        return new ApInt(BitWidth, remainder);
    }

    /// <summary>
    /// Determines whether two values are bitwise equal and have the same bit width.
    /// </summary>
    public bool Equals(ApInt other)
    {
        return BitWidth == other.BitWidth && value.Equals(other.value);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current value.
    /// </summary>
    public override bool Equals(object? obj) => obj is ApInt other && Equals(other);

    /// <summary>
    /// Returns a hash code for this value.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return (BitWidth * 397) ^ value.GetHashCode();
        }
    }

    /// <summary>
    /// Determines whether two values are bitwise equal and have the same bit width.
    /// </summary>
    public static bool operator ==(ApInt left, ApInt right) => left.Equals(right);

    /// <summary>
    /// Determines whether two values differ in bits or bit width.
    /// </summary>
    public static bool operator !=(ApInt left, ApInt right) => !left.Equals(right);

    /// <summary>
    /// Returns a diagnostic string representation of this value.
    /// </summary>
    ///
    /// <remarks>
    /// Prefer <see cref="ToStringUnsigned(int)"/> or <see cref="ToStringSigned(int)"/> when
    /// the intended interpretation must be explicit.
    /// </remarks>
    public override string ToString() => ToStringUnsigned();

    private sealed class UnsignedApIntComparer : IComparer<ApInt>
    {
        public int Compare(ApInt x, ApInt y) => x.CompareUnsigned(y);
    }

    private sealed class SignedApIntComparer : IComparer<ApInt>
    {
        public int Compare(ApInt x, ApInt y) => x.CompareSigned(y);
    }

    private static int ValidateBitWidth(int bitWidth)
    {
        if (bitWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitWidth));
        }

        return bitWidth;
    }

    private static void ValidateShiftAmount(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }
    }

    private void ValidateBitIndex(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex >= BitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(bitIndex));
        }
    }

    private void ValidateBitRange(int loBit, int hiBit)
    {
        if (loBit < 0 || hiBit < 0 || loBit > hiBit || hiBit > BitWidth)
        {
            throw new ArgumentOutOfRangeException(loBit > hiBit ? nameof(hiBit) : nameof(loBit));
        }
    }

    private void ValidateBitIndexForWrap(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > BitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(bitIndex));
        }
    }

    private void ValidateBitIndexForRangeStart(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > BitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(bitIndex));
        }
    }

    private static void EnsureCompatibleWidth(ApInt left, ApInt right)
    {
        if (left.BitWidth != right.BitWidth)
        {
            throw new ArgumentException("ApInt values must have the same bit width.", nameof(right));
        }
    }

    private void EnsureCompatibleWidth(ApInt other) => EnsureCompatibleWidth(this, other);

    private static BigInteger Normalize(int bitWidth, BigInteger value)
    {
        if (bitWidth == 0)
        {
            return BigInteger.Zero;
        }

        BigInteger mask = Mask(bitWidth);
        BigInteger normalized = value & mask;
        if (normalized.Sign < 0)
        {
            normalized += mask + BigInteger.One;
        }

        return normalized;
    }

    private static int ValidateRangeCount(int bitWidth, int count)
    {
        if (count < 0 || count > bitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return count;
    }

    private static BigInteger Mask(int bitWidth)
    {
        if (bitWidth == 0)
        {
            return BigInteger.Zero;
        }

        return (BigInteger.One << bitWidth) - BigInteger.One;
    }

    private static BigInteger SingleBitMask(int bitWidth, int bitNo)
    {
        if (bitNo < 0 || bitNo >= bitWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(bitNo));
        }

        return BigInteger.One << bitNo;
    }

    private static BigInteger RangeMask(int bitWidth, int loBit, int hiBit)
    {
        if (loBit < 0 || hiBit < 0 || loBit > hiBit || hiBit > bitWidth)
        {
            throw new ArgumentOutOfRangeException(loBit > hiBit ? nameof(hiBit) : nameof(loBit));
        }

        if (loBit == hiBit)
        {
            return BigInteger.Zero;
        }

        BigInteger lowerMask = loBit == 0 ? BigInteger.Zero : (BigInteger.One << loBit) - BigInteger.One;
        BigInteger upperMask = hiBit == bitWidth ? Mask(bitWidth) : (BigInteger.One << hiBit) - BigInteger.One;
        return upperMask & ~lowerMask;
    }

    private static BigInteger RangeMaskWithWrap(int bitWidth, int loBit, int hiBit)
    {
        if (bitWidth == 0)
        {
            if (loBit == 0 && hiBit == 0)
            {
                return BigInteger.Zero;
            }

            throw new ArgumentOutOfRangeException(nameof(loBit));
        }

        if (loBit < 0 || hiBit < 0 || loBit > bitWidth || hiBit > bitWidth)
        {
            throw new ArgumentOutOfRangeException(loBit > bitWidth ? nameof(loBit) : nameof(hiBit));
        }

        if (loBit == hiBit)
        {
            return Mask(bitWidth);
        }

        if (loBit < hiBit)
        {
            return RangeMask(bitWidth, loBit, hiBit);
        }

        return RangeMask(bitWidth, loBit, bitWidth) | RangeMask(bitWidth, 0, hiBit);
    }

    private static bool TestBitUnchecked(BigInteger value, int bitIndex)
    {
        return ((value >> bitIndex) & BigInteger.One) == BigInteger.One;
    }

    private static int GetBitLength(BigInteger value)
    {
        if (value.IsZero)
        {
            return 0;
        }

        byte[] bytes = value.ToByteArray();
        int length = bytes.Length;
        while (length > 1 && bytes[length - 1] == 0)
        {
            length--;
        }

        byte msb = bytes[length - 1];
        int bitsInMsb = 8;
        while ((msb & 0x80) == 0)
        {
            msb <<= 1;
            bitsInMsb--;
        }

        return (length - 1) * 8 + bitsInMsb;
    }

    private static int PopCountByte(byte value)
    {
        int count = 0;
        while (value != 0)
        {
            value = (byte)(value & (value - 1));
            count++;
        }

        return count;
    }

    private static int TrailingZeroCountByte(byte value)
    {
        int count = 0;
        while ((value & 1) == 0)
        {
            value >>= 1;
            count++;
        }

        return count;
    }

    private static BigInteger ParseText(string text, int radix, bool isSigned)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix));
        }

        if (text.Length == 0)
        {
            throw new FormatException("The input text is empty.");
        }

        int index = 0;
        bool negative = false;

        if (text[index] == '+' || text[index] == '-')
        {
            negative = text[index] == '-';
            index++;
        }

        if (index >= text.Length)
        {
            throw new FormatException("The input text does not contain any digits.");
        }

        if (negative && !isSigned)
        {
            throw new FormatException("Unsigned ApInt values cannot be parsed from negative text.");
        }

        BigInteger result = BigInteger.Zero;
        bool sawDigit = false;
        for (; index < text.Length; index++)
        {
            if (!TryParseDigit(text[index], out int digit))
            {
                break;
            }

            if (digit >= radix)
            {
                break;
            }

            sawDigit = true;
            result = (result * radix) + digit;
        }

        if (!sawDigit)
        {
            throw new FormatException("The input text does not contain any digits for the requested radix.");
        }

        return negative ? BigInteger.Negate(result) : result;
    }

    private static bool TryParseDigit(char c, out int digit)
    {
        if (c >= '0' && c <= '9')
        {
            digit = c - '0';
            return true;
        }

        if (c >= 'a' && c <= 'z')
        {
            digit = 10 + (c - 'a');
            return true;
        }

        if (c >= 'A' && c <= 'Z')
        {
            digit = 10 + (c - 'A');
            return true;
        }

        digit = 0;
        return false;
    }

    private static string FormatBigInteger(BigInteger value, int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix));
        }

        if (value.IsZero)
        {
            return "0";
        }

        if (radix == 10)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        bool negative = value.Sign < 0;
        BigInteger remaining = BigInteger.Abs(value);
        StringBuilder builder = new StringBuilder();

        while (remaining > BigInteger.Zero)
        {
            remaining = BigInteger.DivRem(remaining, radix, out BigInteger remainder);
            builder.Append(DigitToChar((int)remainder));
        }

        if (negative)
        {
            builder.Append('-');
        }

        char[] chars = builder.ToString().ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    private static char DigitToChar(int digit)
    {
        return digit < 10
            ? (char)('0' + digit)
            : (char)('a' + (digit - 10));
    }
}
