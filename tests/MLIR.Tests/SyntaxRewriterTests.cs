namespace MLIR.Tests;

using System.Collections.Generic;
using MLIR;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed class SyntaxRewriterTests
{
    // ─── helpers ────────────────────────────────────────────────────────────────

    private static ModuleSyntax Parse(string mlir)
    {
        return Parser.ParseModule(mlir);
    }

    // A synthetic type syntax used to replace real ones in tests.
    private sealed class ReplacementTypeSyntax : TypeSyntax
    {
        public override bool TryGetRawText(out RawSyntaxText? rawText)
        {
            rawText = null;
            return false;
        }

        public override void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
        {
            writer.Write(defaultLeadingTrivia);
            writer.Write("i64");
        }
    }

    // A synthetic attribute value syntax used to replace real ones in tests.
    private sealed class ReplacementAttributeValueSyntax : AttributeValueSyntax
    {
        public override bool TryGetRawText(out RawSyntaxText? rawText)
        {
            rawText = null;
            return false;
        }

        public override void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
        {
            writer.Write(defaultLeadingTrivia);
            writer.Write("99 : i32");
        }
    }

    // ─── identity rewriter ──────────────────────────────────────────────────────

    [Fact]
    public void IdentityRewriter_ReturnsExactSameModuleWhenNothingChanges()
    {
        var module = Parse(
            "%lhs = \"test.val\"() : () -> i32\n" +
            "%rhs = \"test.val\"() : () -> i32\n" +
            "%0 = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32");

        var rewriter = new SyntaxRewriter();
        var rewritten = rewriter.VisitModule(module);

        Assert.Same(module, rewritten);
    }

    [Fact]
    public void IdentityRewriter_ReturnsExactSameOperationWhenNothingChanges()
    {
        var module = Parse("%0 = \"test.op\"() : () -> i32");

        var op = module.Operations[0];
        var rewriter = new SyntaxRewriter();
        var rewritten = rewriter.VisitOperation(op);

        Assert.Same(op, rewritten);
    }

    [Fact]
    public void IdentityRewriter_ReturnsExactSameRegionWhenNothingChanges()
    {
        var module = Parse(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var body = (GenericOperationBodySyntax)module.Operations[0].Body;
        var region = body.Regions[0];
        var rewriter = new SyntaxRewriter();
        var rewritten = rewriter.VisitRegion(region);

        Assert.Same(region, rewritten);
    }

    [Fact]
    public void IdentityRewriter_ReturnsExactSameBlockWhenNothingChanges()
    {
        var module = Parse(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var body = (GenericOperationBodySyntax)module.Operations[0].Body;
        var block = body.Regions[0].Blocks[0];
        var rewriter = new SyntaxRewriter();
        var rewritten = rewriter.VisitBlock(block);

        Assert.Same(block, rewritten);
    }

    // ─── module-level operation replacement ─────────────────────────────────────

    [Fact]
    public void VisitModule_ReplacesChangedTopLevelOperations()
    {
        var module = Parse(
            "\"first.op\"() : () -> ()\n" +
            "\"second.op\"() : () -> ()");

        var replacement = new OperationSyntax([], "replacement.op", [], [], [], [], null);
        var rewriter = new ReplaceFirstOpRewriter("\"first.op\"", replacement);
        var rewritten = rewriter.VisitModule(module);

        Assert.NotSame(module, rewritten);
        Assert.Equal(2, rewritten.Operations.Count);
        Assert.Same(replacement, rewritten.Operations[0]);
        // Second op is unchanged -- same object reference.
        Assert.Same(module.Operations[1], rewritten.Operations[1]);
    }

    private sealed class ReplaceFirstOpRewriter : SyntaxRewriter
    {
        private readonly string targetName;
        private readonly OperationSyntax replacement;

        public ReplaceFirstOpRewriter(string targetName, OperationSyntax replacement)
        {
            this.targetName = targetName;
            this.replacement = replacement;
        }

        public override OperationSyntax VisitOperation(OperationSyntax operation)
        {
            if (operation.Name == targetName)
                return replacement;
            return base.VisitOperation(operation);
        }
    }

    // ─── type syntax replacement ─────────────────────────────────────────────────

    [Fact]
    public void VisitTypeSyntax_ReplacesTypeInGenericOperationBody()
    {
        var module = Parse("%0 = \"test.op\"() : () -> i32");

        var original = module.Operations[0];
        var replacementType = new ReplacementTypeSyntax();

        var rewriter = new ReplaceTypeSyntaxRewriter(replacementType);
        var rewritten = rewriter.VisitOperation(original);

        Assert.NotSame(original, rewritten);
        var newBody = (GenericOperationBodySyntax)rewritten.Body;
        Assert.Same(replacementType, newBody.TypeSignatureSyntax);
    }

    [Fact]
    public void VisitTypeSyntax_PreservesOperationWhenTypeUnchanged()
    {
        var module = Parse("%0 = \"test.op\"() : () -> i32");

        var op = module.Operations[0];
        var rewriter = new SyntaxRewriter(); // identity
        var rewritten = rewriter.VisitOperation(op);

        Assert.Same(op, rewritten);
    }

    private sealed class ReplaceTypeSyntaxRewriter : SyntaxRewriter
    {
        private readonly TypeSyntax replacement;

        public ReplaceTypeSyntaxRewriter(TypeSyntax replacement)
        {
            this.replacement = replacement;
        }

        public override TypeSyntax VisitTypeSyntax(TypeSyntax typeSyntax)
        {
            return replacement;
        }
    }

    // ─── attribute value syntax replacement ──────────────────────────────────────

    [Fact]
    public void VisitAttributeValue_ReplacesAttributeValueInNamedAttribute()
    {
        var module = Parse("%0 = \"test.op\"() {value = 42 : i32} : () -> i32");

        var original = module.Operations[0];
        var originalBody = (GenericOperationBodySyntax)original.Body;
        var originalAttr = originalBody.Attributes[0];

        var replacementValue = new ReplacementAttributeValueSyntax();
        var rewriter = new ReplaceAttributeValueRewriter(replacementValue);
        var rewritten = rewriter.VisitOperation(original);

        Assert.NotSame(original, rewritten);
        var newBody = (GenericOperationBodySyntax)rewritten.Body;
        Assert.Same(replacementValue, newBody.Attributes[0].ValueSyntax);
        // Attribute name is preserved.
        Assert.Equal(originalAttr.Name, newBody.Attributes[0].Name);
    }

    [Fact]
    public void VisitAttributeValue_PreservesOperationWhenAttributeValueUnchanged()
    {
        var module = Parse("%0 = \"test.op\"() {value = 42 : i32} : () -> i32");

        var op = module.Operations[0];
        var rewriter = new SyntaxRewriter(); // identity
        var rewritten = rewriter.VisitOperation(op);

        Assert.Same(op, rewritten);
    }

    private sealed class ReplaceAttributeValueRewriter : SyntaxRewriter
    {
        private readonly AttributeValueSyntax replacement;

        public ReplaceAttributeValueRewriter(AttributeValueSyntax replacement)
        {
            this.replacement = replacement;
        }

        public override AttributeValueSyntax VisitAttributeValue(AttributeValueSyntax attributeValue)
        {
            return replacement;
        }
    }

    // ─── deep traversal ──────────────────────────────────────────────────────────

    [Fact]
    public void VisitModule_TraversesNestedOperationsInRegions()
    {
        var module = Parse(
            "\"outer.op\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"inner.op\"() : () -> ()\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var visited = new List<string>();
        var rewriter = new RecordingRewriter(visited);
        rewriter.VisitModule(module);

        Assert.Contains("\"outer.op\"", visited);
        Assert.Contains("\"inner.op\"", visited);
        Assert.Contains("\"func.return\"", visited);
    }

    private sealed class RecordingRewriter : SyntaxRewriter
    {
        private readonly List<string> visited;

        public RecordingRewriter(List<string> visited)
        {
            this.visited = visited;
        }

        public override OperationSyntax VisitOperation(OperationSyntax operation)
        {
            visited.Add(operation.Name);
            return base.VisitOperation(operation);
        }
    }

    [Fact]
    public void VisitModule_ReplacesNestedOperation_RebuildingEnclosingNodes()
    {
        var module = Parse(
            "\"outer.op\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"inner.op\"() : () -> ()\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var replacement = new OperationSyntax([], "replacement.op", [], [], [], [], null);
        var rewriter = new ReplaceFirstOpRewriter("\"inner.op\"", replacement);
        var rewritten = rewriter.VisitModule(module);

        Assert.NotSame(module, rewritten);
        var outerBody = (GenericOperationBodySyntax)rewritten.Operations[0].Body;
        var innerBlock = outerBody.Regions[0].Blocks[0];
        Assert.Same(replacement, innerBlock.Operations[0]);
        // The second op inside the block is unchanged.
        Assert.Same(
            ((GenericOperationBodySyntax)module.Operations[0].Body).Regions[0].Blocks[0].Operations[1],
            innerBlock.Operations[1]);
    }

    // ─── block argument type rewriting ──────────────────────────────────────────

    [Fact]
    public void VisitTypeSyntax_IsCalledForBlockArguments()
    {
        var module = Parse(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32):\n" +
            "    \"func.return\"(%arg0) : (i32) -> ()\n" +
            "} : (i1) -> ()");

        var typeSyntaxNodes = new List<TypeSyntax>();
        var rewriter = new CollectTypeSyntaxRewriter(typeSyntaxNodes);
        rewriter.VisitModule(module);

        Assert.NotEmpty(typeSyntaxNodes);
    }

    [Fact]
    public void VisitBlockArgument_PreservesWhenTypeUnchanged()
    {
        var module = Parse(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32):\n" +
            "    \"func.return\"(%arg0) : (i32) -> ()\n" +
            "} : (i1) -> ()");

        var body = (GenericOperationBodySyntax)module.Operations[0].Body;
        var arg = body.Regions[0].Blocks[0].Arguments[0];
        var rewriter = new SyntaxRewriter();
        var rewritten = rewriter.VisitBlockArgument(arg);

        Assert.Same(arg, rewritten);
    }

    private sealed class CollectTypeSyntaxRewriter : SyntaxRewriter
    {
        private readonly List<TypeSyntax> collected;

        public CollectTypeSyntaxRewriter(List<TypeSyntax> collected)
        {
            this.collected = collected;
        }

        public override TypeSyntax VisitTypeSyntax(TypeSyntax typeSyntax)
        {
            collected.Add(typeSyntax);
            return base.VisitTypeSyntax(typeSyntax);
        }
    }

    // ─── no-copy for unchanged siblings ─────────────────────────────────────────

    [Fact]
    public void VisitModule_PreservesUnchangedSiblingOperations()
    {
        var module = Parse(
            "\"first.op\"() : () -> ()\n" +
            "\"second.op\"() : () -> ()\n" +
            "\"third.op\"() : () -> ()");

        var replacement = new OperationSyntax([], "replacement.op", [], [], [], [], null);
        var rewriter = new ReplaceFirstOpRewriter("\"second.op\"", replacement);
        var rewritten = rewriter.VisitModule(module);

        Assert.NotSame(module, rewritten);
        Assert.Same(module.Operations[0], rewritten.Operations[0]);
        Assert.Same(replacement, rewritten.Operations[1]);
        Assert.Same(module.Operations[2], rewritten.Operations[2]);
    }

    // ─── EndOfFileToken preservation ─────────────────────────────────────────────

    [Fact]
    public void VisitModule_PreservesEndOfFileToken()
    {
        var module = Parse("\"test.op\"() : () -> ()");
        var replacement = new OperationSyntax([], "replacement.op", [], [], [], [], null);
        var rewriter = new ReplaceFirstOpRewriter("\"test.op\"", replacement);
        var rewritten = rewriter.VisitModule(module);

        Assert.NotSame(module, rewritten);
        Assert.Equal(module.EndOfFileToken.Text, rewritten.EndOfFileToken.Text);
    }
}
