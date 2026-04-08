namespace MLIR.Tests.Numerics;

using System;
using MLIR.Numerics;
using Xunit;

public sealed class ApFloatRoundingTests
{
    private static readonly FloatSemantics TinyFormat = new FloatSemantics(
        exponentBits: 3,
        precision: 3,
        hasImplicitLeadingBit: true,
        hasInfinity: false,
        hasNaN: false,
        supportsSubnormals: true);

    [Fact]
    public void TinyFormatUsesAllExponentBitsForFiniteValues()
    {
        ApFloat maxFinite = ApFloat.FromDouble(TinyFormat, 28.0);
        ApFloat roundedFromOverflow = ApFloat.FromDouble(TinyFormat, 32.0);
        ApFloat decodedFromMaxFiniteBits = ApFloat.FromBits(TinyFormat, ApInt.Parse(TinyFormat.BitWidth, "31"));

        Assert.Equal(28.0, maxFinite.ToDouble());
        Assert.Equal(28.0, roundedFromOverflow.ToDouble());
        Assert.Equal(28.0, decodedFromMaxFiniteBits.ToDouble());
        Assert.True(decodedFromMaxFiniteBits.IsNormal);
        Assert.False(decodedFromMaxFiniteBits.IsInfinity);
        Assert.False(decodedFromMaxFiniteBits.IsNaN);
    }

    [Fact]
    public void TinyFormatSupportsSubnormalsAndZero()
    {
        ApFloat smallestSubnormal = ApFloat.FromDouble(TinyFormat, 0.0625);
        ApFloat largerSubnormal = ApFloat.FromDouble(TinyFormat, 0.1875);
        ApFloat underflowed = ApFloat.FromDouble(TinyFormat, 0.03);

        Assert.True(smallestSubnormal.IsSubnormal);
        Assert.True(largerSubnormal.IsSubnormal);
        Assert.True(underflowed.IsZero);
        Assert.Equal(0.0625, smallestSubnormal.ToDouble());
        Assert.Equal(0.1875, largerSubnormal.ToDouble());
    }

    [Fact]
    public void NearestTiesToEvenRoundsHalfWayCasesTowardEvenResults()
    {
        ApFloat roundedDown = ApFloat.FromDouble(TinyFormat, 1.125, FloatingRoundingMode.NearestTiesToEven);
        ApFloat roundedUp = ApFloat.FromDouble(TinyFormat, 1.125, FloatingRoundingMode.TowardPositive);
        ApFloat roundedNegative = ApFloat.FromDouble(TinyFormat, -1.125, FloatingRoundingMode.TowardNegative);

        Assert.Equal(1.0, roundedDown.ToDouble());
        Assert.Equal(1.25, roundedUp.ToDouble());
        Assert.Equal(-1.25, roundedNegative.ToDouble());
    }

    [Fact]
    public void ExplicitRoundingModesAffectIntegerConversions()
    {
        ApFloat positive = ApFloat.FromDouble(FloatSemantics.IEEESingle, 2.9);
        ApFloat negative = ApFloat.FromDouble(FloatSemantics.IEEESingle, -2.9);

        Assert.Equal(2, positive.ToSignedInteger(8).ToBigIntegerSigned());
        Assert.Equal(3, positive.ToSignedInteger(8, FloatingRoundingMode.TowardPositive).ToBigIntegerSigned());
        Assert.Equal(-2, negative.ToSignedInteger(8).ToBigIntegerSigned());
        Assert.Throws<OverflowException>(() => negative.ToUnsignedInteger(8));
    }
}
