namespace DialectTests;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR;
using MLIR.Dialects.Arith;
using MLIR.Semantics;
using Xunit;

public sealed class ArithDialectTests : DialectIntegrationTestBase
{
    public static TheoryData<string, int, int> ExampleModules => CreateExampleModules();

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
    public void MinimalGeneratedArithConstantCustomAssemblyParses()
    {
        var document = Document.Parse("%0 = arith.constant 42 : i32", CreateArithRegistry());

        Assert.Single(document.Module.Operations);
    }

    [Fact]
    public void BindsIntegerConstantFromPreludeDialect()
    {
        var operation = BindSingleOperation<Arith_ConstantOp>(
            "%value = arith.constant 42 : i32",
            CreateArithRegistry());

        Assert.Equal("%value", operation.ResultValue.Name);
        Assert.Equal("42 : i32", operation.Value.Syntax!.ToString());
    }

    [Fact(Skip = "Temporarily disabled while optional groups are rebuilt on the unified emitter.")]
    public void BindsIntegerAddExpressionFromPreludeDialect()
    {
        var operation = BindSingleOperation<Arith_AddIOp>(
            "%sum = arith.addi %lhs, %rhs : i32",
            CreateArithRegistry());

        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%sum", operation.ResultValue.Name);
    }

    [Fact(Skip = "Temporarily disabled while optional groups are rebuilt on the unified emitter.")]
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

    [Fact(Skip = "Temporarily disabled while optional groups are rebuilt on the unified emitter.")]
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

