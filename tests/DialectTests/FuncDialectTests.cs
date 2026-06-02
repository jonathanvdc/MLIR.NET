namespace DialectTests;

using MLIR;
using MLIR.Dialects.Func;
using MLIR.Semantics;
using MLIR.Dialects;
using Xunit;

/// <summary>
/// Integration tests for the func dialect, based on the upstream FuncOps.td examples.
/// </summary>
public sealed class FuncDialectTests : DialectIntegrationTestBase
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static DialectRegistry CreateFuncRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(FuncDialectRegistration.Create());
        return registry;
    }

    // ---------------------------------------------------------------------------
    // Registration
    // ---------------------------------------------------------------------------

    [Fact]
    public void RegistrationExposesPreludeFuncDefinitions()
    {
        var dialect = FuncDialectRegistration.Create();
        var registry = CreateFuncRegistry();
        var registeredFuncDef = Assert.Single(dialect.Operations, static op => op.Name == "func.func");

        Assert.Equal("func", dialect.Name);
        Assert.True(registry.TryGetOperation("func.call", out var callDef));
        Assert.True(registry.TryGetOperation("func.call_indirect", out var callIndirectDef));
        Assert.True(registry.TryGetOperation("func.constant", out var constantDef));
        Assert.True(registry.TryGetOperation("func.func", out var funcDef));
        Assert.True(registry.TryGetOperation("func.return", out var returnDef));

        Assert.NotNull(callDef?.AssemblyFormat);

        // These operations have declarative assembly formats.
        Assert.NotNull(callIndirectDef?.AssemblyFormat);
        Assert.NotNull(constantDef?.AssemblyFormat);
        Assert.NotNull(returnDef?.AssemblyFormat);

        Assert.NotNull(registeredFuncDef.AssemblyFormat);
        Assert.NotNull(funcDef?.AssemblyFormat);
    }

    // ---------------------------------------------------------------------------
    // func.return
    // ---------------------------------------------------------------------------

    /// <summary>
    /// func.return with no operands.
    /// Format: attr-dict ($operands^ ':' type($operands))?
    /// Example from FuncOps.td: func.return
    /// </summary>
    [Fact]
    public void BindsReturnOpWithNoOperands()
    {
        var op = BindSingleOperation<ReturnOp>(
            "func.return",
            CreateFuncRegistry());

        Assert.Empty(op.Operands);
        Assert.Null(op.TypeSignatureReference);
    }

    /// <summary>
    /// func.return with a single operand.
    /// Format: attr-dict ($operands^ ':' type($operands))?
    /// Example from FuncOps.td: return %operand : i32
    /// </summary>
    [Fact]
    public void BindsReturnOpWithSingleOperand()
    {
        var op = BindSingleOperation<ReturnOp>(
            "func.return %x : i32",
            CreateFuncRegistry());

        Assert.Single(op.Operands);
        Assert.Equal("%x", op.Operands[0].Name);
    }

    /// <summary>
    /// Round-trip: parse → reprint → reparse for func.return with no operands.
    /// </summary>
    [Fact]
    public void ReprintsReturnOpWithNoOperands()
    {
        var op = ReprintAndRebindSingleOperation<ReturnOp>(
            "func.return",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("func.return", printed);
        Assert.Empty(op.Operands);
    }

    /// <summary>
    /// Round-trip: parse → reprint → reparse for func.return with one operand.
    /// </summary>
    [Fact]
    public void ReprintsReturnOpWithSingleOperand()
    {
        var op = ReprintAndRebindSingleOperation<ReturnOp>(
            "func.return %x : i32",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("func.return", printed);
        Assert.Contains("%x", printed);
        Assert.Single(op.Operands);
        Assert.Equal("%x", op.Operands[0].Name);
    }

    /// <summary>
    /// func.return with multiple operands — the variadic list parses correctly, but
    /// <c>type($operands)</c> for a variadic generates only a single <c>ParseTypeSyntax()</c>
    /// call that reads <c>i32</c> and leaves <c>, f32</c> stranded, causing a
    /// "Expected end of operation" error.
    /// </summary>
    [Fact]
    public void BindsReturnOpWithMultipleOperands()
    {
        var op = BindSingleOperation<ReturnOp>(
            "func.return %x, %y : i32, f32",
            CreateFuncRegistry());

        Assert.Equal(2, op.Operands.Count);
        Assert.Equal("%x", op.Operands[0].Name);
        Assert.Equal("%y", op.Operands[1].Name);
    }

    // ---------------------------------------------------------------------------
    // func.call_indirect
    // ---------------------------------------------------------------------------

    /// <summary>
    /// func.call_indirect with a single operand and single result.
    /// Format: $callee '(' $callee_operands ')' attr-dict ':' type($callee)
    /// Example from FuncOps.td:
    ///   %result = func.call_indirect %func(%0) : (tensor&lt;16xf32&gt;) -> tensor&lt;16xf32&gt;
    /// </summary>
    [Fact]
    public void BindsCallIndirectOpWithSingleOperand()
    {
        var op = BindSingleOperation<CallIndirectOp>(
            "%result = func.call_indirect %callee(%arg0) : (i32) -> i32",
            CreateFuncRegistry());

        Assert.Equal("%callee", op.Callee.Name);
        Assert.Single(op.CalleeOperands);
        Assert.Equal("%arg0", op.CalleeOperands[0].Name);
        Assert.Equal("%result", Assert.Single(op.Results).Name);
    }

    /// <summary>
    /// Round-trip for func.call_indirect with a single operand.
    /// </summary>
    [Fact]
    public void ReprintsCallIndirectOpWithSingleOperand()
    {
        var op = ReprintAndRebindSingleOperation<CallIndirectOp>(
            "%result = func.call_indirect %callee(%arg0) : (i32) -> i32",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("func.call_indirect", printed);
        Assert.Contains("%callee", printed);
        Assert.Contains("%arg0", printed);
        Assert.Equal("%callee", op.Callee.Name);
        Assert.Single(op.CalleeOperands);
        Assert.Equal("%arg0", op.CalleeOperands[0].Name);
    }

    /// <summary>
    /// func.call_indirect with no operands — now supported via variadic operand parsing.
    /// Format: $callee '(' $callee_operands ')' attr-dict ':' type($callee)
    /// </summary>
    [Fact]
    public void BindsCallIndirectOpWithNoOperands()
    {
        var op = BindSingleOperation<CallIndirectOp>(
            "%result = func.call_indirect %callee() : () -> i32",
            CreateFuncRegistry());

        Assert.Equal("%callee", op.Callee.Name);
        Assert.Empty(op.CalleeOperands);
    }

    /// <summary>
    /// func.call_indirect with multiple operands — now supported via variadic operand parsing.
    /// Format: $callee '(' $callee_operands ')' attr-dict ':' type($callee)
    /// </summary>
    [Fact]
    public void BindsCallIndirectOpWithMultipleOperands()
    {
        var op = BindSingleOperation<CallIndirectOp>(
            "%result = func.call_indirect %callee(%arg0, %arg1) : (i32, i32) -> i32",
            CreateFuncRegistry());

        Assert.Equal("%callee", op.Callee.Name);
        Assert.Equal(2, op.CalleeOperands.Count);
        Assert.Equal("%arg0", op.CalleeOperands[0].Name);
        Assert.Equal("%arg1", op.CalleeOperands[1].Name);
    }

    // ---------------------------------------------------------------------------
    // func.call
    // ---------------------------------------------------------------------------

    /// <summary>
    /// func.call uses 'functional-type($operands, results)' in its assembly format.
    /// Example from FuncOps.td:
    ///   %result = func.call @my_add(%0, %1) : (f32, f32) -> f32
    /// </summary>
    [Fact]
    public void BindsCallOpFromCustomFormat()
    {
        var op = BindSingleOperation<CallOp>(
            "%result = func.call @my_add(%0, %1) : (f32, f32) -> f32",
            CreateFuncRegistry());

        Assert.NotNull(op);
    }

    // ---------------------------------------------------------------------------
    // func.constant
    // ---------------------------------------------------------------------------

    /// <summary>
    /// The ODS for func.constant declares 'let results = (outs AnyType)' with an
    /// unnamed result, now synthesized to 'result' in the ODS importer.
    /// Example from FuncOps.td:
    ///   %2 = func.constant @myfn : (tensor&lt;16xf32&gt;, f32) -&gt; tensor&lt;16xf32&gt;
    /// </summary>
    [Fact(Skip = "Temporarily disabled while constrained attribute parsing is rebuilt on the unified emitter.")]
    public void BindsConstantOpWithResult()
    {
        var op = BindSingleOperation<ConstantOp>(
            "%2 = func.constant @myfn : (i32) -> f32",
            CreateFuncRegistry());

        Assert.Equal("%2", op.ResultValue.Name);
        Assert.NotNull(op.Value);
    }

    /// <summary>
    /// Round-trip for func.constant.
    /// </summary>
    [Fact(Skip = "Temporarily disabled while constrained attribute parsing is rebuilt on the unified emitter.")]
    public void ReprintsConstantOp()
    {
        var op = ReprintAndRebindSingleOperation<ConstantOp>(
            "%2 = func.constant @myfn : (i32) -> f32",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("func.constant", printed);
        Assert.Contains("@myfn", printed);
        Assert.Equal("%2", op.ResultValue.Name);
    }

    // ---------------------------------------------------------------------------
    // func.func (FuncOp)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Binding the custom func.func syntax should populate the generated symbol
    /// and function-type accessors.
    /// </summary>
    [Fact]
    public void BindsFuncOpWithSignatureAndVisibility()
    {
        var op = BindSingleOperation<FuncOp>(
            "func.func public @count(%x: i64, %y: i32) -> (i64, i32)",
            CreateFuncRegistry());

        Assert.Equal("count", op.SymName);
        Assert.Equal("public", op.SymVisibility);
        Assert.NotNull(op.TypeSignatureReference);
    }

    /// <summary>
    /// Regression test for the synthetic function_type operand slot required by
    /// the generated FuncOp constructor.
    /// </summary>
    [Fact]
    public void ReprintsFuncOpPrivateAbort()
    {
        var op = ReprintAndRebindSingleOperation<FuncOp>(
            "func.func private @abort()",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("func.func private @abort()", printed);
        Assert.Equal("abort", op.SymName);
        Assert.Equal("private", op.SymVisibility);
    }

    /// <summary>
    /// Round-tripping should preserve a func.func body and its function-level
    /// attributes through the custom assembly formatter.
    /// </summary>
    [Fact(Skip = "Temporarily disabled while optional groups are rebuilt on the unified emitter.")]
    public void RoundTripsFuncOpWithBodyAndAttributes()
    {
        var module = ParseAndBind(
            "func.func @count() attributes {fruit = \"banana\"} {\n" +
            "  func.return\n" +
            "}",
            CreateFuncRegistry());

        var printed = module.ToText(CustomAssemblyOptions);
        var rebound = ParseAndBind(printed, CreateFuncRegistry());

        Assert.Contains("func.func @count()", printed);
        Assert.Contains("fruit = \"banana\"", printed);
        Assert.Contains("func.return", printed);
        Assert.Empty(rebound.AssemblyDiagnostics);

        var reboundFunc = Assert.Single(rebound.Operations);
        var reboundFuncOp = Assert.IsType<FuncOp>(reboundFunc);
        Assert.Equal("count", reboundFuncOp.SymName);
        Assert.Equal("fruit", reboundFuncOp.Attributes["fruit"].Name);
        Assert.NotNull(reboundFuncOp.Attributes["fruit"].Value.Syntax);
    }

    /// <summary>
    /// Argument attribute dictionaries should be preserved by the custom
    /// func.func assembly format.
    /// </summary>
    [Fact]
    public void ReprintsFuncOpWithArgumentAttributes()
    {
        var op = ReprintAndRebindSingleOperation<FuncOp>(
            "func.func private @example_fn_arg(%x: i32 {swift.self = unit})",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("example_fn_arg", printed);
        Assert.Contains("swift.self = unit", printed);
        Assert.Equal("example_fn_arg", op.SymName);
        Assert.NotNull(op.TypeSignatureReference);
    }

    /// <summary>
    /// Result attribute dictionaries should be preserved by the custom func.func
    /// assembly format.
    /// </summary>
    [Fact]
    public void ReprintsFuncOpWithResultAttributes()
    {
        var op = ReprintAndRebindSingleOperation<FuncOp>(
            "func.func private @example_fn_result() -> (f64 {dialectName.attrName = 0 : i64})",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("example_fn_result", printed);
        Assert.Contains("dialectName.attrName = 0 : i64", printed);
        Assert.Contains("->", printed);
        Assert.Equal("example_fn_result", op.SymName);
        Assert.NotNull(op.TypeSignatureReference);
    }

    /// <summary>
    /// Function-level attributes and nested regions should round-trip through the
    /// custom func.func formatter.
    /// </summary>
    [Fact(Skip = "Temporarily disabled while optional groups are rebuilt on the unified emitter.")]
    public void ReprintsFuncOpWithAttributesAndBody()
    {
        var op = ReprintAndRebindSingleOperation<FuncOp>(
            "func.func @count() attributes {fruit = \"banana\"} {\n  func.return\n}",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("fruit = \"banana\"", printed);
        Assert.Contains("func.return", printed);
        Assert.Equal("count", op.SymName);
        Assert.NotNull(op.TypeSignatureReference);
    }
}
