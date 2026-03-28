namespace MLIR.Tests;

using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Text;
using Xunit;

public sealed class MlirSemanticTests
{
    private sealed class ArithConstantView : OperationView
    {
        public ArithConstantView(Operation operation)
            : base(operation, "arith.constant")
        {
        }

        public NamedAttribute ValueAttribute => GetAttribute("value");
        public string ParsedValueText => GetProperty<string>("parsed.value");
        public ValueReference ResultValue => ResultValues[0];
    }

    private sealed class PrefixConstantAssemblyFormat : IOperationAssemblyFormat
    {
        public void Bind(Operation operation, OperationAssemblyBindingContext context)
        {
            if (!operation.HasAttribute("value"))
            {
                context.Report("arith.constant custom assembly expects a 'value' attribute.");
                return;
            }

            context.SetProperty("parsed.value", operation.GetAttribute("value").Value.Text);
            if (operation.TypeSignature != null)
            {
                context.SetProperty("parsed.type", operation.TypeSignature.Text);
            }
        }

        public void Print(Operation operation, OperationPrintingContext context)
        {
            context.WriteOperationPrefix();

            if (operation.Results.Count > 0)
            {
                context.Write(string.Join(", ", operation.Results));
                context.Write(" = ");
            }

            context.Write(operation.Name);
            if (operation.Attributes.Count > 0)
            {
                context.Write(" ");
                context.Write(operation.Attributes[0].Value.Text);
            }

            if (operation.TypeSignature != null)
            {
                context.Write(" : ");
                context.Write(operation.TypeSignature.Text);
            }
        }
    }

