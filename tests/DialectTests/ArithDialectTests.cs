namespace DialectTests;

using MLIR.Arith;
using Xunit;

public sealed class ArithDialectTests : DialectIntegrationTestBase
{
    [Fact]
    public void RegistrationExposesPreludeArithDefinitions()
    {
        var dialect = ArithDialectRegistration.Create();
        var registry = CreateArithRegistry();

        Assert.Equal("arith", dialect.Name);
        Assert.True(registry.TryGetOperation("arith.constant", out var constantDefinition));
        Assert.True(registry.TryGetOperation("arith.addi", out var addiDefinition));
        Assert.True(registry.TryGetOperation("arith.addf", out var addfDefinition));
        Assert.True(registry.TryGetOperation("arith.cmpi", out var cmpiDefinition));
        Assert.NotNull(constantDefinition?.AssemblyFormat);
        Assert.NotNull(addiDefinition?.AssemblyFormat);
        Assert.NotNull(addfDefinition?.AssemblyFormat);
        Assert.NotNull(cmpiDefinition?.AssemblyFormat);
    }

    [Fact]
    public void BindsIntegerConstantFromPreludeDialect()
    {
        var operation = BindSingleOperation<Arith_ConstantOp>(
            "%value = arith.constant 42 : i32",
            CreateArithRegistry());

        Assert.Equal("%value", operation.ResultValue.Name);
        Assert.Equal("value", operation.Value.Name);
        Assert.Equal("42 : i32", operation.Value.Value.Syntax!.GetRawText().Text);
    }

    [Fact]
    public void BindsIntegerAddExpressionFromPreludeDialect()
    {
        var operation = BindSingleOperation<Arith_AddIOp>(
            "%sum = arith.addi %lhs, %rhs : i32",
            CreateArithRegistry());

        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%sum", operation.ResultValue.Name);
    }

    [Fact]
    public void BindsFloatAddExpressionFromPreludeDialect()
    {
        var operation = BindSingleOperation<Arith_AddFOp>(
            "%sum = arith.addf %lhs, %rhs : f32",
            CreateArithRegistry());

        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%sum", operation.ResultValue.Name);
    }

    [Fact]
    public void BindsIntegerComparisonExpressionFromPreludeDialect()
    {
        var operation = BindSingleOperation<Arith_CmpIOp>(
            "%cmp = arith.cmpi slt, %lhs, %rhs : i32",
            CreateArithRegistry());

        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%cmp", operation.ResultValue.Name);
    }

    [Fact]
    public void IntegerExpressionsRoundTripAsCustomAssembly()
    {
        var operation = ReprintAndRebindSingleOperation<Arith_AddIOp>(
            "%sum = arith.addi %lhs, %rhs : i32",
            CreateArithRegistry(),
            out var printed);

        Assert.DoesNotContain("\"arith.addi\"", printed);
        Assert.Contains("arith.addi", printed);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
    }

    [Fact]
    public void CompareExpressionsRoundTripAsCustomAssembly()
    {
        var operation = ReprintAndRebindSingleOperation<Arith_CmpIOp>(
            "%cmp = arith.cmpi slt, %lhs, %rhs : i32",
            CreateArithRegistry(),
            out var printed);

        Assert.DoesNotContain("\"arith.cmpi\"", printed);
        Assert.Contains("arith.cmpi", printed);
        Assert.Contains("slt", printed);
        Assert.Contains("%lhs", printed);
        Assert.Contains("%rhs", printed);
        Assert.Contains(": i32", printed.Replace("%rhs:", "%rhs :"));
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
    }
}
