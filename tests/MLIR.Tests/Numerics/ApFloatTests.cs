namespace MLIR.Tests.Numerics;

using System;
using System.Numerics;
using MLIR.Numerics;
using Xunit;

public sealed class ApFloatTests
{
    [Fact]
    public void PredefinedSemanticsExposeExpectedBitWidths()
    {
        Assert.Equal(16, FloatSemantics.IEEEHalf.BitWidth);
        Assert.Equal(16, FloatSemantics.BFloat16.BitWidth);
        Assert.Equal(32, FloatSemantics.IEEESingle.BitWidth);
        Assert.Equal(64, FloatSemantics.IEEEDouble.BitWidth);
    }

    [Fact]
    public void ZeroInfinityAndNaNAreConstructible()
    {
        ApFloat positiveZero = ApFloat.Zero(FloatSemantics.IEEESingle);
        ApFloat negativeInfinity = ApFloat.Infinity(FloatSemantics.IEEESingle, negative: true);
        ApFloat quietNaN = ApFloat.QuietNaN(FloatSemantics.IEEESingle);

        Assert.True(positiveZero.IsZero);
        Assert.False(positiveZero.Sign);
        Assert.True(negativeInfinity.IsInfinity);
        Assert.True(negativeInfinity.Sign);
        Assert.True(quietNaN.IsNaN);
        Assert.Equal("0", positiveZero.ToString());
        Assert.Equal("-Infinity", negativeInfinity.ToString());
        Assert.Equal("NaN", quietNaN.ToString());
    }

    [Fact]
    public void ParseAndConvertRoundTripCommonValues()
    {
        ApFloat value = ApFloat.Parse(FloatSemantics.IEEESingle, "1.5");

        Assert.True(value.IsNormal);
        Assert.Equal(1.5d, value.ToDouble());
        Assert.Equal(1.5f, value.ToSingle());
        Assert.Equal(value, value.ConvertTo(FloatSemantics.IEEESingle));
    }

    [Fact]
    public void NegativeZeroKeepsItsSignAndComparesNumericallyEqual()
    {
        ApFloat positiveZero = ApFloat.Zero(FloatSemantics.IEEESingle);
        ApFloat negativeZero = ApFloat.Zero(FloatSemantics.IEEESingle, negative: true);

        Assert.NotEqual(positiveZero, negativeZero);
        Assert.True(positiveZero.NumericEquals(negativeZero));
        Assert.True(negativeZero.IsNegativeZero);
    }

    [Fact]
    public void ArithmeticUsesTheRequestedSemantics()
    {
        ApFloat left = ApFloat.FromDouble(FloatSemantics.IEEESingle, 1.25);
        ApFloat right = ApFloat.FromDouble(FloatSemantics.IEEESingle, 0.75);

        ApFloat sum = left.Add(right);
        ApFloat product = left.Multiply(right);

        Assert.Equal(2.0d, sum.ToDouble());
        Assert.Equal(0.9375d, product.ToDouble());
    }

    [Fact]
    public void BitPatternsRoundTripThroughFromBits()
    {
        ApFloat original = ApFloat.FromDouble(FloatSemantics.IEEEDouble, -42.5);
        ApFloat decoded = ApFloat.FromBits(FloatSemantics.IEEEDouble, original.ToBits());

        Assert.Equal(original, decoded);
        Assert.Equal(-42.5d, decoded.ToDouble());
    }

    [Fact]
    public void IntegerConversionsRespectRoundingDirection()
    {
        ApFloat value = ApFloat.FromDouble(FloatSemantics.IEEESingle, 2.9);

        Assert.Equal((BigInteger)2, value.ToUnsignedInteger(8).ToBigIntegerUnsigned());
        Assert.Equal((BigInteger)3, value.ToUnsignedInteger(8, FloatingRoundingMode.TowardPositive).ToBigIntegerUnsigned());
    }
}