    [Fact]
    public void BindsRegisteredOperationsToDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("arith", [new OperationDefinition("arith.addi")]));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32"),
            registry);

        var operation = module.Operations[0];
        Assert.True(operation.IsKnown);
        Assert.Equal("arith.addi", operation.Name);
        Assert.Equal("\"arith.addi\"", operation.SyntaxName);
        Assert.Equal("arith", operation.DialectName);
        Assert.NotNull(operation.Definition);
        Assert.Equal("%0", operation.ResultValues[0].Name);
        Assert.Equal("%lhs", operation.OperandValues[0].Name);
    }

    [Fact]
    public void LeavesUnknownOperationsUnbound()
    {
        var module = MlirBinder.BindModule(MlirParser.ParseModule("\"test.unknown\"() : () -> ()"));

        var operation = module.Operations[0];
        Assert.False(operation.IsKnown);
        Assert.Null(operation.Definition);
        Assert.Equal("test.unknown", operation.Name);
    }

    [Fact]
    public void BindsNestedRegionsBlocksArgumentsAndAttributes()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  ^bb0(%arg0: i32):\n" +
                "    \"func.return\"(%arg0) {value = 1 : i32} : (i32) -> ()\n" +
                "} : (i1) -> ()"));

        var region = module.Operations[0].Regions[0];
        var block = region.Blocks[0];
        var nestedOperation = block.Operations[0];

        Assert.Single(module.Operations);
        Assert.Equal("^bb0", block.Label);
        Assert.Single(block.Arguments);
        Assert.Equal("%arg0", block.Arguments[0].Name);
        Assert.Equal("i32", block.Arguments[0].Type.Text);
        Assert.Single(nestedOperation.Attributes);
        Assert.Equal("value", nestedOperation.Attributes[0].Name);
        Assert.Equal("1 : i32", nestedOperation.Attributes[0].Value.Text);
        Assert.Equal("%arg0", block.Arguments[0].Value.Name);
    }

    [Fact]
    public void BindsTypedSuccessorReferences()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("\"cf.cond_br\"(%cond) [^then, ^else] : (i1) -> ()"));

        var operation = module.Operations[0];

        Assert.Equal("^then", operation.SuccessorReferences[0].Label);
        Assert.Equal("^else", operation.SuccessorReferences[1].Label);
    }

    [Fact]
    public void DocumentBindUsesTheDialectRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("func", [new OperationDefinition("func.return")]));

        var module = MlirDocument.Parse("\"func.return\"() : () -> ()").Bind(registry);

        Assert.True(module.Operations[0].IsKnown);
        Assert.Equal("func.return", module.Operations[0].Name);
    }

    [Fact]
    public void VerifierReportsDiagnosticsFromRegisteredDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "arith",
                [
                    new OperationDefinition(
                        "arith.addi",
                        operandCount: 2,
                        verifier: new DelegateOperationVerifier(
                            static (operation, context) =>
                            {
                                if (operation.Operands.Count != 2)
                                {
                                    context.Report("arith.addi expects exactly two operands.");
                                }
                            })),
                ]));

        var module = MlirBinder.BindModule(MlirParser.ParseModule("\"arith.addi\"(%lhs) : (i32) -> i32"), registry);
        var result = MlirVerifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Diagnostics.Count);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "'arith.addi' expects exactly 2 operands but found 1.");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "arith.addi expects exactly two operands.");
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("arith.addi", diagnostic.Operation.Name));
    }

    [Fact]
    public void VerifierReportsStructuralOperandAndResultConstraints()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "arith",
                [
                    new OperationDefinition(
                        "arith.constant",
                        resultCount: 1),
                    new OperationDefinition(
                        "arith.addi",
                        operandCount: 2,
                        resultCount: 1),
                ]));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("\"arith.addi\"(%lhs) : (i32) -> i32"),
            registry);

        var result = MlirVerifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "'arith.addi' expects exactly 2 operands but found 1.");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "'arith.addi' expects exactly 1 result but found 0.");
    }

    [Fact]
    public void VerifierReportsMissingRequiredAttributes()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "arith",
                [
                    new OperationDefinition(
                        "arith.constant",
                        resultCount: 1,
                        requiredAttributes: ["value"]),
                ]));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() : () -> i32"),
            registry);

        var result = MlirVerifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Diagnostics);
        Assert.Equal("'arith.constant' requires the 'value' attribute.", result.Diagnostics[0].Message);
    }

    [Fact]
    public void VerifierSupportsVariadicOperandDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "test",
                [
                    new OperationDefinition(
                        "test.concat",
                        operandDefinitions:
                        [
                            new OperationSegmentDefinition("head"),
                            new OperationSegmentDefinition("tail", isVariadic: true),
                        ],
                        resultDefinitions: [new OperationSegmentDefinition("result")]),
                ]));

        var validModule = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"test.concat\"(%a, %b, %c) : (i32, i32, i32) -> i32"),
            registry);
        var invalidModule = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"test.concat\"() : () -> i32"),
            registry);

        Assert.True(MlirVerifier.Verify(validModule).IsSuccess);

        var invalidResult = MlirVerifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'test.concat' expects at least 1 operand but found 0.");
    }

    [Fact]
    public void VerifierSupportsRegionAndSuccessorDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "test",
                [
                    new OperationDefinition(
                        "test.branching",
                        regionDefinitions: [new OperationSegmentDefinition("body")],
                        successorDefinitions:
                        [
                            new OperationSegmentDefinition("primary"),
                            new OperationSegmentDefinition("extra", isVariadic: true),
                        ]),
                ]));

        var missingRegionModule = MlirBinder.BindModule(
            MlirParser.ParseModule("\"test.branching\"() [^bb0] : () -> ()"),
            registry);
        var missingSuccessorModule = MlirBinder.BindModule(
            MlirParser.ParseModule("\"test.branching\"() {} : () -> ()"),
            registry);

        var missingRegionResult = MlirVerifier.Verify(missingRegionModule);
        var missingSuccessorResult = MlirVerifier.Verify(missingSuccessorModule);

        Assert.Contains(missingRegionResult.Diagnostics, diagnostic => diagnostic.Message == "'test.branching' expects exactly 1 region but found 0.");
        Assert.Contains(missingSuccessorResult.Diagnostics, diagnostic => diagnostic.Message == "'test.branching' expects at least 1 successor but found 0.");
    }

    [Fact]
    public void VerifierSupportsOptionalAndRequiredAttributeDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "test",
                [
                    new OperationDefinition(
                        "test.attrs",
                        attributeDefinitions:
                        [
                            new AttributeDefinition("required"),
                            new AttributeDefinition("optional", isRequired: false),
                        ]),
                ]));

        var validModule = MlirBinder.BindModule(
            MlirParser.ParseModule("\"test.attrs\"() {required = 1 : i32} : () -> ()"),
            registry);
        var invalidModule = MlirBinder.BindModule(
            MlirParser.ParseModule("\"test.attrs\"() {optional = 1 : i32} : () -> ()"),
            registry);

        Assert.True(MlirVerifier.Verify(validModule).IsSuccess);

        var invalidResult = MlirVerifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Single(invalidResult.Diagnostics);
        Assert.Equal("'test.attrs' requires the 'required' attribute.", invalidResult.Diagnostics[0].Message);
    }

    [Fact]
    public void OperationCanCheckForAttributesByName()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var operation = module.Operations[0];

        Assert.True(operation.HasAttribute("value"));
        Assert.False(operation.HasAttribute("fastmath"));
    }

    [Fact]
    public void OperationCanRetrieveAttributesByName()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var attribute = module.Operations[0].GetAttribute("value");

        Assert.Equal("value", attribute.Name);
        Assert.Equal("0 : i32", attribute.Value.Text);
    }

    [Fact]
    public void OperationViewProvidesTypedWrapperOverSemanticOperation()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var view = new ArithConstantView(module.Operations[0]);

        Assert.Equal("%0", view.Results[0]);
        Assert.Equal("%0", view.ResultValue.Name);
        Assert.Equal("0 : i32", view.ValueAttribute.Value.Text);
        Assert.False(view.HasProperty("missing"));
    }

    [Fact]
    public void OperationViewRejectsUnexpectedOperationNames()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("\"func.return\"() : () -> ()"));

        var exception = Assert.Throws<ArgumentException>(() => new ArithConstantView(module.Operations[0]));

        Assert.Contains("arith.constant", exception.Message);
        Assert.Contains("func.return", exception.Message);
    }

    [Fact]
    public void DialectBuilderCreatesDefinitionsFluently()
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
                                .WithVerifier(static (semanticOperation, context) =>
                                {
                                    if (semanticOperation.Results.Count != 1)
                                    {
                                        context.Report("arith.constant should define a single result.");
                                    }
                                });
                        });
                }));

        var validModule = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"),
            registry);
        var invalidModule = MlirBinder.BindModule(
            MlirParser.ParseModule("\"arith.constant\"() : () -> i32"),
            registry);

        Assert.True(validModule.Operations[0].IsKnown);

        var invalidResult = MlirVerifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'arith.constant' expects exactly 1 result but found 0.");
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'arith.constant' requires the 'value' attribute.");
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "arith.constant should define a single result.");
    }

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
                                .WithAssemblyFormat(new PrefixConstantAssemblyFormat());
                        });
                }));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() {value = 0} : () -> i32"),
            registry);

        Assert.Equal("%0 = arith.constant 0 : () -> i32", module.ToText());
    }

    [Fact]
    public void AssemblyBindingCanPopulateTypedSemanticProperties()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation.WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"),
            registry);
        var view = new ArithConstantView(module.Operations[0]);

        Assert.True(module.Operations[0].HasProperty("parsed.value"));
        Assert.Equal("0 : i32", view.ParsedValueText);
        Assert.Equal("() -> i32", module.Operations[0].GetProperty<string>("parsed.type"));
        Assert.Empty(module.AssemblyDiagnostics);
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
                        operation => operation.WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.constant\"() : () -> i32"),
            registry);

        Assert.Single(module.AssemblyDiagnostics);
        Assert.Equal("arith.constant custom assembly expects a 'value' attribute.", module.AssemblyDiagnostics[0].Message);
        Assert.Equal("arith.constant", module.AssemblyDiagnostics[0].Operation.Name);
        Assert.True(module.AssemblyDiagnostics[0].Location.IsKnown);
        Assert.Equal(1, module.AssemblyDiagnostics[0].Location.Line);
        Assert.Equal(6, module.AssemblyDiagnostics[0].Location.Column);
    }

    [Fact]
    public void SemanticPrinterFallsBackToGenericAssemblyForUnknownOperations()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("\"test.unknown\"(%arg0) : (i32) -> i32"));

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
                        operation => operation.WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule(
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
            module.ToText());
    }

    [Fact]
    public void VerifierWalksNestedOperations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "func",
                [
                    new OperationDefinition(
                        "func.return",
                        verifier: new DelegateOperationVerifier(
                            static (operation, context) =>
                            {
                                if (operation.Operands.Count == 0)
                                {
                                    context.Report("func.return expects a value in this test dialect.");
                                }
                            })),
                ]));

        var module = MlirBinder.BindModule(
            MlirParser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : (i1) -> ()"),
            registry);

        var result = MlirVerifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Diagnostics);
        Assert.Equal("func.return", result.Diagnostics[0].Operation.Name);
        Assert.True(result.Diagnostics[0].Location.IsKnown);
        Assert.Equal(2, result.Diagnostics[0].Location.Line);
        Assert.Equal(3, result.Diagnostics[0].Location.Column);
    }

    [Fact]
    public void SemanticReferencesExposeSourceLocations()
    {
        var module = MlirBinder.BindModule(
            MlirParser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) [^bb1] : (i32, i32) -> i32"));

        var operation = module.Operations[0];

        Assert.Equal(1, operation.Location.Line);
        Assert.Equal(6, operation.Location.Column);
        Assert.Equal(1, operation.ResultValues[0].Location.Line);
        Assert.Equal(1, operation.ResultValues[0].Location.Column);
        Assert.Equal(1, operation.OperandValues[0].Location.Line);
        Assert.Equal(19, operation.OperandValues[0].Location.Column);
        Assert.Equal(1, operation.SuccessorReferences[0].Location.Line);
        Assert.Equal(32, operation.SuccessorReferences[0].Location.Column);
    }

    [Fact]
    public void RegistryRejectsDuplicateOperationRegistrations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("arith", [new OperationDefinition("arith.addi")]));

        var exception = Assert.Throws<ArgumentException>(
            () => registry.RegisterDialect(new Dialect("arithx", [new OperationDefinition("arith.addi")])));

        Assert.Contains("already registered", exception.Message);
    }
}
