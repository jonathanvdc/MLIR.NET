namespace MLIR.Tests.Numerics;

using MLIR.Numerics;
using Xunit;

public sealed class FloatSemanticsTests
{
    [Fact]
    public void PredefinedSemanticsMatchTheirDocumentedShapes()
    {
        Assert.Equal(5, FloatSemantics.IEEEHalf.ExponentBits);
        Assert.Equal(11, FloatSemantics.IEEEHalf.Precision);
        Assert.Equal(16, FloatSemantics.IEEEHalf.BitWidth);

        Assert.Equal(8, FloatSemantics.BFloat16.ExponentBits);
        Assert.Equal(8, FloatSemantics.BFloat16.Precision);
        Assert.Equal(16, FloatSemantics.BFloat16.BitWidth);

        Assert.Equal(8, FloatSemantics.IEEESingle.ExponentBits);
        Assert.Equal(24, FloatSemantics.IEEESingle.Precision);
        Assert.Equal(32, FloatSemantics.IEEESingle.BitWidth);

        Assert.Equal(11, FloatSemantics.IEEEDouble.ExponentBits);
        Assert.Equal(53, FloatSemantics.IEEEDouble.Precision);
        Assert.Equal(64, FloatSemantics.IEEEDouble.BitWidth);
    }

    [Fact]
    public void EqualityAndToStringAreStable()
    {
        FloatSemantics singleLike = new FloatSemantics(8, 24, true, true, true, true);
        FloatSemantics custom = new FloatSemantics(3, 3, true, false, false, true);

        Assert.Equal(FloatSemantics.IEEESingle, singleLike);
        Assert.Equal("binary32", FloatSemantics.IEEESingle.ToString());
        Assert.Equal("FloatSemantics(expBits=3, precision=3, implicitLeadingBit=True, infinity=False, nan=False, subnormals=True)", custom.ToString());
        Assert.NotEqual(FloatSemantics.IEEESingle, custom);
    }
}
