namespace DialectTests;

using System.Linq;
using MLIR;
using MLIR.Dialects.Arith;
using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Dialects.Extensions;
using MLIR.Dialects.Func;
using MLIR.Semantics;
using MLIR.Syntax;
using Xunit;

/// <summary>
/// Integration tests that exercise multiple real prelude dialects in the same module.
/// </summary>
public sealed class MixedPreludeDialectTests : DialectIntegrationTestBase
{
    private const string MixedPreludeModuleSource =
        "%c0 = arith.constant 7 : i32\n" +
        "func.func public @kernel(%arg0: i32) -> i32";

    private const string ScaleModuleSource =
        "module {\n" +
        "  func.func @scale(%x: i32) -> i32 {\n" +
        "    %c10 = arith.constant 10 : i32\n" +
        "    %res = arith.muli %x, %c10 : i32\n" +
        "    func.return %res : i32\n" +
        "  }\n" +
        "}";

    private const string LinearModuleSource =
        "module {\n" +
        "  func.func @linear(%x: i32) -> i32 {\n" +
        "    %c2 = arith.constant 2 : i32\n" +
        "    %c3 = arith.constant 3 : i32\n" +
        "    %t0 = arith.muli %x, %c2 : i32\n" +
        "    %t1 = arith.addi %t0, %c3 : i32\n" +
        "    func.return %t1 : i32\n" +
        "  }\n" +
        "}";

