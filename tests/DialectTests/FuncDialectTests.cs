namespace DialectTests;

using MLIR;
using MLIR.Func;
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

        // CallOp falls back to generic parsing (functional-type is unsupported in TryParse),
        // but it should still have an assembly format object registered.
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
        Assert.Equal("%result", op.Results.Name);
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
    /// func.call uses 'functional-type($operands, results)' in its assembly format,
    /// which the generator's TryParse cannot handle.  The parser falls back to the
    /// generic format and the binding produces an UninterpretedOperation.
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
    [Fact]
    public void BindsConstantOpWithResult()
    {
        var op = BindSingleOperation<ConstantOp>(
            "%2 = func.constant @myfn : (i32) -> f32",
            CreateFuncRegistry());

        Assert.Equal("%2", op.ResultValue.Name);
        Assert.Equal("value", op.Value.Name);
    }

    /// <summary>
    /// Round-trip for func.constant.
    /// </summary>
    [Fact]
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
    /// FuncOp sets 'hasCustomAssemblyFormat = 1' and has no declarative assembly format.
    /// Accordingly, the generator does not emit an IOperationAssemblyFormat for FuncOp.
    /// Parsing a func.func definition falls through to the generic format.
    /// Example from FuncOps.td:
    ///   func.func @count(%x: i64) -> (i64, i64) { return %x, %x: i64, i64 }
    /// </summary>
    [Fact]
    public void BindsFuncOp()
    {
        var op = BindSingleOperation<FuncOp>(
            "func.func private @abort()",
            CreateFuncRegistry());

        Assert.NotNull(op);
    }

    /// <summary>
    /// Round-trip for func.func with the registered custom assembly format.
    /// </summary>
    [Fact]
    public void ReprintsFuncOp()
    {
        var op = ReprintAndRebindSingleOperation<FuncOp>(
            "func.func private @abort()",
            CreateFuncRegistry(),
            out var printed);

        Assert.Contains("func.func private @abort()", printed);
        Assert.NotNull(op);
    }
}