    [Fact(Skip = "Temporarily disabled while optional groups are rebuilt on the unified emitter.")]
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
        Assert.Contains(": i32", printed.Replace("%rhs:", "%rhs :", StringComparison.Ordinal));
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
    }

    [Theory(Skip = "Temporarily disabled while optional groups are rebuilt on the unified emitter.")]
    [MemberData(nameof(ExampleModules))]
    public void EveryArithOpsTdExampleBindsThroughGeneratedDialect(string source, int expectedOperationCount, int expectedArithOperationCount)
    {
        var module = ParseAndBind(source, CreateArithRegistry());

        Assert.Equal(expectedOperationCount, module.Operations.Count);
        Assert.Empty(module.AssemblyDiagnostics);

        var arithOperations = module.Operations
            .Where(static operation => operation.Name.StartsWith("arith.", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(expectedArithOperationCount, arithOperations.Length);
        Assert.All(arithOperations, static operation => Assert.False(operation is UninterpretedOperation, operation.Name));
    }

    private static TheoryData<string, int, int> CreateExampleModules()
    {
        var data = new TheoryData<string, int, int>();

        static string Join(params string[] lines) => string.Join("\n", lines);
        static void Add(TheoryData<string, int, int> data, string source, int totalOps, int arithOps) => data.Add(source, totalOps, arithOps);

        Add(data, "%1 = arith.constant 42 : i32", 1, 1);
        Add(data, "%1 = \"arith.constant\"() {value = 42 : i32} : () -> i32", 1, 1);

        Add(data, "%a = arith.addi %b, %c : i64", 1, 1);
        Add(data, "%a = arith.addi %b, %c overflow<nsw, nuw> : i64", 1, 1);
        Add(data, "%f = arith.addi %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.addi %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%sum, %overflow = arith.addui_extended %b, %c : i64, i1", 1, 1);
        Add(data, "%d:2 = arith.addui_extended %e, %f : vector<4xi32>, vector<4xi1>", 1, 1);
        Add(data, "%x:2 = arith.addui_extended %y, %z : tensor<4x?xi8>, tensor<4x?xi1>", 1, 1);

        Add(data, "%a = arith.subi %b, %c : i64", 1, 1);
        Add(data, "%a = arith.subi %b, %c overflow<nsw, nuw> : i64", 1, 1);
        Add(data, "%f = arith.subi %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.subi %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.muli %b, %c : i64", 1, 1);
        Add(data, "%a = arith.muli %b, %c overflow<nsw, nuw> : i64", 1, 1);
        Add(data, "%f = arith.muli %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.muli %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%low, %high = arith.mulsi_extended %a, %b : i32", 1, 1);
        Add(data, "%c:2 = arith.mulsi_extended %d, %e : vector<4xi32>", 1, 1);
        Add(data, "%x:2 = arith.mulsi_extended %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%low, %high = arith.mului_extended %a, %b : i32", 1, 1);
        Add(data, "%c:2 = arith.mului_extended %d, %e : vector<4xi32>", 1, 1);
        Add(data, "%x:2 = arith.mului_extended %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.divui %b, %c : i64", 1, 1);
        Add(data, "%a = arith.divui %b, %c exact : i64", 1, 1);
        Add(data, "%f = arith.divui %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.divui %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.divsi %b, %c : i64", 1, 1);
        Add(data, "%a = arith.divsi %b, %c exact : i64", 1, 1);
        Add(data, "%f = arith.divsi %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.divsi %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.ceildivui %b, %c : i64", 1, 1);
        Add(data, "%a = arith.ceildivsi %b, %c : i64", 1, 1);
        Add(data, "%a = arith.floordivsi %b, %c : i64", 1, 1);

        Add(data, "%a = arith.remui %b, %c : i64", 1, 1);
        Add(data, "%f = arith.remui %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.remui %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.remsi %b, %c : i64", 1, 1);
        Add(data, "%f = arith.remsi %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.remsi %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.andi %b, %c : i64", 1, 1);
        Add(data, "%f = arith.andi %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.andi %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.ori %b, %c : i64", 1, 1);
        Add(data, "%f = arith.ori %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.ori %y, %z : tensor<4x?xi8>", 1, 1);

        Add(data, "%a = arith.xori %b, %c : i64", 1, 1);
        Add(data, "%f = arith.xori %g, %h : vector<4xi32>", 1, 1);
        Add(data, "%x = arith.xori %y, %z : tensor<4x?xi8>", 1, 1);

        Add(
            data,
            Join(
                "%1 = arith.constant 5 : i8",
                "%2 = arith.constant 3 : i8",
                "%3 = arith.shli %1, %2 : i8",
                "%4 = arith.shli %1, %2 overflow<nsw, nuw> : i8"),
            4,
            4);

        Add(
            data,
            Join(
                "%1 = arith.constant 160 : i8",
                "%2 = arith.constant 3 : i8",
                "%3 = arith.constant 6 : i8",
                "%4 = arith.shrui %1, %2 exact : i8",
                "%5 = arith.shrui %1, %3 : i8"),
            5,
            5);

        Add(
            data,
            Join(
                "%1 = arith.constant 160 : i8",
                "%2 = arith.constant 3 : i8",
                "%3 = arith.shrsi %1, %2 exact : i8",
                "%4 = arith.constant 98 : i8",
                "%5 = arith.shrsi %4, %2 : i8"),
            5,
            5);

        Add(data, "%a = arith.negf %b : f64", 1, 1);
        Add(data, "%f = arith.negf %g : vector<4xf32>", 1, 1);
        Add(data, "%x = arith.negf %y : tensor<4x?xf8>", 1, 1);

        Add(data, "%a = arith.addf %b, %c : f64", 1, 1);
        Add(data, "%f = arith.addf %g, %h : vector<4xf32>", 1, 1);
        Add(data, "%x = arith.addf %y, %z : tensor<4x?xbf16>", 1, 1);

        Add(data, "%a = arith.subf %b, %c : f64", 1, 1);
        Add(data, "%f = arith.subf %g, %h : vector<4xf32>", 1, 1);
        Add(data, "%x = arith.subf %y, %z : tensor<4x?xbf16>", 1, 1);

        Add(data, "%a = arith.maximumf %b, %c : f64", 1, 1);
        Add(data, "%a = arith.maxnumf %b, %c : f64", 1, 1);
        Add(data, "%a = arith.minimumf %b, %c : f64", 1, 1);
        Add(data, "%a = arith.minnumf %b, %c : f64", 1, 1);

        Add(data, "%a = arith.mulf %b, %c : f64", 1, 1);
        Add(data, "%f = arith.mulf %g, %h : vector<4xf32>", 1, 1);
        Add(data, "%x = arith.mulf %y, %z : tensor<4x?xbf16>", 1, 1);

        Add(
            data,
            Join(
                "%1 = arith.constant 5 : i3",
                "%2 = arith.extui %1 : i3 to i6",
                "%3 = arith.constant 2 : i3",
                "%4 = arith.extui %3 : i3 to i6",
                "%5 = arith.extui %0 : vector<2 x i32> to vector<2 x i64>",
                "%6 = arith.extui %3 nneg : i3 to i6"),
            6,
            6);

        Add(
            data,
            Join(
                "%1 = arith.constant 5 : i3",
                "%2 = arith.extsi %1 : i3 to i6",
                "%3 = arith.constant 2 : i3",
                "%4 = arith.extsi %3 : i3 to i6",
                "%5 = arith.extsi %0 : vector<2 x i32> to vector<2 x i64>"),
            5,
            5);

        Add(
            data,
            Join(
                "%0 = arith.truncf %1 : f32 to f8E8M0FNU",
                "%1 = arith.extf %0 : f8E8M0FNU to f16",
                "%2 = arith.extf %3 : f4E2M1FN to f16",
                "%3 = arith.mulf %2, %1 : f16"),
            4,
            4);

        Add(data, "%a = arith.scaling_extf %b, %c : f4E2M1FN, f8E8M0FNU to f32", 1, 1);
        Add(
            data,
            Join(
                "%f = vector.broadcast %g : vector<1xf8E8M0FNU> to vector<32xf8E8M0FNU>",
                "%h = arith.scaling_extf %i, %f : vector<32xf4E2M1FN>, vector<32xf8E8M0FNU> to vector<32xbf16>"),
            2,
            1);

        Add(
            data,
            Join(
                "%1 = arith.constant 21 : i5",
                "%2 = arith.trunci %1 : i5 to i4",
                "%3 = arith.trunci %1 : i5 to i3",
                "%4 = arith.trunci %0 : vector<2 x i32> to vector<2 x i16>",
                "%5 = arith.trunci %a overflow<nsw, nuw> : i32 to i16"),
            5,
            5);

        Add(
            data,
            Join(
                "%0 = arith.truncf %1 : f32 to f8E8M0FNU",
                "%1 = arith.extf %0 : f8E8M0FNU to f16",
                "%3 = arith.divf %2, %1 : f16",
                "%4 = arith.truncf %3 : f16 to f4E2M1FN"),
            4,
            4);

        Add(data, "%a = arith.scaling_truncf %b, %c : f32, f8E8M0FNU to f4E2M1FN", 1, 1);
        Add(
            data,
            Join(
                "%f = vector.broadcast %g : vector<1xf8E8M0FNU> to vector<32xf8E8M0FNU>",
                "%h = arith.scaling_truncf %i, %f : vector<32xbf16>, vector<32xf8E8M0FNU> to vector<32xf4E2M1FN>"),
            2,
            1);

        Add(
            data,
            Join(
                "%0 = arith.uitofp %a : i32 to f64",
                "%1 = arith.uitofp %a nneg : i32 to f64"),
            2,
            2);

        Add(
            data,
            Join(
                "%0 = arith.index_castui %a : i32 to index",
                "%1 = arith.index_castui %a nneg : i32 to index",
                "%2 = arith.index_castui %b nneg : index to i64"),
            3,
            3);

        Add(data, "%x = arith.cmpi slt, %lhs, %rhs : i32", 1, 1);
        Add(data, "%x = \"arith.cmpi\"(%lhs, %rhs) {predicate = 2 : i64} : (i32, i32) -> i1", 1, 1);
        Add(data, "%x = arith.cmpi eq, %lhs, %rhs : vector<4xi64>", 1, 1);
        Add(data, Join(
                "%x = \"arith.cmpi\"(%lhs, %rhs) {predicate = 0 : i64}",
                "    : (vector<4xi64>, vector<4xi64>) -> vector<4xi1>"),
            1,
            1);

        Add(data, "%r1 = arith.cmpf oeq, %0, %1 : f32", 1, 1);
        Add(data, "%r2 = arith.cmpf ult, %0, %1 : tensor<42x42xf64>", 1, 1);
        Add(data, "%r3 = \"arith.cmpf\"(%0, %1) {predicate: 0} : (f8, f8) -> i1", 1, 1);

        Add(data, "%x = arith.select %cond, %true, %false : i32", 1, 1);
        Add(data, "%x = \"arith.select\"(%cond, %true, %false) : (i1, i32, i32) -> i32", 1, 1);
        Add(data, "%vx = arith.select %vcond, %vtrue, %vfalse : vector<42xi1>, vector<42xf32>", 1, 1);
        Add(data, "%vx = arith.select %cond, %vtrue, %vfalse : vector<42xf32>", 1, 1);

        return data;
    }
}
