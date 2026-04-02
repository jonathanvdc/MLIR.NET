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
        var syntheticBlock = new Block(new BlockReference("^entry"), [], []);

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
        Assert.Null(syntheticOperation.SyntaxName);
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

        var originalValue = module.Operations[0].ResultValues[0];
        var replacementValue = module.Operations[1].ResultValues[0];
        var consumer = module.Operations[2];

        Assert.Single(originalValue.Uses);
        Assert.Empty(replacementValue.Uses);
        Assert.Same(originalValue, consumer.OperandUses[0].Value);

        originalValue.ReplaceAllUsesWith(replacementValue);

        Assert.Empty(originalValue.Uses);
        Assert.Single(replacementValue.Uses);
        Assert.Same(replacementValue, consumer.OperandUses[0].Value);
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
        Assert.Same(block.Arguments[1], terminator.OperandUses[0].Value);
    }

    [Fact]
    public void MutableContainersWireParentsAndIndices()
    {
        var region = new Region(null, []);
        var block = new Block(new BlockReference("^entry"), [], []);
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
        Assert.Same(operation, operation.ResultValues[0].DefiningOperation);
        Assert.Equal(0, operation.ResultValues[0].ResultIndex);
        Assert.Null(region.Syntax);
        Assert.Null(block.Syntax);
    }
}
