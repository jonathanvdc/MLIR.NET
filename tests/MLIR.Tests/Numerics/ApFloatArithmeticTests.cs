namespace MLIR.Tests.Numerics;

using System;
using MLIR.Numerics;
using Xunit;

public sealed class ApFloatArithmeticTests
{
    private static readonly FloatSemantics TinyFormat = new FloatSemantics(
        exponentBits: 3,
        precision: 3,
        hasImplicitLeadingBit: true,
        hasInfinity: false,
        hasNaN: false,
        supportsSubnormals: true);

    [Fact]
    public void ExactArithmeticMatchesExpectedResults()
    {
        ApFloat one = ApFloat.FromDouble(TinyFormat, 1.0);
        ApFloat half = ApFloat.FromDouble(TinyFormat, 0.5);
        ApFloat two = ApFloat.FromDouble(TinyFormat, 2.0);

        Assert.Equal(1.5, one.Add(half).ToDouble());
        Assert.Equal(0.5, one.Subtract(half).ToDouble());
        Assert.Equal(2.0, one.Multiply(two).ToDouble());
        Assert.Equal(2.0, two.Divide(one).ToDouble());
    }

    [Fact]
    public void ArithmeticPreservesSemanticsAndRejectsMismatches()
    {
        ApFloat left = ApFloat.FromDouble(FloatSemantics.IEEESingle, 1.0);
        ApFloat right = ApFloat.FromDouble(FloatSemantics.IEEEDouble, 1.0);

        Assert.Throws<ArgumentException>(() => left.Add(right));
        Assert.Throws<ArgumentException>(() => left.Subtract(right));
        Assert.Throws<ArgumentException>(() => left.Multiply(right));
        Assert.Throws<ArgumentException>(() => left.Divide(right));
    }

    [Fact]
    public void CopySignAbsAndNegateBehaveAsExpected()
    {
        ApFloat value = ApFloat.FromDouble(FloatSemantics.IEEESingle, -3.5);
        ApFloat positive = value.CopySign(false);
        ApFloat negative = value.Abs().Negate();

        Assert.False(positive.Sign);
        Assert.Equal(3.5, positive.ToDouble());
        Assert.True(negative.Sign);
        Assert.Equal(-3.5, negative.ToDouble());
    }

    [Fact]
    public void EqualitySeparatesBitwiseAndNumericNotions()
    {
        ApFloat positiveZero = ApFloat.Zero(FloatSemantics.IEEESingle);
        ApFloat negativeZero = ApFloat.Zero(FloatSemantics.IEEESingle, negative: true);
        ApFloat sameValue = ApFloat.FromDouble(FloatSemantics.IEEESingle, 1.5);

        Assert.NotEqual(positiveZero, negativeZero);
        Assert.True(positiveZero.NumericEquals(negativeZero));
        Assert.True(sameValue.BitwiseEquals(sameValue.ConvertTo(FloatSemantics.IEEESingle)));
        Assert.True(sameValue.Equals(sameValue));
    }
}
