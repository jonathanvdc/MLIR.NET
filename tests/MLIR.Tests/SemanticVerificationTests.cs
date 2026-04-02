namespace MLIR.Tests;

using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Text;
using Xunit;

public sealed partial class SemanticTests
{
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
                        operandDefinitions:
                        [
                            new OperationSegmentDefinition("lhs"),
                            new OperationSegmentDefinition("rhs"),
                        ],
                        verifier: new DelegateOperationVerifier(
                            static (operation, context) =>
                            {
                                if (operation.Operands.Count != 2)
                                {
                                    context.Report("arith.addi expects exactly two operands.");
                                }
                            })),
                ]));

        var module = Binder.BindModule(Parser.ParseModule("\"arith.addi\"(%lhs) : (i32) -> i32"), registry);
        var result = Verifier.Verify(module);

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
                        resultDefinitions: [new OperationSegmentDefinition("result")]),
                    new OperationDefinition(
                        "arith.addi",
                        operandDefinitions:
                        [
                            new OperationSegmentDefinition("lhs"),
                            new OperationSegmentDefinition("rhs"),
                        ],
                        resultDefinitions: [new OperationSegmentDefinition("result")]),
                ]));

        var module = Binder.BindModule(
            Parser.ParseModule("\"arith.addi\"(%lhs) : (i32) -> i32"),
            registry);

        var result = Verifier.Verify(module);

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
                        resultDefinitions: [new OperationSegmentDefinition("result")],
                        requiredAttributes: ["value"]),
                ]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() : () -> i32"),
            registry);

        var result = Verifier.Verify(module);

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

        var validModule = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.concat\"(%a, %b, %c) : (i32, i32, i32) -> i32"),
            registry);
        var invalidModule = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.concat\"() : () -> i32"),
            registry);

        Assert.True(Verifier.Verify(validModule).IsSuccess);

        var invalidResult = Verifier.Verify(invalidModule);
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

        var missingRegionModule = Binder.BindModule(
            Parser.ParseModule("\"test.branching\"() [^bb0] : () -> ()"),
            registry);
        var missingSuccessorModule = Binder.BindModule(
            Parser.ParseModule("\"test.branching\"() {} : () -> ()"),
            registry);

        var missingRegionResult = Verifier.Verify(missingRegionModule);
        var missingSuccessorResult = Verifier.Verify(missingSuccessorModule);

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
                            new OperationAttributeDefinition("required"),
                            new OperationAttributeDefinition("optional", isRequired: false),
                        ]),
                ]));

        var validModule = Binder.BindModule(
            Parser.ParseModule("\"test.attrs\"() {required = 1 : i32} : () -> ()"),
            registry);
        var invalidModule = Binder.BindModule(
            Parser.ParseModule("\"test.attrs\"() {optional = 1 : i32} : () -> ()"),
            registry);

        Assert.True(Verifier.Verify(validModule).IsSuccess);

        var invalidResult = Verifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Single(invalidResult.Diagnostics);
        Assert.Equal("'test.attrs' requires the 'required' attribute.", invalidResult.Diagnostics[0].Message);
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
                                .WithFactory(static context => new GeneratedConstantOperation(context))
                                .WithVerifier(static (semanticOperation, context) =>
                                {
                                    if (semanticOperation.Results.Count != 1)
                                    {
                                        context.Report("arith.constant should define a single result.");
                                    }
                                });
                        });
                }));

        var validModule = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"),
            registry);
        var invalidModule = Binder.BindModule(
            Parser.ParseModule("\"arith.constant\"() : () -> i32"),
            registry);

        Assert.True(validModule.Operations[0].IsKnown);

        var invalidResult = Verifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'arith.constant' expects exactly 1 result but found 0.");
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'arith.constant' requires the 'value' attribute.");
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "arith.constant should define a single result.");
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

        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : (i1) -> ()"),
            registry);

        var result = Verifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Diagnostics);
        Assert.Equal("func.return", result.Diagnostics[0].Operation.Name);
        Assert.True(result.Diagnostics[0].Location.IsKnown);
        Assert.Equal(2, result.Diagnostics[0].Location.Line);
        Assert.Equal(3, result.Diagnostics[0].Location.Column);
    }
}
