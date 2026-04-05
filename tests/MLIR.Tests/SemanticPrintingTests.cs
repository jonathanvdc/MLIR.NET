namespace MLIR.Tests;

using System;
using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Text;
using MLIR.Syntax.Attributes.Primitives;
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
    public void SemanticPrinterRebuildsF32AttributesFromTheirNumericValue()
    {
        var f32AttributeDefinition = new AttributeDefinition("f32", new F32AttributeAssemblyFormat(), factory: static context => new TestF32AttributeValue(context));
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
                                .WithFactory(static context => new GeneratedConstantOperation(context))
                                .WithAssemblyFormat(new ContextDirectedConstantAssemblyFormat(f32AttributeDefinition));
                        });
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = arith.constant 1.500 : f32", registry),
            registry);

        Assert.Equal("%0 = arith.constant 1.5 : f32", module.ToText(ReplaceExistingSyntaxOptions()));
    }

    [Theory]
    [InlineData("+1.500")]
    public void SemanticPrinterNormalizesAndRoundTripsF32Attributes(string sourceValue)
    {
        var f32AttributeDefinition = new AttributeDefinition("f32", new F32AttributeAssemblyFormat(), factory: static context => new TestF32AttributeValue(context));
        var registry = CreateFloatingPointConstantRegistry(f32AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f32", registry),
            registry);

        var printed = module.ToText(ReplaceExistingSyntaxOptions());

        var parsedValue = Assert.IsType<TestF32AttributeValue>(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]).ValueAttribute.Value).Value;
        Assert.Equal($"%0 = arith.constant {FloatingPointLiteralParser.FormatSingle(parsedValue)} : f32", printed);

        var rebound = Binder.BindModule(Parser.ParseModule(printed, registry), registry);
        Assert.Equal(
            FloatingPointLiteralParser.FormatSingle(parsedValue),
            Assert.IsType<TestF32AttributeValue>(Assert.IsType<GeneratedConstantOperation>(rebound.Operations[0]).ValueAttribute.Value)
                .Value
                .ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("+2.5000")]
    [InlineData("-3.125e200")]
    public void SemanticPrinterNormalizesAndRoundTripsF64Attributes(string sourceValue)
    {
        var f64AttributeDefinition = new AttributeDefinition("f64", new F64AttributeAssemblyFormat(), factory: static context => new TestF64AttributeValue(context));
        var registry = CreateFloatingPointConstantRegistry(f64AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f64", registry),
            registry);

        var printed = module.ToText(ReplaceExistingSyntaxOptions());

        var parsedValue = Assert.IsType<TestF64AttributeValue>(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]).ValueAttribute.Value).Value;
        Assert.Equal($"%0 = arith.constant {FloatingPointLiteralParser.FormatDouble(parsedValue)} : f64", printed);

        var rebound = Binder.BindModule(Parser.ParseModule(printed, registry), registry);
        Assert.Equal(
            FloatingPointLiteralParser.FormatDouble(parsedValue),
            Assert.IsType<TestF64AttributeValue>(Assert.IsType<GeneratedConstantOperation>(rebound.Operations[0]).ValueAttribute.Value)
                .Value
                .ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("%0 = arith.constant 0x3f800000 : f32")]
    [InlineData("%0 = arith.constant inf : f32")]
    [InlineData("%0 = arith.constant nan : f32")]
    [InlineData("%0 = arith.constant 0x3ff0000000000000 : f64")]
    [InlineData("%0 = arith.constant -inf : f64")]
    [InlineData("%0 = arith.constant nan : f64")]
    public void ParserPreservesCustomFloatAssemblyExactly(string source)
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
                            .WithAssemblyFormat(source.Contains(": f32", StringComparison.Ordinal) || source.Contains("f32", StringComparison.Ordinal)
                                ? new ContextDirectedConstantAssemblyFormat(new AttributeDefinition("f32", new F32AttributeAssemblyFormat(), factory: static context => new TestF32AttributeValue(context)))
                                : new ContextDirectedConstantAssemblyFormat(new AttributeDefinition("f64", new F64AttributeAssemblyFormat(), factory: static context => new TestF64AttributeValue(context)))));
                }));

        var module = Parser.ParseModule(source, registry);

        Assert.Equal(source, Printer.Print(module));
    }

    [Theory]
    [InlineData("0x3f800000", "1.0")]
    [InlineData("0x7f800000", "inf")]
    [InlineData("nan", "nan")]
    public void SemanticPrinterCanonicalizesF32HexAndSpecialForms(string sourceValue, string normalizedValue)
    {
        var f32AttributeDefinition = new AttributeDefinition("f32", new F32AttributeAssemblyFormat(), factory: static context => new TestF32AttributeValue(context));
        var registry = CreateFloatingPointConstantRegistry(f32AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f32", registry),
            registry);

        Assert.Equal($"%0 = arith.constant {normalizedValue} : f32", module.ToText(ReplaceExistingSyntaxOptions()));
    }

    [Theory]
    [InlineData("0x3ff0000000000000", "1.0")]
    [InlineData("0x7ff0000000000000", "inf")]
    [InlineData("nan", "nan")]
    public void SemanticPrinterCanonicalizesF64HexAndSpecialForms(string sourceValue, string normalizedValue)
    {
        var f64AttributeDefinition = new AttributeDefinition("f64", new F64AttributeAssemblyFormat(), factory: static context => new TestF64AttributeValue(context));
        var registry = CreateFloatingPointConstantRegistry(f64AttributeDefinition);

        var module = Binder.BindModule(
            Parser.ParseModule($"%0 = arith.constant {sourceValue} : f64", registry),
            registry);

        Assert.Equal($"%0 = arith.constant {normalizedValue} : f64", module.ToText(ReplaceExistingSyntaxOptions()));
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
        Assert.Equal("0", body.Value.ToString());
        Assert.Equal("i32", body.TypeSignature.ToString());
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
        Assert.Equal("0", module.Operations[0].GetAttribute("value").Value.Syntax!.ToString());
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
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat(), static context => new IntegerTypeReference(context))]));

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
