namespace MLIR.Tests.Numerics;

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
    public void OneBitValuesBehaveLikeBooleansWithSign()
    {
        ApInt zero = ApInt.Zero(1);
        ApInt one = ApInt.One(1);

        Assert.False(zero.SignBit);
        Assert.True(one.SignBit);
        Assert.True(one.IsAllOnes);
        Assert.Equal((BigInteger)(-1), one.ToBigIntegerSigned());
        Assert.Equal("1", one.ToStringUnsigned());
        Assert.Equal("-1", one.ToStringSigned());
        Assert.Equal(zero, one & zero);
        Assert.Equal(one, one | zero);
    }

    [Fact]
    public void RangeConstructorsBuildTheExpectedBitPatterns()
    {
        Assert.Equal("8", ApInt.GetOneBitSet(8, 3).ToStringUnsigned(16));
        Assert.Equal("1c", ApInt.GetBitsSet(8, 2, 5).ToStringUnsigned(16));
        Assert.Equal("c3", ApInt.GetBitsSetWithWrap(8, 6, 2).ToStringUnsigned(16));
        Assert.Equal("f0", ApInt.GetBitsSetFrom(8, 4).ToStringUnsigned(16));
        Assert.Equal("e0", ApInt.GetHighBitsSet(8, 3).ToStringUnsigned(16));
        Assert.Equal("7", ApInt.GetLowBitsSet(8, 3).ToStringUnsigned(16));
        Assert.Equal("0", ApInt.GetZeroWidth().ToStringUnsigned());
        Assert.Equal("ff", ApInt.GetMaxValue(8).ToStringUnsigned(16));
        Assert.Equal("7f", ApInt.GetSignedMaxValue(8).ToStringUnsigned(16));
        Assert.Equal("80", ApInt.GetSignedMinValue(8).ToStringUnsigned(16));
        Assert.Equal("80", ApInt.GetSignMask(8).ToStringUnsigned(16));
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
    public void RangeMutationHelpersSetAndClearBitRanges()
    {
        ApInt empty = ApInt.Zero(8);
        ApInt full = ApInt.AllOnes(8);

        Assert.Equal("1c", empty.SetBits(2, 5).ToStringUnsigned(16));
        Assert.Equal("c3", empty.SetBitsWithWrap(6, 2).ToStringUnsigned(16));
        Assert.Equal("f0", empty.SetBitsFrom(4).ToStringUnsigned(16));
        Assert.Equal("e0", empty.SetHighBits(3).ToStringUnsigned(16));
        Assert.Equal("7", empty.SetLowBits(3).ToStringUnsigned(16));
        Assert.Equal("e3", full.ClearBits(2, 5).ToStringUnsigned(16));
        Assert.Equal("3c", full.ClearBitsWithWrap(6, 2).ToStringUnsigned(16));
        Assert.Equal("f", full.ClearBitsFrom(4).ToStringUnsigned(16));
        Assert.Equal("1f", full.ClearHighBits(3).ToStringUnsigned(16));
        Assert.Equal("f8", full.ClearLowBits(3).ToStringUnsigned(16));
    }

    [Fact]
    public void AdditionCarriesAndSubtractionBorrowsWithinWidth()
    {
        ApInt max = ApInt.AllOnes(8);
        ApInt one = ApInt.One(8);

        Assert.Equal(ApInt.Zero(8), max + one);
        Assert.Equal(ApInt.AllOnes(8), ApInt.Zero(8) - one);
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
    public void ValuePredicatesMatchSignedAndUnsignedExtremes()
    {
        ApInt zero = ApInt.Zero(8);
        ApInt oneBit = ApInt.GetOneBitSet(8, 6);
        ApInt negativeOne = ApInt.AllOnes(8);
        ApInt minSigned = ApInt.GetSignedMinValue(8);
        ApInt maxSigned = ApInt.GetSignedMaxValue(8);

        Assert.True(zero.IsZero);
        Assert.True(zero.IsMinValue);
        Assert.True(zero.IsNonNegative);
        Assert.False(zero.IsNegative);
        Assert.False(zero.IsPowerOf2());
        Assert.False(zero.IsNegatedPowerOf2());
        Assert.True(oneBit.IsPowerOf2());
        Assert.True(oneBit.IsOneBitSet(6));
        Assert.False(oneBit.IsMaxSignedValue);
        Assert.True(negativeOne.IsAllOnes);
        Assert.True(negativeOne.IsMaxValue);
        Assert.True(negativeOne.IsNegative);
        Assert.True(negativeOne.IsNonPositive);
        Assert.True(maxSigned.IsMaxSignedValue);
        Assert.True(minSigned.IsMinSignedValue);
        Assert.True(minSigned.IsNegatedPowerOf2());
        Assert.True(maxSigned.IsStrictlyPositive);
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
    public void ComparisonHelpersAreReflexiveAndOrdered()
    {
        ApInt left = ApInt.Parse(8, "20", radix: 16, isSigned: false);
        ApInt right = ApInt.Parse(8, "40", radix: 16, isSigned: false);

        Assert.True(left.ULessThanOrEqual(left));
        Assert.True(left.UGreaterThanOrEqual(left));
        Assert.True(left.ULessThan(right));
        Assert.True(right.UGreaterThan(left));
        Assert.True(left.SLessThanOrEqual(left));
        Assert.True(left.SGreaterThanOrEqual(left));
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
    public void ParseStopsAtTheFirstInvalidDigitLikeUpstreamApInt()
    {
        ApInt parsedDecimal = ApInt.Parse(16, "123xyz", radix: 10);
        ApInt parsedHex = ApInt.Parse(16, "ABCDg", radix: 16);

        Assert.Equal((BigInteger)123, parsedDecimal.ToBigIntegerUnsigned());
        Assert.Equal("abcd", parsedHex.ToStringUnsigned(16));
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
    public void TruncationDropsHighBitsAndExtensionsRestoreShape()
    {
        ApInt value = ApInt.Parse(16, "12ab", radix: 16, isSigned: false);

        ApInt truncated = value.Trunc(8);

        Assert.Equal("ab", truncated.ToStringUnsigned(16));
        Assert.Equal(truncated, truncated.ZeroExtend(16).Trunc(8));
        Assert.Equal(truncated, truncated.SignExtend(16).Trunc(8));
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
    public void DivisionByOneLeavesTheValueUnchanged()
    {
        ApInt value = ApInt.Parse(8, "8b", radix: 16, isSigned: false);
        ApInt one = ApInt.One(8);

        Assert.Equal(value, value.UDiv(one));
        Assert.Equal(ApInt.Zero(8), value.URem(one));
        Assert.Equal(value, value.SDiv(one));
        Assert.Equal(ApInt.Zero(8), value.SRem(one));
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
    public void DivisionAndRemainderByLargerDivisorAreStable()
    {
        ApInt dividend = ApInt.Parse(8, "05", radix: 16, isSigned: false);
        ApInt divisor = ApInt.Parse(8, "10", radix: 16, isSigned: false);

        Assert.Equal(ApInt.Zero(8), dividend.UDiv(divisor));
        Assert.Equal(dividend, dividend.URem(divisor));
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

    [Fact]
    public void ShiftAndBitManipulationRoundTripSingleBits()
    {
        ApInt value = ApInt.Zero(8).SetBit(5);

        Assert.Equal(ApInt.One(3), value.LShr(5).Trunc(3));
        Assert.Equal(value, value.LShr(0));
        Assert.Equal(value, value.Shl(0));
        Assert.Equal(value, value.AShr(0));
        Assert.True(value.TestBit(5));
    }
}
