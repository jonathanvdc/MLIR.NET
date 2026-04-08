namespace MLIR.Tests.Numerics;

using MLIR.Numerics;
using Xunit;

public sealed class ApFloatFmaTests
{
    private static readonly FloatSemantics TinyFormat = new FloatSemantics(
        exponentBits: 3,
        precision: 3,
        hasImplicitLeadingBit: true,
        hasInfinity: false,
        hasNaN: false,
        supportsSubnormals: true);

    [Fact]
    public void FusedMultiplyAddHandlesTheStraightforwardLLVMExample()
    {
        ApFloat f1 = ApFloat.FromSingle(FloatSemantics.IEEESingle, 14.5f);
        ApFloat f2 = ApFloat.FromSingle(FloatSemantics.IEEESingle, -14.5f);
        ApFloat f3 = ApFloat.FromSingle(FloatSemantics.IEEESingle, 225.0f);

        ApFloat result = f1.FusedMultiplyAdd(f2, f3);

        Assert.Equal(14.75f, result.ToSingle());
    }

    [Fact]
    public void FusedMultiplyAddPreservesTheSignOfExactZeroWhenRoundingTowardNegative()
    {
        ApFloat f1 = ApFloat.FromDouble(FloatSemantics.IEEESingle, 1.0);
        ApFloat f2 = ApFloat.FromDouble(FloatSemantics.IEEESingle, -1.0);
        ApFloat f3 = ApFloat.FromDouble(FloatSemantics.IEEESingle, 1.0);

        ApFloat nearest = f1.FusedMultiplyAdd(f2, f3, FloatingRoundingMode.NearestTiesToEven);
        ApFloat towardNegative = f1.FusedMultiplyAdd(f2, f3, FloatingRoundingMode.TowardNegative);

        Assert.True(nearest.IsZero);
        Assert.False(nearest.Sign);
        Assert.True(towardNegative.IsNegativeZero);
    }

    [Fact]
    public void FusedMultiplyAddKeepsNegativeZeroForSignedZeroInputs()
    {
        ApFloat f1 = ApFloat.Zero(FloatSemantics.IEEESingle);
        ApFloat f2 = ApFloat.Zero(FloatSemantics.IEEESingle, negative: true);
        ApFloat f3 = ApFloat.Zero(FloatSemantics.IEEESingle, negative: true);

        ApFloat result = f1.FusedMultiplyAdd(f2, f3);

        Assert.True(result.IsNegativeZero);
    }

    [Fact]
    public void FusedMultiplyAddRoundsHalfwayCasesLikeLLVMsDirectedAndNearestModes()
    {
        ApFloat one = ApFloat.FromDouble(TinyFormat, 1.0);
        ApFloat minusOne = ApFloat.FromDouble(TinyFormat, -1.0);
        ApFloat halfUlp = ApFloat.FromDouble(TinyFormat, 0.125);

        ApFloat positiveNearest = one.FusedMultiplyAdd(one, halfUlp, FloatingRoundingMode.NearestTiesToEven);
        ApFloat positiveTowardPositive = one.FusedMultiplyAdd(one, halfUlp, FloatingRoundingMode.TowardPositive);
        ApFloat positiveTowardNegative = one.FusedMultiplyAdd(one, halfUlp, FloatingRoundingMode.TowardNegative);

        ApFloat negativeNearest = minusOne.FusedMultiplyAdd(one, halfUlp.Negate(), FloatingRoundingMode.NearestTiesToEven);
        ApFloat negativeTowardPositive = minusOne.FusedMultiplyAdd(one, halfUlp.Negate(), FloatingRoundingMode.TowardPositive);
        ApFloat negativeTowardNegative = minusOne.FusedMultiplyAdd(one, halfUlp.Negate(), FloatingRoundingMode.TowardNegative);

        Assert.Equal(ApFloat.FromDouble(TinyFormat, 1.0), positiveNearest);
        Assert.Equal(ApFloat.FromDouble(TinyFormat, 1.25), positiveTowardPositive);
        Assert.Equal(ApFloat.FromDouble(TinyFormat, 1.0), positiveTowardNegative);
        Assert.Equal(ApFloat.FromDouble(TinyFormat, -1.0), negativeNearest);
        Assert.Equal(ApFloat.FromDouble(TinyFormat, -1.0), negativeTowardPositive);
        Assert.Equal(ApFloat.FromDouble(TinyFormat, -1.25), negativeTowardNegative);
    }
}
