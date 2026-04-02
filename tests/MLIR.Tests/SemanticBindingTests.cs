namespace MLIR.Tests;

using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed partial class SemanticTests
{
    [Fact]
    public void BindsRegisteredOperationsToDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("arith", [new OperationDefinition("arith.addi")]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32"),
            registry);

        var operation = module.Operations[0];
        Assert.True(operation.IsKnown);
        Assert.IsType<UnknownOperation>(operation);
        Assert.Equal("arith.addi", operation.Name);
        Assert.Equal("\"arith.addi\"", operation.Syntax?.Name);
        Assert.Equal("arith", operation.DialectName);
        Assert.NotNull(operation.Definition);
        Assert.Equal("%0", operation.Results[0].Name);
        Assert.Equal("%lhs", operation.OperandValues[0].Name);
    }

    [Fact]
    public void LeavesUnknownOperationsUnbound()
    {
        var module = Binder.BindModule(Parser.ParseModule("\"test.unknown\"() : () -> ()"));

        var operation = module.Operations[0];
        Assert.False(operation.IsKnown);
        Assert.IsType<UnknownOperation>(operation);
        Assert.Null(operation.Definition);
        Assert.Equal("test.unknown", operation.Name);
    }

    [Fact]
    public void BinderCanConstructGeneratedTypedOperations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.addi",
                        operation => operation
                            .Operand("lhs")
                            .Operand("rhs")
                            .Result("result")
                            .WithFactory(static context => new GeneratedAddIOperation(context)));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%sum = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32"),
            registry);

        var operation = Assert.IsType<GeneratedAddIOperation>(module.Operations[0]);
        Assert.Equal("%lhs", operation.LeftOperand.Name);
        Assert.Equal("%rhs", operation.RightOperand.Name);
        Assert.Equal("%sum", operation.ResultValue.Name);
    }

    [Fact]
    public void BindsNestedRegionsBlocksArgumentsAndAttributes()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
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
        Assert.Equal("1 : i32", nestedOperation.Attributes[0].Value.Syntax!.GetRawText().Text);
        Assert.Equal("i32", block.Arguments[0].TypeReference.Name);
    }

    [Fact]
    public void BindsAttributeAndTypeDefinitionsFromTheRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [new AttributeDefinition("dense", new DenseAttributeAssemblyFormat(), factory: static context => new DenseAttributeValue(context))],
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat(), static context => new BuiltinIntegerTypeReference(context))]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.op\"() {value = #dense<[1, 2]> : tensor<2xi32>} : i32", registry),
            registry);

        var operation = module.Operations[0];

        Assert.True(operation.Attributes[0].Value.IsKnown);
        Assert.Equal("dense", operation.Attributes[0].Value.Name);
        Assert.Equal("dense", Assert.IsType<DenseAttributeValue>(operation.Attributes[0].Value).Kind);
        Assert.IsType<DenseAttributeValueSyntax>(operation.Attributes[0].Value.Syntax);
        Assert.NotNull(operation.TypeSignatureReference);
        Assert.True(operation.TypeSignatureReference!.IsKnown);
        Assert.Equal("i32", operation.TypeSignatureReference.Name);
        Assert.Equal(32, Assert.IsType<BuiltinIntegerTypeReference>(operation.TypeSignatureReference).Width);
        Assert.IsType<BuiltinIntegerTypeSyntax>(operation.TypeSignatureReference.Syntax);
    }

    [Fact]
    public void OperationAssemblyFormatCanParseAttributesUsingExpectedDefinition()
    {
        var registry = new DialectRegistry();
        var i32AttributeDefinition = new AttributeDefinition("i32", new I32AttributeAssemblyFormat());
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [i32AttributeDefinition],
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat(), static context => new BuiltinIntegerTypeReference(context))]));
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
                            .WithAssemblyFormat(new ContextDirectedConstantAssemblyFormat(i32AttributeDefinition)));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = arith.constant 42 : i32", registry),
            registry);

        var operation = Assert.IsType<GeneratedConstantOperation>(module.Operations[0]);
        var value = Assert.IsType<I32AttributeValue>(operation.ValueAttribute.Value);
        Assert.Equal(42, value.Value);
        Assert.IsType<IntegerLiteralAttributeSyntax>(value.Syntax);
        Assert.Equal("%0 = arith.constant 42 : i32", module.ToText(ReplaceExistingSyntaxOptions()));
    }

    [Fact]
    public void BindsTypedSuccessorReferences()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"cf.cond_br\"(%cond) [^then, ^else] : (i1) -> ()"));

        var operation = module.Operations[0];

        Assert.Equal("^then", operation.Successors[0].Label);
        Assert.Equal("^else", operation.Successors[1].Label);
    }

    [Fact]
    public void BindsSuccessorsToBlockInstances()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"func.func\"() {\n" +
                "^bb0:\n" +
                "  \"cf.br\"() [^bb1] : () -> ()\n" +
                "^bb1:\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : () -> ()"));

        var region = module.Operations[0].Regions[0];
        var branchOp = region.Blocks[0].Operations[0];
        var bb1 = region.Blocks[1];

        Assert.Same(bb1, branchOp.Successors[0].Block);
        Assert.Equal("^bb1", branchOp.Successors[0].Label);
    }

    [Fact]
    public void BlockTracksItsSuccessorUses()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"func.func\"() {\n" +
                "^bb0:\n" +
                "  \"cf.br\"() [^bb1] : () -> ()\n" +
                "^bb1:\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : () -> ()"));

        var region = module.Operations[0].Regions[0];
        var branchOp = region.Blocks[0].Operations[0];
        var bb1 = region.Blocks[1];

        Assert.Single(bb1.Uses);
        Assert.Same(branchOp.Successors[0], bb1.Uses[0]);
    }

    [Fact]
    public void DocumentBindUsesTheDialectRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("func", [new OperationDefinition("func.return")]));

        var module = Document.Parse("\"func.return\"() : () -> ()").Bind(registry);

        Assert.True(module.Operations[0].IsKnown);
        Assert.Equal("func.return", module.Operations[0].Name);
    }

    [Fact]
    public void OperationCanCheckForAttributesByName()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var operation = module.Operations[0];

        Assert.True(operation.HasAttribute("value"));
        Assert.False(operation.HasAttribute("fastmath"));
    }

    [Fact]
    public void OperationCanRetrieveAttributesByName()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var attribute = module.Operations[0].GetAttribute("value");

        Assert.Equal("value", attribute.Name);
        Assert.Equal("0 : i32", attribute.Value.Syntax!.GetRawText().Text);
    }

    [Fact]
    public void OperationViewProvidesTypedWrapperOverSemanticOperation()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(CreateArithConstantDialect());

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"),
            registry);

        var view = new ArithConstantView(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]));

        Assert.Equal("%0", view.Results[0]);
        Assert.Equal("%0", view.ResultValue.Name);
        Assert.Equal("0 : i32", view.ValueAttribute.Value.Syntax!.GetRawText().Text);
    }

    [Fact]
    public void OperationViewRejectsUnexpectedOperationNames()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"func.return\"() : () -> ()"));

        var exception = Assert.Throws<ArgumentException>(() => new ArithConstantView(module.Operations[0]));

        Assert.Contains("arith.constant", exception.Message);
        Assert.Contains("func.return", exception.Message);
    }

    [Fact]
    public void SemanticReferencesExposeSourceLocations()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) [^bb1] : (i32, i32) -> i32"));

        var operation = module.Operations[0];

        Assert.Equal(1, operation.Location.Line);
        Assert.Equal(6, operation.Location.Column);
        Assert.Equal(1, operation.Results[0].Location.Line);
        Assert.Equal(1, operation.Results[0].Location.Column);
        Assert.Equal(1, operation.OperandValues[0].Location.Line);
        Assert.Equal(19, operation.OperandValues[0].Location.Column);
        Assert.Equal(1, operation.Successors[0].Location.Line);
        Assert.Equal(32, operation.Successors[0].Location.Column);
    }
}
