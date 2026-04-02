namespace MLIR.Tests;

using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Text;
using Xunit;

public sealed partial class SemanticTests
{
    [Fact]
    public void SemanticPrinterUsesCustomAssemblyFormatsWhenAvailable()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation =>
                        {
                            operation.Result("result")
                                .RequiredAttribute("value")
                                .WithFactory(static context => new GeneratedConstantOperation(context))
                                .WithAssemblyFormat(new PrefixConstantAssemblyFormat());
                        });
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0} : () -> i32"),
            registry);

        Assert.Equal("%0 = arith.constant 0 : () -> i32", module.ToText(ReplaceExistingSyntaxOptions()));
    }

    [Fact]
    public void ParserCanUseRegisteredCustomAssemblyFormats()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = Parser.ParseModule("%0 = arith.constant 0 : i32", registry);

        Assert.Single(module.Operations);
        Assert.Equal("arith.constant", module.Operations[0].Name);
        Assert.True(module.Operations[0].HasCustomAssemblyBody);

        var body = Assert.IsType<PrefixConstantBodySyntax>(module.Operations[0].Body);
        Assert.Equal("0", body.Value.Text);
        Assert.Equal("i32", body.TypeSignature.GetRawText().Text);
    }

    [Fact]
    public void DocumentCanParseRegisteredCustomAssemblyFormats()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var document = Document.Parse("%0 = arith.constant 0 : i32", registry);
        var module = Binder.BindModule(document.Module, registry);

        Assert.Equal("%0 = arith.constant 0 : i32", module.ToText(ReplaceExistingSyntaxOptions()));
        Assert.Equal("0", module.Operations[0].GetAttribute("value").Value.Syntax!.GetRawText().Text);
    }

    [Fact]
    public void CustomAssemblyBodiesRoundTripExactlyThroughTheConcreteSyntaxTree()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        const string source = "%0 = arith.constant  0  :  i32\n";

        var module = Parser.ParseModule(source, registry);

        Assert.True(module.Operations[0].HasCustomAssemblyBody);
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void AssemblyBindingCanReportDiagnostics()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .RequiredAttribute("value")
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() : () -> i32"),
            registry);

        Assert.Single(module.AssemblyDiagnostics);
        Assert.Equal("arith.constant expects a 'value' required attribute.", module.AssemblyDiagnostics[0].Message);
        Assert.True(module.AssemblyDiagnostics[0].Location.IsKnown);
        Assert.Equal(1, module.AssemblyDiagnostics[0].Location.Line);
        Assert.Equal(6, module.AssemblyDiagnostics[0].Location.Column);
    }

    [Fact]
    public void AttributeAndTypeBindingCanReportDiagnostics()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [new AttributeDefinition("dense", new DenseAttributeAssemblyFormat(), factory: static context => new DenseAttributeValue(context))],
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat(), static context => new BuiltinIntegerTypeReference(context))]));

        var module = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() {value = #dense<[1, 2]>} : () -> i32"),
            registry);

        Assert.Single(module.AssemblyDiagnostics);
        Assert.Equal("dense attribute literals should mention a tensor type.", module.AssemblyDiagnostics[0].Message);
        Assert.True(module.AssemblyDiagnostics[0].Location.IsKnown);
    }

    [Fact]
    public void SemanticPrinterFallsBackToGenericAssemblyForUnknownOperations()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"test.unknown\"(%arg0) : (i32) -> i32"));

        Assert.Equal("\"test.unknown\"(%arg0) : (i32) -> i32", module.ToText());
    }

    [Fact]
    public void SemanticPrinterCanMixCustomAndGenericAssemblyWithinRegions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  %0 = \"arith.constant\"() {value = 0} : () -> i32\n" +
                "  \"func.return\"(%0) : (i32) -> ()\n" +
                "} : (i1) -> ()"),
            registry);

        Assert.Equal(
            "\"scf.if\"(%cond) {\n" +
            "  %0 = arith.constant 0 : () -> i32\n" +
            "  \"func.return\"(%0) : (i32) -> ()\n" +
            "} : (i1) -> ()",
            module.ToText(ReplaceExistingSyntaxOptions()));
    }
}
