namespace MLIR.Tests.Numerics;

using MLIR.Numerics;
using Xunit;

public sealed class ApFloatNavigationTests
{
    private static readonly FloatSemantics TinyFormat = new FloatSemantics(
        exponentBits: 3,
        precision: 3,
        hasImplicitLeadingBit: true,
        hasInfinity: false,
        hasNaN: false,
        supportsSubnormals: true);

    private static readonly FloatSemantics SmallerTinyFormat = new FloatSemantics(
        exponentBits: 2,
        precision: 4,
        hasImplicitLeadingBit: true,
        hasInfinity: false,
        hasNaN: false,
        supportsSubnormals: true);

    [Fact]
    public void NextAroundZeroMatchesLLVMStyleBoundaries()
    {
        ApFloat positiveZero = ApFloat.Zero(FloatSemantics.IEEESingle);
        ApFloat negativeZero = ApFloat.Zero(FloatSemantics.IEEESingle, negative: true);

        Assert.Equal(ApFloat.FromSingle(FloatSemantics.IEEESingle, float.Epsilon), positiveZero.NextUp());
        Assert.Equal(ApFloat.FromSingle(FloatSemantics.IEEESingle, -float.Epsilon), positiveZero.NextDown());
        Assert.Equal(ApFloat.FromSingle(FloatSemantics.IEEESingle, float.Epsilon), negativeZero.NextUp());
        Assert.Equal(ApFloat.FromSingle(FloatSemantics.IEEESingle, -float.Epsilon), negativeZero.NextDown());
    }

    [Fact]
    public void NextAtInfinityAndLargestFiniteValuesMatchesLLVMStyleBehavior()
    {
        ApFloat positiveInfinity = ApFloat.Infinity(FloatSemantics.IEEESingle);
        ApFloat negativeInfinity = ApFloat.Infinity(FloatSemantics.IEEESingle, negative: true);
        ApFloat largestPositive = ApFloat.FromBits(
            FloatSemantics.IEEESingle,
            ApInt.Parse(FloatSemantics.IEEESingle.BitWidth, "2139095039"));
        ApFloat largestNegative = ApFloat.FromBits(
            FloatSemantics.IEEESingle,
            ApInt.Parse(FloatSemantics.IEEESingle.BitWidth, "4286578687"));

        Assert.Equal(positiveInfinity, positiveInfinity.NextUp());
        Assert.Equal(largestPositive, positiveInfinity.NextDown());
        Assert.Equal(largestNegative, negativeInfinity.NextUp());
        Assert.Equal(negativeInfinity, negativeInfinity.NextDown());
        Assert.Equal(positiveInfinity, largestPositive.NextUp());
        Assert.Equal(negativeInfinity, largestNegative.NextDown());
    }

    [Fact]
    public void NextStepsAcrossTheTinyFiniteFormat()
    {
        ApFloat smallestPositive = ApFloat.FromDouble(TinyFormat, 0.0625);
        ApFloat smallestNegative = ApFloat.FromDouble(TinyFormat, -0.0625);
        ApFloat positiveZero = ApFloat.Zero(TinyFormat);
        ApFloat negativeZero = ApFloat.Zero(TinyFormat, negative: true);
        ApFloat maxPositive = ApFloat.FromDouble(TinyFormat, 28.0);

        Assert.True(smallestNegative.NextUp().IsNegativeZero);
        Assert.Equal(smallestPositive, negativeZero.NextUp());
        Assert.Equal(smallestPositive, positiveZero.NextUp());
        Assert.Equal(smallestNegative, positiveZero.NextDown());
        Assert.Equal(positiveZero, smallestPositive.NextDown());
        Assert.Equal(maxPositive, maxPositive.NextUp());
        Assert.Equal(maxPositive, maxPositive.NextDown().NextUp());
    }

    [Fact]
    public void NextCrossesTheDenormalNormalBoundaryInTheLLVMStyleTinyFormat()
    {
        ApFloat largestPositiveDenormal = ApFloat.FromBits(TinyFormat, ApInt.Parse(TinyFormat.BitWidth, "3"));
        ApFloat smallestPositiveNormal = ApFloat.FromBits(TinyFormat, ApInt.Parse(TinyFormat.BitWidth, "4"));
        ApFloat largestNegativeDenormal = ApFloat.FromBits(TinyFormat, ApInt.Parse(TinyFormat.BitWidth, "35"));
        ApFloat smallestNegativeNormal = ApFloat.FromBits(TinyFormat, ApInt.Parse(TinyFormat.BitWidth, "36"));

        Assert.Equal(smallestPositiveNormal, largestPositiveDenormal.NextUp());
        Assert.Equal(largestPositiveDenormal, smallestPositiveNormal.NextDown());
        Assert.Equal(smallestNegativeNormal, largestNegativeDenormal.NextDown());
        Assert.Equal(largestNegativeDenormal, smallestNegativeNormal.NextUp());
    }

    [Fact]
    public void NextCrossesTheBoundaryInTheSmallerLLVMStyleTinyFormat()
    {
        ApFloat largestPositiveDenormal = ApFloat.FromBits(SmallerTinyFormat, ApInt.Parse(SmallerTinyFormat.BitWidth, "7"));
        ApFloat smallestPositiveNormal = ApFloat.FromBits(SmallerTinyFormat, ApInt.Parse(SmallerTinyFormat.BitWidth, "8"));

        Assert.Equal(smallestPositiveNormal, largestPositiveDenormal.NextUp());
        Assert.Equal(largestPositiveDenormal, smallestPositiveNormal.NextDown());
    }
}
