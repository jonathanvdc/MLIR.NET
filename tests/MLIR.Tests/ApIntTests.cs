namespace MLIR.Tests;

using System;
using System.Numerics;
using MLIR.Numerics;
using Xunit;

public sealed class ApIntTests
{
    [Fact]
    public void ZeroWidthValuesRemainWellDefined()
    {
        ApInt zero = ApInt.Zero(0);
        ApInt parsed = ApInt.Parse(0, "0");

        Assert.Equal(0, zero.BitWidth);
        Assert.True(zero.IsZero);
        Assert.True(zero.IsAllOnes);
        Assert.False(zero.SignBit);
        Assert.Equal("0", zero.ToStringUnsigned());
        Assert.Equal(zero, parsed);
    }

    [Fact]
    public void WrapsArithmeticModuloBitWidth()
    {
        ApInt left = ApInt.FromUInt64(8, 250);
        ApInt right = ApInt.FromUInt64(8, 10);

        ApInt sum = left + right;

        Assert.Equal(8, sum.BitWidth);
        Assert.Equal((BigInteger)4, sum.ToBigIntegerUnsigned());
        Assert.Equal((BigInteger)4, sum.ToBigIntegerSigned());
    }

    [Fact]
    public void BasicArithmeticAndBitwiseIdentitiesHold()
    {
        ApInt value = ApInt.Parse(8, "5a", radix: 16, isSigned: false);
        ApInt zero = ApInt.Zero(8);
        ApInt one = ApInt.One(8);
        ApInt allOnes = ApInt.AllOnes(8);

        Assert.Equal(value, value + zero);
        Assert.Equal(value, value - zero);
        Assert.Equal(value, value * one);
        Assert.Equal(value, value & allOnes);
        Assert.Equal(value, value | zero);
        Assert.Equal(zero, value ^ value);
        Assert.Equal(zero, ~allOnes);
        Assert.Equal(allOnes, ~zero);
    }

    [Fact]
    public void PreservesWidthInEquality()
    {
        ApInt eightBit = ApInt.Zero(8);
        ApInt sixteenBit = ApInt.Zero(16);

        Assert.NotEqual(eightBit, sixteenBit);
        Assert.True(eightBit != sixteenBit);
    }

    [Fact]
    public void WidthMismatchInComparisonThrows()
    {
        ApInt left = ApInt.Zero(8);
        ApInt right = ApInt.Zero(16);

        Assert.Throws<ArgumentException>(() => left.ULessThan(right));
        Assert.Throws<ArgumentException>(() => left.SGreaterThanOrEqual(right));
    }

    [Fact]
    public void SignedAndUnsignedComparisonsDistinguishTheSameBits()
    {
        ApInt minusOne = ApInt.Parse(8, "ff", radix: 16, isSigned: false);
        ApInt zero = ApInt.Zero(8);

        Assert.True(minusOne.UGreaterThan(zero));
        Assert.True(minusOne.SLessThan(zero));
        Assert.True(zero.ULessThan(minusOne));
    }

    [Fact]
    public void SignExtensionReplicatesTheSignBit()
    {
        ApInt value = ApInt.Parse(8, "ff", radix: 16, isSigned: false);

        ApInt extended = value.SignExtend(16);

        Assert.Equal(16, extended.BitWidth);
        Assert.True(extended.SignBit);
        Assert.Equal((BigInteger)(short)-1, extended.ToBigIntegerSigned());
        Assert.Equal("ffff", extended.ToStringUnsigned(16));
    }

    [Fact]
    public void SignedAndUnsignedViewsDifferAsExpected()
    {
        ApInt value = ApInt.Parse(8, "ff", radix: 16, isSigned: false);

        Assert.Equal((BigInteger)255, value.ToBigIntegerUnsigned());
        Assert.Equal((BigInteger)(-1), value.ToBigIntegerSigned());
        Assert.Equal("-1", value.ToStringSigned());
        Assert.Equal("255", value.ToStringUnsigned());
    }

    [Fact]
    public void SignedParseUsesTwoComplementBitPattern()
    {
        ApInt negativeOne = ApInt.Parse(8, "-1", isSigned: true);

        Assert.Equal((BigInteger)255, negativeOne.ToBigIntegerUnsigned());
        Assert.Equal((BigInteger)(-1), negativeOne.ToBigIntegerSigned());
        Assert.True(negativeOne.IsAllOnes);
    }

    [Fact]
    public void ParseRejectsNegativeUnsignedValues()
    {
        Assert.Throws<FormatException>(() => ApInt.Parse(8, "-1", isSigned: false));
    }

