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

        // FuncOp has hasCustomAssemblyFormat = 1 (no declarative format).
        Assert.Null(funcDef?.AssemblyFormat);
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

        Assert.Null(op.Operands);
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

        Assert.Equal("%x", op.Operands?.Name);
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
        Assert.Null(op.Operands);
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
        Assert.Equal("%x", op.Operands?.Name);
    }

    /// <summary>
    /// func.return with multiple operands is not yet supported by the declarative parser.
    /// The generated TryParse only reads a single SSA token for the variadic operand,
    /// so parsing 'func.return %x, %y : i32, f32' would throw a parse exception.
    /// </summary>
    [Fact(Skip = "Variadic operands in optional group: TryParse reads only one SSA token; " +
                 "parsing multiple operands throws a parse exception. " +
                 "Needs variadic-aware operand parsing support in the generator.")]
    public void BindsReturnOpWithMultipleOperands()
    {
        var op = BindSingleOperation<ReturnOp>(
            "func.return %x, %y : i32, f32",
            CreateFuncRegistry());

        Assert.NotNull(op.Operands);
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
        Assert.Equal("%arg0", op.CalleeOperands.Name);
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
        Assert.Equal("%arg0", op.CalleeOperands.Name);
    }

    /// <summary>
    /// func.call_indirect with no operands is not yet supported.
    /// The TryParse always attempts context.ParseSsaToken() for the variadic operand,
    /// which throws a ParseException when the argument list is empty.
    /// </summary>
    [Fact(Skip = "Empty variadic operand list: TryParse calls context.ParseSsaToken() " +
                 "unconditionally and throws a ParseException when the arg list is empty. " +
                 "Needs variadic-aware operand parsing support in the generator.")]
    public void BindsCallIndirectOpWithNoOperands()
    {
        var op = BindSingleOperation<CallIndirectOp>(
            "%result = func.call_indirect %callee() : () -> i32",
            CreateFuncRegistry());

        Assert.Equal("%callee", op.Callee.Name);
    }

    /// <summary>
    /// func.call_indirect with multiple operands is not yet supported.
    /// The TryParse only parses one SSA token for the variadic operand field,
    /// so the second operand and the closing paren mismatch causes a ParseException.
    /// </summary>
    [Fact(Skip = "Variadic callee_operands: TryParse reads only one SSA token; " +
                 "multiple operands cause a ParseException. " +
                 "Needs variadic-aware operand parsing support in the generator.")]
    public void BindsCallIndirectOpWithMultipleOperands()
    {
        var op = BindSingleOperation<CallIndirectOp>(
            "%result = func.call_indirect %callee(%arg0, %arg1) : (i32, i32) -> i32",
            CreateFuncRegistry());

        Assert.Equal("%callee", op.Callee.Name);
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
    [Fact(Skip = "func.call uses functional-type(...) in its assembly format. " +
                 "The generated TryParse returns false for unsupported directives, " +
                 "so the parser falls back to generic format and binding yields " +
                 "UninterpretedOperation instead of CallOp. " +
                 "Would need functional-type parsing support in the generator.")]
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
    /// unnamed result.  The ODS importer skips unnamed results, so the generated
    /// ConstantOp expects exactly 0 result tokens.  Parsing '%2 = func.constant ...'
    /// (with a result prefix) causes the Bind method to report a diagnostic and
    /// return UninterpretedOperation.
    /// Example from FuncOps.td:
    ///   %2 = func.constant @myfn : (tensor&lt;16xf32&gt;, f32) -&gt; tensor&lt;16xf32&gt;
    /// </summary>
    [Fact(Skip = "Unnamed results are not imported from ODS. " +
                 "The generated ConstantOp has 0 results, but func.constant " +
                 "produces one result in MLIR.  Binding yields UninterpretedOperation. " +
                 "Needs unnamed-result support in the ODS importer.")]
    public void BindsConstantOpWithResult()
    {
        var op = BindSingleOperation<ConstantOp>(
            "%2 = func.constant @myfn : (i32) -> f32",
            CreateFuncRegistry());

        Assert.NotNull(op);
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
    [Fact(Skip = "FuncOp uses hasCustomAssemblyFormat = 1; no declarative format is generated. " +
                 "Parsing falls back to generic format and binding yields UninterpretedOperation. " +
                 "Would need a hand-written assembly format extension.")]
    public void BindsFuncOp()
    {
        var op = BindSingleOperation<FuncOp>(
            "func.func private @abort()",
            CreateFuncRegistry());

        Assert.NotNull(op);
    }
}
