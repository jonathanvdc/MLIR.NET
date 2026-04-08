namespace MLIR.Tests.Numerics;

using System;
using MLIR.Numerics;
using Xunit;

public sealed class ApFloatSpecialValueTests
{
    [Fact]
    public void ZeroInfinityAndNaNCarryTheirClassificationAndSigns()
    {
        ApFloat positiveZero = ApFloat.Zero(FloatSemantics.IEEESingle);
        ApFloat negativeZero = ApFloat.Zero(FloatSemantics.IEEESingle, negative: true);
        ApFloat positiveInfinity = ApFloat.Infinity(FloatSemantics.IEEESingle);
        ApFloat negativeInfinity = ApFloat.Infinity(FloatSemantics.IEEESingle, negative: true);
        ApFloat quietNaN = ApFloat.QuietNaN(FloatSemantics.IEEESingle);

        Assert.True(positiveZero.IsZero);
        Assert.False(positiveZero.Sign);
        Assert.Equal(FloatCategory.Zero, positiveZero.Category);
        Assert.True(negativeZero.IsNegativeZero);
        Assert.True(negativeInfinity.IsInfinity);
        Assert.True(negativeInfinity.Sign);
        Assert.True(quietNaN.IsNaN);
        Assert.Equal("0", positiveZero.ToString());
        Assert.Equal("-0", negativeZero.ToString());
        Assert.Equal("Infinity", positiveInfinity.ToString());
        Assert.Equal("-Infinity", negativeInfinity.ToString());
        Assert.Equal("NaN", quietNaN.ToString());
    }

    [Fact]
    public void SpecialValuesRoundTripThroughBits()
    {
        ApFloat positiveZero = ApFloat.Zero(FloatSemantics.IEEESingle);
        ApFloat negativeZero = ApFloat.Zero(FloatSemantics.IEEESingle, negative: true);
        ApFloat positiveInfinity = ApFloat.Infinity(FloatSemantics.IEEESingle);
        ApFloat quietNaN = ApFloat.QuietNaN(FloatSemantics.IEEESingle);

        Assert.Equal(positiveZero, ApFloat.FromBits(FloatSemantics.IEEESingle, positiveZero.ToBits()));
        Assert.Equal(negativeZero, ApFloat.FromBits(FloatSemantics.IEEESingle, negativeZero.ToBits()));
        Assert.Equal(positiveInfinity, ApFloat.FromBits(FloatSemantics.IEEESingle, positiveInfinity.ToBits()));
        Assert.True(ApFloat.FromBits(FloatSemantics.IEEESingle, quietNaN.ToBits()).IsNaN);
    }

    [Fact]
    public void SpecialValueFactoriesRejectUnsupportedFormats()
    {
        FloatSemantics noSpecials = new FloatSemantics(3, 3, true, false, false, true);

        Assert.Throws<NotSupportedException>(() => ApFloat.Infinity(noSpecials));
        Assert.Throws<NotSupportedException>(() => ApFloat.QuietNaN(noSpecials));
    }

    [Fact]
    public void ParseRecognizesCommonSpecialTokens()
    {
        ApFloat infinity = ApFloat.Parse(FloatSemantics.IEEESingle, "inf");
        ApFloat negativeInfinity = ApFloat.Parse(FloatSemantics.IEEESingle, "-Infinity");
        ApFloat quietNaN = ApFloat.Parse(FloatSemantics.IEEESingle, "nan");
        ApFloat negativeZero = ApFloat.Parse(FloatSemantics.IEEESingle, "-0");

        Assert.True(infinity.IsInfinity);
        Assert.True(negativeInfinity.Sign);
        Assert.True(quietNaN.IsNaN);
        Assert.True(negativeZero.IsNegativeZero);
    }
}