    [Fact]
    public void ParseRejectsInvalidRadixAndDigits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ApInt.Parse(8, "10", radix: 1));
        Assert.Throws<FormatException>(() => ApInt.Parse(8, "z", radix: 10));
    }

    [Fact]
    public void TruncationAndExtensionRespectIdentityCases()
    {
        ApInt value = ApInt.Parse(8, "5a", radix: 16, isSigned: false);

        Assert.Equal(value, value.Trunc(8));
        Assert.Equal(value, value.ZeroExtend(8));
        Assert.Equal(value, value.SignExtend(8));
    }

    [Fact]
    public void CountsBitsAndShiftsAsExpected()
    {
        ApInt value = ApInt.Parse(8, "81", radix: 16, isSigned: false);

        Assert.Equal(2, value.PopCount());
        Assert.Equal(0, value.CountLeadingZeros());
        Assert.Equal(0, value.CountTrailingZeros());
        Assert.True(value.TestBit(0));
        Assert.True(value.TestBit(7));
        Assert.Equal("2", value.LShr(6).ToStringUnsigned());
        Assert.Equal("0", value.Shl(8).ToStringUnsigned());
        Assert.Equal("ff", (~ApInt.Zero(8)).ToStringUnsigned(16));
    }

    [Fact]
    public void ShiftEdgeCasesFollowExpectedWidthSemantics()
    {
        ApInt negative = ApInt.Parse(8, "80", radix: 16, isSigned: false);
        ApInt positive = ApInt.Parse(8, "01", radix: 16, isSigned: false);

        Assert.Equal(ApInt.Zero(8), positive.LShr(8));
        Assert.Equal(ApInt.Zero(8), positive.Shl(8));
        Assert.Equal(ApInt.AllOnes(8), negative.AShr(8));
        Assert.Equal(ApInt.Zero(8), positive.AShr(8));
    }

    [Fact]
    public void SignedDivisionAndRemainderUseTwoComplementInterpretation()
    {
        ApInt dividend = ApInt.Parse(8, "fe", radix: 16, isSigned: false);
        ApInt divisor = ApInt.FromInt64(8, 2);

        ApInt quotient = dividend.SDiv(divisor);
        ApInt remainder = dividend.SRem(divisor);

        Assert.Equal((BigInteger)(-1), quotient.ToBigIntegerSigned());
        Assert.Equal((BigInteger)0, remainder.ToBigIntegerSigned());
        Assert.Equal("ff", quotient.ToStringUnsigned(16));
    }

    [Fact]
    public void ConversionMethodsRoundTripForSmallValues()
    {
        ApInt value = ApInt.FromUInt64(16, 0x7fffu);

        Assert.Equal((ulong)0x7fffu, value.ToUInt64());
        Assert.Equal((long)0x7fffu, value.ToInt64());
    }

    [Fact]
    public void ConversionMethodsThrowWhenTheValueDoesNotFit()
    {
        ApInt value = ApInt.Parse(65, "18446744073709551616", isSigned: false);

        Assert.Throws<OverflowException>(() => value.ToUInt64());
    }

    [Fact]
    public void UnsignedDivisionAndRemainderMatchExpectedResults()
    {
        ApInt dividend = ApInt.Parse(8, "fe", radix: 16, isSigned: false);
        ApInt divisor = ApInt.FromUInt64(8, 2);

        ApInt quotient = dividend.UDiv(divisor);
        ApInt remainder = dividend.URem(divisor);

        Assert.Equal((BigInteger)127, quotient.ToBigIntegerUnsigned());
        Assert.Equal((BigInteger)0, remainder.ToBigIntegerUnsigned());
    }

    [Fact]
    public void BitManipulationMethodsRespectWidth()
    {
        ApInt value = ApInt.Zero(8);

        ApInt set = value.SetBit(3);
        ApInt cleared = set.ClearBit(3);

        Assert.True(set.TestBit(3));
        Assert.Equal((BigInteger)8, set.ToBigIntegerUnsigned());
        Assert.True(cleared.IsZero);
        Assert.Throws<ArgumentOutOfRangeException>(() => value.TestBit(8));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.SetBit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.ClearBit(8));
    }

    [Fact]
    public void StringFormattingSupportsMultipleRadices()
    {
        ApInt value = ApInt.Parse(12, "fff", radix: 16, isSigned: false);

        Assert.Equal("4095", value.ToStringUnsigned());
        Assert.Equal("fff", value.ToStringUnsigned(16));
        Assert.Equal("111111111111", value.ToStringUnsigned(2));
    }

    [Fact]
    public void CountLeadingAndTrailingZerosHandleExtremes()
    {
        ApInt zero = ApInt.Zero(12);
        ApInt highBit = ApInt.Parse(12, "800", radix: 16, isSigned: false);
        ApInt lowBit = ApInt.Parse(12, "001", radix: 16, isSigned: false);

        Assert.Equal(12, zero.CountLeadingZeros());
        Assert.Equal(12, zero.CountTrailingZeros());
        Assert.Equal(0, highBit.CountLeadingZeros());
        Assert.Equal(11, highBit.CountTrailingZeros());
        Assert.Equal(11, lowBit.CountLeadingZeros());
        Assert.Equal(0, lowBit.CountTrailingZeros());
    }
}
