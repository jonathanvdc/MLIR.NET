namespace DialectTests;

using System.Numerics;
using MLIR.Miniarith;
using MLIR.Minitest;
using Xunit;

public sealed class GeneratedDialectPrintingTests : DialectIntegrationTestBase
{
    [Fact]
    public void MiniArithAddIOpPrintsAndRoundTripsAsCustomAssembly()
    {
        var operation = ReprintAndRebindSingleOperation<MiniArith_AddIOp>(
            "%result = miniarith.addi %lhs, %rhs : i32",
            CreateMiniArithRegistry(),
            out var printed);

        Assert.DoesNotContain("\"miniarith.addi\"", printed);
        Assert.Contains("miniarith.addi", printed);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
    }

    [Fact]
    public void MiniArithConstantOpPrintsAndRoundTripsAsCustomAssembly()
    {
        var operation = ReprintAndRebindSingleOperation<MiniArith_ConstantOp>(
            "%result = miniarith.constant 42",
            CreateMiniArithRegistry(),
            out var printed);

        Assert.DoesNotContain("\"miniarith.constant\"", printed);
        Assert.Contains("miniarith.constant", printed);
        Assert.Equal((BigInteger)42, operation.Value);
        Assert.Equal("%result", operation.ResultValue.Name);
    }

    [Fact]
    public void MiniTestCastOpPrintsAndRoundTripsAsCustomAssembly()
    {
        var operation = ReprintAndRebindSingleOperation<MiniTest_CastOp>(
            "%result = minitest.cast %input : i32",
            CreateMiniTestRegistry(),
            out var printed);

        Assert.DoesNotContain("\"minitest.cast\"", printed);
        Assert.Contains("minitest.cast", printed);
        Assert.Equal("%input", operation.Input.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
    }

    [Theory]
    [InlineData("%result = minitest.binary %lhs, %rhs : i32", "%rhs")]
    [InlineData("%result = minitest.binary %lhs : i32", null)]
    public void MiniTestBinaryOpPrintsOptionalGroupWhenAppropriate(string source, string? rhsName)
    {
        var operation = ReprintAndRebindSingleOperation<MiniTest_BinaryOp>(
            source,
            CreateMiniTestRegistry(),
            out var printed);

        Assert.Contains("minitest.binary", printed);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal(rhsName, operation.Rhs?.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
    }

    [Theory]
    [InlineData("minitest.config\n    stride 4\n    padding 0\n    {}", true, true)]
    [InlineData("minitest.config\n    stride 4\n    {}", true, false)]
    public void MiniTestConfigOpPrintsOnlyPresentOilistClauses(string source, bool hasStride, bool hasPadding)
    {
        var operation = ReprintAndRebindSingleOperation<MiniTest_ConfigOp>(
            source,
            CreateMiniTestRegistry(),
            out var printed);

        Assert.Contains("minitest.config", printed);
        Assert.Equal(hasStride, printed.Contains("stride"));
        Assert.Equal(hasPadding, printed.Contains("padding"));
        Assert.Equal(hasStride, operation.Stride is not null);
        Assert.Equal(hasPadding, operation.Padding is not null);
    }
}