    private static DialectRegistry CreateMixedPreludeRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(BuiltinDialectRegistration.Create());
        registry.RegisterDialect(ArithDialectRegistration.Create());
        registry.RegisterDialect(FuncDialectRegistration.Create());
        return registry;
    }

    [Fact]
    public void ParsesMixedPreludeModuleWithCustomFuncSyntax()
    {
        var document = Document.Parse(MixedPreludeModuleSource, CreateMixedPreludeRegistry());

        var syntax = document.Module.Operations[1];
        var body = Assert.IsType<FuncOperationBodySyntax>(syntax.Body);

        Assert.Equal("func.func", syntax.Name);
        Assert.True(syntax.HasCustomAssemblyBody);
        Assert.Equal("public", body.VisibilityToken?.Text);
        Assert.Equal("@kernel", body.SymbolName.Text);
        Assert.Single(body.Arguments);
        Assert.Equal("%arg0", body.Arguments[0].Name.Text);
        Assert.Null(body.BodyRegion);
    }

    [Fact]
    public void BindsMixedPreludeModuleAndInspectsTheNestedAst()
    {
        var module = ParseAndBind(MixedPreludeModuleSource, CreateMixedPreludeRegistry());

        Assert.Collection(
            module.Operations,
            static op => Assert.IsType<Arith_ConstantOp>(op),
            static op => Assert.IsType<FuncOp>(op));

        var constant = Assert.IsType<Arith_ConstantOp>(module.Operations[0]);
        var func = Assert.IsType<FuncOp>(module.Operations[1]);

        Assert.Equal("%c0", constant.ResultValue.Name);
        Assert.Equal("kernel", func.SymName);
        Assert.Equal("public", func.SymVisibility);
        Assert.NotNull(func.TypeSignatureReference);
    }

    [Fact]
    public void PrintsMixedPreludeModuleAsCustomAssembly()
    {
        var module = ParseAndBind(MixedPreludeModuleSource, CreateMixedPreludeRegistry());

        var printed = module.ToText(CustomAssemblyOptions);

        Assert.Contains("func.func public @kernel(%arg0 : i32) -> i32", printed);
        Assert.Contains("arith.constant 7 : i32", printed);
    }

    [Fact]
    public void RoundTripsMixedPreludeModuleThroughCustomAssembly()
    {
        var module = ParseAndBind(MixedPreludeModuleSource, CreateMixedPreludeRegistry());

        var printed = module.ToText(CustomAssemblyOptions);
        var rebound = ParseAndBind(printed, CreateMixedPreludeRegistry());

        Assert.Empty(rebound.AssemblyDiagnostics);
        Assert.Collection(
            rebound.Operations,
            static op => Assert.IsType<Arith_ConstantOp>(op),
            static op => Assert.IsType<FuncOp>(op));
        Assert.Equal(printed, rebound.ToText(CustomAssemblyOptions));
        Assert.All(rebound.Operations, static op => Assert.True(op.IsKnown, op.Name));
        var reboundFunc = Assert.IsType<FuncOp>(rebound.Operations[1]);
        Assert.Equal("kernel", reboundFunc.SymName);
        Assert.Equal("public", reboundFunc.SymVisibility);
        Assert.NotNull(reboundFunc.TypeSignatureReference);
    }

    [Fact]
    public void ScaleModuleParsesBindsPrintsAndRoundsTrip()
    {
        var document = Document.Parse(ScaleModuleSource, CreateMixedPreludeRegistry());
        var moduleSyntax = document.Module;
        var moduleSyntaxOp = Assert.Single(moduleSyntax.Operations);
        Assert.Equal("ModuleOpSyntax", moduleSyntaxOp.Body.GetType().Name);
        var moduleRegion = GetBodyRegion(moduleSyntaxOp.Body);
        var moduleBlock = Assert.Single(moduleRegion.Blocks);
        var funcSyntax = Assert.IsType<OperationSyntax>(moduleBlock.Operations[0]);
        var funcBody = Assert.IsType<FuncOperationBodySyntax>(funcSyntax.Body);

        Assert.Equal("module", moduleSyntaxOp.Name);
        Assert.Equal("func.func", funcSyntax.Name);
        Assert.Equal("@scale", funcBody.SymbolName.Text);
        Assert.Single(funcBody.Arguments);
        Assert.Single(funcBody.BodyRegion!.Blocks);
        Assert.Equal(3, funcBody.BodyRegion.Blocks[0].Operations.Count);

        var module = ParseAndBind(ScaleModuleSource, CreateMixedPreludeRegistry());
        var boundModule = Assert.IsType<ModuleOp>(Assert.Single(module.Operations));
        var boundModuleRegion = Assert.Single(boundModule.Regions);
        var boundModuleBlock = Assert.Single(boundModuleRegion.Blocks);
        var boundFunc = Assert.IsType<FuncOp>(boundModuleBlock.Operations[0]);
        var boundFuncSyntax = Assert.IsType<FuncOperationBodySyntax>(boundFunc.Syntax!.Body);

        Assert.Equal("scale", boundFunc.SymName);
        Assert.NotNull(boundFunc.TypeSignatureReference);
        Assert.Single(boundFuncSyntax.BodyRegion!.Blocks);
        Assert.Equal(3, boundFuncSyntax.BodyRegion.Blocks[0].Operations.Count);

        var printed = module.ToText(CustomAssemblyOptions);
        var rebound = ParseAndBind(printed, CreateMixedPreludeRegistry());

        Assert.Contains("module {", printed);
        Assert.Contains("func.func @scale(%x : i32) -> i32", printed);
        Assert.Contains("%c10 = arith.constant 10 : i32", printed);
        Assert.Contains("%res = arith.muli %x, %c10 : i32", printed);
        Assert.Contains("func.return %res : i32", printed);
        Assert.Empty(rebound.AssemblyDiagnostics);
        Assert.Equal(printed, rebound.ToText(CustomAssemblyOptions));
    }

    [Fact]
    public void LinearModuleParsesBindsPrintsAndRoundsTrip()
    {
        var document = Document.Parse(LinearModuleSource, CreateMixedPreludeRegistry());
        var moduleSyntax = document.Module;
        var moduleSyntaxOp = Assert.Single(moduleSyntax.Operations);
        Assert.Equal("ModuleOpSyntax", moduleSyntaxOp.Body.GetType().Name);
        var moduleRegion = GetBodyRegion(moduleSyntaxOp.Body);
        var moduleBlock = Assert.Single(moduleRegion.Blocks);
        var funcSyntax = Assert.IsType<OperationSyntax>(moduleBlock.Operations[0]);
        var funcBody = Assert.IsType<FuncOperationBodySyntax>(funcSyntax.Body);

        Assert.Equal("module", moduleSyntaxOp.Name);
        Assert.Equal("func.func", funcSyntax.Name);
        Assert.Equal("@linear", funcBody.SymbolName.Text);
        Assert.Single(funcBody.Arguments);
        Assert.Single(funcBody.BodyRegion!.Blocks);
        Assert.Equal(5, funcBody.BodyRegion.Blocks[0].Operations.Count);

        var module = ParseAndBind(LinearModuleSource, CreateMixedPreludeRegistry());
        var boundModule = Assert.IsType<ModuleOp>(Assert.Single(module.Operations));
        var boundModuleRegion = Assert.Single(boundModule.Regions);
        var boundModuleBlock = Assert.Single(boundModuleRegion.Blocks);
        var boundFunc = Assert.IsType<FuncOp>(boundModuleBlock.Operations[0]);
        var boundFuncSyntax = Assert.IsType<FuncOperationBodySyntax>(boundFunc.Syntax!.Body);

        Assert.Equal("linear", boundFunc.SymName);
        Assert.NotNull(boundFunc.TypeSignatureReference);
        Assert.Single(boundFuncSyntax.BodyRegion!.Blocks);
        Assert.Equal(5, boundFuncSyntax.BodyRegion.Blocks[0].Operations.Count);

        var printed = module.ToText(CustomAssemblyOptions);
        var rebound = ParseAndBind(printed, CreateMixedPreludeRegistry());

        Assert.Contains("module {", printed);
        Assert.Contains("func.func @linear(%x : i32) -> i32", printed);
        Assert.Contains("%c2 = arith.constant 2 : i32", printed);
        Assert.Contains("%c3 = arith.constant 3 : i32", printed);
        Assert.Contains("%t0 = arith.muli %x, %c2 : i32", printed);
        Assert.Contains("%t1 = arith.addi %t0, %c3 : i32", printed);
        Assert.Contains("func.return %t1 : i32", printed);
        Assert.Empty(rebound.AssemblyDiagnostics);
        Assert.Equal(printed, rebound.ToText(CustomAssemblyOptions));
    }

    private static RegionSyntax GetBodyRegion(OperationBodySyntax body)
    {
        var property = body.GetType().GetProperty("BodyRegion");
        Assert.NotNull(property);
        return Assert.IsType<RegionSyntax>(property.GetValue(body));
    }
}
