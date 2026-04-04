namespace DialectTests;

using System.Linq;
using MLIR;
using MLIR.Arith;
using MLIR.Dialects;
using MLIR.Func;
using MLIR.Semantics;
using Xunit;

/// <summary>
/// Integration tests that exercise multiple real prelude dialects in the same module.
/// </summary>
public sealed class MixedPreludeDialectTests : DialectIntegrationTestBase
{
    private static DialectRegistry CreateMixedPreludeRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(ArithDialectRegistration.Create());
        registry.RegisterDialect(FuncDialectRegistration.Create());
        return registry;
    }

    [Fact]
    public void BindsMixedPreludeDialectOperationsFromTheSameModule()
    {
        var module = ParseAndBind(
            "%c0 = arith.constant 7 : i32\n" +
            "func.func public @compute(%arg0: i32) -> i32\n" +
            "%sum = arith.addi %c0, %c0 : i32",
            CreateMixedPreludeRegistry());

        Assert.Equal(3, module.Operations.Count);

        var constant = Assert.IsType<Arith_ConstantOp>(module.Operations[0]);
        var func = Assert.IsType<FuncOp>(module.Operations[1]);
        var addi = Assert.IsType<Arith_AddIOp>(module.Operations[2]);

        Assert.Equal("%c0", constant.ResultValue.Name);
        Assert.Equal("compute", func.SymName);
        Assert.Equal("public", func.SymVisibility);
        Assert.Equal("%sum", addi.ResultValue.Name);
        Assert.Equal("%c0", addi.Lhs.Name);
        Assert.Equal("%c0", addi.Rhs.Name);
    }

    [Fact]
    public void MixedPreludeDialectsRoundTripAsCustomAssembly()
    {
        var module = ParseAndBind(
            "%c0 = arith.constant 7 : i32\n" +
            "func.func private @helper()\n" +
            "%sum = arith.addi %c0, %c0 : i32",
            CreateMixedPreludeRegistry());

        var printed = module.ToText(CustomAssemblyOptions);
        var rebound = ParseAndBind(printed, CreateMixedPreludeRegistry());

        Assert.Contains("arith.constant", printed);
        Assert.Contains("arith.addi", printed);
        Assert.Contains("func.func private @helper()", printed);
        Assert.Empty(rebound.AssemblyDiagnostics);
        Assert.Equal(3, rebound.Operations.Count);
        Assert.Equal(["arith.constant", "func.func", "arith.addi"], rebound.Operations.Select(static op => op.Name).ToArray());
    }

    [Fact]
    public void MixedPreludeDialectsStayKnownAfterRoundTrip()
    {
        var module = ParseAndBind(
            "%c0 = arith.constant 42 : i32\n" +
            "func.func @identity(%arg0: i32) -> i32\n" +
            "%sum = arith.addi %c0, %c0 : i32",
            CreateMixedPreludeRegistry());

        var printed = module.ToText(CustomAssemblyOptions);
        var rebound = ParseAndBind(printed, CreateMixedPreludeRegistry());

        Assert.All(rebound.Operations, static op => Assert.True(op.IsKnown, op.Name));
        Assert.Equal("identity", Assert.IsType<FuncOp>(rebound.Operations[1]).SymName);
        Assert.Equal("%sum", Assert.IsType<Arith_AddIOp>(rebound.Operations[2]).ResultValue.Name);
    }
}
