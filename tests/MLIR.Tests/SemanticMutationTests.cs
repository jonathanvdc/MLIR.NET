namespace MLIR.Tests;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed partial class SemanticTests
{
    [Fact]
    public void SyntheticBlockHasNullSyntaxAndUnknownLocation()
    {
        var syntheticBlock = new Block("^entry", [], []);

        Assert.Null(syntheticBlock.Syntax);
        Assert.Equal("^entry", syntheticBlock.Label);
        Assert.False(syntheticBlock.Location.IsKnown);
    }

    [Fact]
    public void SyntheticRegionHasNullSyntax()
    {
        var syntheticRegion = new Region(null, []);

        Assert.Null(syntheticRegion.Syntax);
    }

    [Fact]
    public void SyntheticOperationHasNullSyntaxAndUnknownLocation()
    {
        var syntheticOperation = new SyntheticOperation("test.synthetic");

        Assert.Null(syntheticOperation.Syntax);
        Assert.False(syntheticOperation.Location.IsKnown);
        Assert.Equal("test.synthetic", syntheticOperation.Name);
        Assert.Equal("test", syntheticOperation.DialectName);
    }

    [Fact]
    public void SyntheticAttributeValueHasNullSyntax()
    {
        var syntheticAttribute = new SyntheticAttributeValue("test");

        Assert.Null(syntheticAttribute.Syntax);
        Assert.Equal("test", syntheticAttribute.Name);
        Assert.False(syntheticAttribute.Location.IsKnown);
    }

    [Fact]
    public void ReplaceAllUsesWithRetargetsOperandUses()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "%0 = \"test.left\"() : () -> i32\n" +
                "%1 = \"test.right\"() : () -> i32\n" +
                "%2 = \"test.consumer\"(%0) : (i32) -> i32"));

        var originalValue = module.Operations[0].Results[0];
        var replacementValue = module.Operations[1].Results[0];
        var consumer = module.Operations[2];

        Assert.Single(originalValue.Uses);
        Assert.Empty(replacementValue.Uses);
        Assert.Same(originalValue, consumer.Operands[0].Value);

        originalValue.ReplaceAllUsesWith(replacementValue);

        Assert.Empty(originalValue.Uses);
        Assert.Single(replacementValue.Uses);
        Assert.Same(replacementValue, consumer.Operands[0].Value);
        Assert.Same(consumer, replacementValue.Uses[0].Owner);
    }

    [Fact]
    public void SetOperandInvalidatesSyntaxUpward()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  ^bb0(%arg0: i32, %arg1: i32):\n" +
                "    \"func.return\"(%arg0) : (i32) -> ()\n" +
                "} : (i1) -> ()"));

        var outerOperation = module.Operations[0];
        var region = outerOperation.Regions[0];
        var block = region.Blocks[0];
        var terminator = block.Operations[0];

        Assert.NotNull(outerOperation.Syntax);
        Assert.NotNull(region.Syntax);
        Assert.NotNull(block.Syntax);
        Assert.NotNull(terminator.Syntax);

        terminator.SetOperand(0, block.Arguments[1]);

        Assert.Null(terminator.Syntax);
        Assert.Null(block.Syntax);
        Assert.Null(region.Syntax);
        Assert.Null(outerOperation.Syntax);
        Assert.Same(block.Arguments[1], terminator.Operands[0].Value);
    }

    [Fact]
    public void MutableContainersWireParentsAndIndices()
    {
        var region = new Region(null, []);
        var block = new Block("^entry", [], []);
        var typeSyntax = new RawTypeSyntax(new RawSyntaxText("i32"));
        var argument = new BlockArgument(
            new BlockArgumentSyntax(new SyntaxToken("%arg0"), new SyntaxToken(":"), typeSyntax),
            new UnknownTypeReference(typeSyntax, "i32", null, SourceLocation.Unknown));
        var operation = new UnknownOperation(
            new OperationSyntax([], "\"test.op\"", [], [], [], [], null),
            "test.op",
            null,
            [],
            NamedAttributeCollection.Empty,
            null,
            [new OperationResult("%0")],
            [],
            []);

        region.AddBlock(block);
        block.AddArgument(argument);
        block.AddOperation(operation);

        Assert.Same(region, block.ParentRegion);
        Assert.Same(block, argument.Owner);
        Assert.Equal(0, argument.Index);
        Assert.Same(block, operation.ParentBlock);
        Assert.Same(operation, operation.Results[0].DefiningOperation);
        Assert.Equal(0, operation.Results[0].ResultIndex);
        Assert.Null(region.Syntax);
        Assert.Null(block.Syntax);
    }

    [Fact]
    public void ToTextReflectsOperandMutationAfterSyntaxInvalidation()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  ^bb0(%arg0: i32, %arg1: i32):\n" +
                "    \"func.return\"(%arg0) : (i32) -> ()\n" +
                "} : (i1) -> ()"));

        var terminator = module.Operations[0].Regions[0].Blocks[0].Operations[0];
        var replacementArgument = module.Operations[0].Regions[0].Blocks[0].Arguments[1];

        terminator.SetOperand(0, replacementArgument);

        Assert.Equal(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32, %arg1: i32):\n" +
            "    \"func.return\"(%arg1) : (i32) -> ()\n" +
            "} : (i1) -> ()",
            module.ToText());
    }

    [Fact]
    public void ToTextReflectsReplaceAllUsesWithAfterMutation()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "%0 = \"test.left\"() : () -> i32\n" +
                "%1 = \"test.right\"() : () -> i32\n" +
                "%2 = \"test.consumer\"(%0) : (i32) -> i32"));

        module.Operations[0].Results[0].ReplaceAllUsesWith(module.Operations[1].Results[0]);

        Assert.Equal(
            "%0 = \"test.left\"() : () -> i32\n" +
            "%1 = \"test.right\"() : () -> i32\n" +
            "%2 = \"test.consumer\"(%1) : (i32) -> i32",
            module.ToText());
    }

    [Fact]
    public void ToTextPrintsSyntheticInsertionsAfterMutation()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"outer.op\"() : () -> ()"));

        var newRegion = new Region(null, []);
        var newBlock = new Block("^entry", [], []);
        var newOperation = new UnknownOperation(
            new OperationSyntax([], "\"inserted.op\"", [], [], [], [], null),
            "inserted.op",
            null,
            [],
            NamedAttributeCollection.Empty,
            null,
            [],
            [],
            []);

        newRegion.AddBlock(newBlock);
        newBlock.AddOperation(newOperation);
        module.Operations[0].AddRegion(newRegion);

        Assert.Equal(
            "\"outer.op\"() {\n" +
            "  \"inserted.op\"()\n" +
            "} : () -> ()",
            module.ToText());
    }

    [Fact]
    public void AddOperationUniquifiesConflictingResultNames()
    {
        var block = new Block("^entry", [], []);
        block.AddOperation(new UnknownOperation(
            new OperationSyntax([], "\"test.first\"", [], [], [], [], null),
            "test.first",
            null,
            [],
            NamedAttributeCollection.Empty,
            null,
            [new OperationResult("%value")],
            [],
            []));
        var duplicate = new UnknownOperation(
            new OperationSyntax([], "\"test.second\"", [], [], [], [], null),
            "test.second",
            null,
            [],
            NamedAttributeCollection.Empty,
            null,
            [new OperationResult("%value")],
            [],
            []);

        block.AddOperation(duplicate);

        Assert.Equal("%value", block.Operations[0].Results[0].Name);
        Assert.Equal("%value_1", duplicate.Results[0].Name);
    }

    [Fact]
    public void RenameUniquifiesConflictingNamesWithinABlock()
    {
        var block = new Block("^entry", [], []);
        var first = new BlockArgument(new BlockArgumentSyntax("%arg0", new RawSyntaxText("i32")), new UnknownTypeReference(new RawTypeSyntax(new RawSyntaxText("i32")), "i32", null, SourceLocation.Unknown));
        var second = new BlockArgument(new BlockArgumentSyntax("%arg1", new RawSyntaxText("i32")), new UnknownTypeReference(new RawTypeSyntax(new RawSyntaxText("i32")), "i32", null, SourceLocation.Unknown));

        block.AddArgument(first);
        block.AddArgument(second);

        var renamed = second.Rename("%arg0");

        Assert.Equal("%arg0_1", renamed);
        Assert.Equal("%arg0", first.Name);
        Assert.Equal("%arg0_1", second.Name);
    }

    [Fact]
    public void RenameCanRejectConflictingNamesWhenUniquifyIsDisabled()
    {
        var block = new Block("^entry", [], []);
        var first = new BlockArgument(new BlockArgumentSyntax("%arg0", new RawSyntaxText("i32")), new UnknownTypeReference(new RawTypeSyntax(new RawSyntaxText("i32")), "i32", null, SourceLocation.Unknown));
        var second = new BlockArgument(new BlockArgumentSyntax("%arg1", new RawSyntaxText("i32")), new UnknownTypeReference(new RawTypeSyntax(new RawSyntaxText("i32")), "i32", null, SourceLocation.Unknown));

        block.AddArgument(first);
        block.AddArgument(second);

        var exception = Assert.Throws<InvalidOperationException>(() => second.Rename("%arg0", uniquify: false));

        Assert.Contains("%arg0", exception.Message);
        Assert.Equal("%arg1", second.Name);
    }

    [Fact]
    public void AddBlockRejectsConflictingLabels()
    {
        var region = new Region(null, []);
        var first = new Block("^bb0", [], []);
        var duplicate = new Block("^bb0", [], []);

        region.AddBlock(first);

        var exception = Assert.Throws<InvalidOperationException>(() => region.AddBlock(duplicate));

        Assert.Contains("^bb0", exception.Message);
        Assert.Single(region.Blocks);
        Assert.Same(first, region.Blocks[0]);
    }

    [Fact]
    public void ToTextUsesUniquifiedNamesForConflictingInsertedDefinitions()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  ^bb0:\n" +
                "    %value = \"test.left\"() : () -> i32\n" +
                "  } : (i1) -> ()"));

        var block = module.Operations[0].Regions[0].Blocks[0];
        block.AddOperation(new UnknownOperation(
            new OperationSyntax([], "\"test.right\"", [], [], [], [], null),
            "test.right",
            null,
            [],
            NamedAttributeCollection.Empty,
            null,
            [new OperationResult("%value")],
            [],
            []));

        Assert.Equal(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    %value = \"test.left\"() : () -> i32\n" +
            "    %value_1 = \"test.right\"()\n" +
            "} : (i1) -> ()",
            module.ToText());
    }
}
