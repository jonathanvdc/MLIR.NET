namespace DialectTests;

using MLIR;
using MLIR.Minitest;
using MLIR.Numerics;
using MLIR.Semantics;
using Xunit;

public sealed class MiniTestDialectTests : DialectIntegrationTestBase
{
    [Fact]
    public void RegistrationExposesAllOperations()
    {
        var dialect = MinitestDialectRegistration.Create();
        var registry = CreateMiniTestRegistry();

        Assert.Equal("minitest", dialect.Name);
        Assert.True(registry.TryGetOperation("minitest.cast", out var castDef));
        Assert.NotNull(castDef!.AssemblyFormat);
        Assert.True(registry.TryGetOperation("minitest.binary", out var binaryDef));
        Assert.NotNull(binaryDef!.AssemblyFormat);
        Assert.True(registry.TryGetOperation("minitest.config", out var configDef));
        Assert.NotNull(configDef!.AssemblyFormat);
    }

    [Fact]
    public void CastOpParsesQualifiedTypeFormat()
    {
        var document = Document.Parse("%result = minitest.cast %input : i32", CreateMiniTestRegistry());

        var operation = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_CastOpBodySyntax>(operation.Body);
        Assert.Equal("%input", body.Input.Text);
        Assert.Equal("i32", body.ResultType.ToString());
    }

    [Fact]
    public void CastOpBindsToTypedOperation()
    {
        var operation = BindSingleOperation<MiniTest_CastOp>(
            "%result = minitest.cast %input : i32",
            CreateMiniTestRegistry());

        Assert.Equal("minitest.cast", operation.Name);
        Assert.Equal("%input", operation.Input.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
    }

    [Theory]
    [InlineData("%result = minitest.binary %lhs, %rhs : i32", true, "%rhs")]
    [InlineData("%result = minitest.binary %lhs : i32", false, null)]
    public void BinaryOpParsesOptionalOperandGroup(string source, bool hasRhs, string? rhsName)
    {
        var document = Document.Parse(source, CreateMiniTestRegistry());

        var operation = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_BinaryOpBodySyntax>(operation.Body);
        Assert.Equal("%lhs", body.Lhs.Text);
        Assert.Equal(hasRhs, body.CommaToken.HasValue);
        Assert.Equal(hasRhs, body.Rhs.HasValue);
        Assert.Equal("i32", body.ResultType.ToString());
        Assert.Equal(rhsName, body.Rhs.HasValue ? body.Rhs!.Value.Text : null);
    }

    [Theory]
    [InlineData("%result = minitest.binary %lhs, %rhs : i32", "%rhs")]
    [InlineData("%result = minitest.binary %lhs : i32", null)]
    public void BinaryOpBindsOptionalOperand(string source, string? rhsName)
    {
        var operation = BindSingleOperation<MiniTest_BinaryOp>(source, CreateMiniTestRegistry());

        Assert.Equal("minitest.binary", operation.Name);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
        Assert.Equal(rhsName, operation.Rhs?.Name);
    }

    [Theory]
    [InlineData("minitest.config\n    stride 4\n    padding 0\n    {}", true, true)]
    [InlineData("minitest.config\n    padding 0\n    stride 4\n    {}", true, true)]
    [InlineData("minitest.config\n    stride 4\n    {}", true, false)]
    public void ConfigOpParsesOilistClauses(string source, bool hasStride, bool hasPadding)
    {
        var document = Document.Parse(source, CreateMiniTestRegistry());

        var operation = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_ConfigOpBodySyntax>(operation.Body);
        Assert.Equal(hasStride, body.StrideKeyword.HasValue);
        Assert.Equal(hasStride, body.Stride is not null);
        Assert.Equal(hasPadding, body.PaddingKeyword.HasValue);
        Assert.Equal(hasPadding, body.Padding is not null);
    }

    [Theory]
    [InlineData("minitest.config\n    stride 4\n    padding 0\n    {}", 4, 0)]
    [InlineData("minitest.config\n    stride 4\n    {}", 4, null)]
    [InlineData("minitest.config {}", null, null)]
    public void ConfigOpBindsOptionalOilistAttributes(string source, int? stride, int? padding)
    {
        var operation = BindSingleOperation<MiniTest_ConfigOp>(source, CreateMiniTestRegistry());

        Assert.Equal("minitest.config", operation.Name);
        Assert.Equal(stride is null ? null : (uint?)stride.Value, operation.Stride);
        Assert.Equal(padding is null ? null : (uint?)padding.Value, operation.Padding);
    }

    [Fact]
    public void GeneratedOperandSetterUpdatesOptionalOperandAndCustomPrinting()
    {
        const string source =
            "%lhs = \"test.left\"() : () -> i32\n" +
            "%rhs = \"test.right\"() : () -> i32\n" +
            "%result = minitest.binary %lhs : i32";

        var registry = CreateMiniTestRegistry();
        var module = ParseAndBind(source, registry);
        var rhsValue = module.Operations[1].Results[0];
        var operation = Assert.IsType<MiniTest_BinaryOp>(module.Operations[2]);

        operation.Rhs = rhsValue;

        Assert.Same(rhsValue, operation.Rhs);

        var rebound = ParseAndBind(module.ToText(CustomAssemblyOptions), registry);
        var reboundOperation = Assert.IsType<MiniTest_BinaryOp>(rebound.Operations[2]);
        Assert.NotNull(reboundOperation.Rhs);
        Assert.Equal("%rhs", reboundOperation.Rhs!.Name);
    }

    [Fact]
    public void GeneratedAttributeSetterAddsAndRemovesOptionalAttribute()
    {
        var module = ParseAndBind("minitest.config {}", CreateMiniTestRegistry());
        var operation = Assert.IsType<MiniTest_ConfigOp>(Assert.Single(module.Operations));

        operation.Stride = 4u;

        Assert.Equal((uint?)4u, operation.Stride);
        Assert.Contains("stride 4", module.ToText(CustomAssemblyOptions));

        operation.Stride = null;

        Assert.Null(operation.Stride);
        Assert.DoesNotContain("stride", module.ToText(CustomAssemblyOptions));
    }
}
