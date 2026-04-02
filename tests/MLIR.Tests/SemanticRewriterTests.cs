namespace MLIR.Tests;

using System.Collections.Generic;
using System.Linq;
using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed class SemanticRewriterTests
{
    // ─── helpers ────────────────────────────────────────────────────────────────

    private static Module ParseAndBind(string mlir)
    {
        return Binder.BindModule(Parser.ParseModule(mlir));
    }

    // A synthetic attribute value used to replace real ones in tests.
    private sealed class ReplacementAttributeValue : AttributeValue
    {
        public ReplacementAttributeValue()
            : base(null, SourceLocation.Unknown)
        {
        }

        public override string? Name => "replacement";
        public override AttributeDefinition? Definition => null;
    }

    // A synthetic operation used to replace real ones in tests.
    private sealed class ReplacementOperation : Operation
    {
        public ReplacementOperation()
            : base(null)
        {
        }

        public override string Name => "test.replacement";
        public override OperationDefinition? Definition => null;
        public override IReadOnlyList<Region> Regions => [];
        public override NamedAttributeCollection Attributes => NamedAttributeCollection.Empty;
        public override TypeReference? TypeSignatureReference => null;
        public override IReadOnlyList<ValueReference> ResultValues => [];
        public override IReadOnlyList<ValueReference> OperandValues => [];
        public override IReadOnlyList<BlockReference> SuccessorReferences => [];
    }

    // ─── identity rewriter ──────────────────────────────────────────────────────

    [Fact]
    public void IdentityRewriter_ReturnsExactSameModuleWhenNothingChanges()
    {
        var module = ParseAndBind(
            "%lhs = \"test.val\"() : () -> i32\n" +
            "%rhs = \"test.val\"() : () -> i32\n" +
            "%0 = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32");

        var rewriter = new SemanticRewriter();
        var rewritten = rewriter.VisitModule(module);

        Assert.Same(module, rewritten);
    }

    [Fact]
    public void IdentityRewriter_ReturnsExactSameOperationWhenNothingChanges()
    {
        var module = ParseAndBind("%0 = \"test.op\"() : () -> i32");

        var op = module.Operations[0];
        var rewriter = new SemanticRewriter();
        var rewritten = rewriter.VisitOperation(op);

        Assert.Same(op, rewritten);
    }

    [Fact]
    public void IdentityRewriter_ReturnsExactSameRegionWhenNothingChanges()
    {
        var module = ParseAndBind(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var region = module.Operations[0].Regions[0];
        var rewriter = new SemanticRewriter();
        var rewritten = rewriter.VisitRegion(region);

        Assert.Same(region, rewritten);
    }

    [Fact]
    public void IdentityRewriter_ReturnsExactSameBlockWhenNothingChanges()
    {
        var module = ParseAndBind(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var block = module.Operations[0].Regions[0].Blocks[0];
        var rewriter = new SemanticRewriter();
        var rewritten = rewriter.VisitBlock(block);

        Assert.Same(block, rewritten);
    }

    [Fact]
    public void IdentityRewriter_ReturnsExactSameCollectionWhenNothingChanges()
    {
        var module = ParseAndBind("%0 = \"test.op\"() {attr1 = 1 : i32, attr2 = 2 : i32} : () -> i32");

        var collection = module.Operations[0].Attributes;
        var rewriter = new SemanticRewriter();
        var rewritten = rewriter.VisitNamedAttributeCollection(collection);

        Assert.Same(collection, rewritten);
    }

    // ─── module-level operation replacement ─────────────────────────────────────

    [Fact]
    public void VisitModule_ReplacesChangedTopLevelOperations()
    {
        var module = ParseAndBind(
            "\"first.op\"() : () -> ()\n" +
            "\"second.op\"() : () -> ()");

        var replacement = new ReplacementOperation();
        var rewriter = new ReplaceFirstOpRewriter("first.op", replacement);
        var rewritten = rewriter.VisitModule(module);

        Assert.NotSame(module, rewritten);
        Assert.Equal(2, rewritten.Operations.Count);
        Assert.Same(replacement, rewritten.Operations[0]);
        // Second op is unchanged -- same object reference.
        Assert.Same(module.Operations[1], rewritten.Operations[1]);
    }

    private sealed class ReplaceFirstOpRewriter : SemanticRewriter
    {
        private readonly string targetName;
        private readonly Operation replacement;

        public ReplaceFirstOpRewriter(string targetName, Operation replacement)
        {
            this.targetName = targetName;
            this.replacement = replacement;
        }

        public override Operation VisitOperation(Operation operation)
        {
            if (operation.Name == targetName)
                return replacement;
            return base.VisitOperation(operation);
        }
    }

    // ─── attribute value replacement ────────────────────────────────────────────

    [Fact]
    public void VisitAttributeValue_ReplacesAttributeValue()
    {
        var module = ParseAndBind("%0 = \"test.op\"() {value = 42 : i32} : () -> i32");

        var original = module.Operations[0];
        var replacementAttrValue = new ReplacementAttributeValue();

        var rewriter = new ReplaceAttributeValueRewriter(replacementAttrValue);
        var rewritten = (UnknownOperation)rewriter.VisitOperation(original);

        Assert.NotSame(original, rewritten);
        Assert.Same(replacementAttrValue, rewritten.Attributes[0].Value);
        // The attribute name is preserved.
        Assert.Equal(original.Attributes[0].Name, rewritten.Attributes[0].Name);
    }

    [Fact]
    public void VisitAttributeValue_PreservesOperationWhenAttributeValueUnchanged()
    {
        var module = ParseAndBind("%0 = \"test.op\"() {value = 42 : i32} : () -> i32");

        var op = module.Operations[0];
        var rewriter = new SemanticRewriter(); // identity
        var rewritten = rewriter.VisitOperation(op);

        Assert.Same(op, rewritten);
    }

    private sealed class ReplaceAttributeValueRewriter : SemanticRewriter
    {
        private readonly AttributeValue replacement;

        public ReplaceAttributeValueRewriter(AttributeValue replacement)
        {
            this.replacement = replacement;
        }

        public override AttributeValue VisitAttributeValue(AttributeValue attributeValue)
        {
            return replacement;
        }
    }

    // ─── deep traversal ─────────────────────────────────────────────────────────

    [Fact]
    public void VisitModule_TraversesNestedOperationsInRegions()
    {
        var module = ParseAndBind(
            "\"outer.op\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"inner.op\"() : () -> ()\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var visited = new List<string>();
        var rewriter = new RecordingRewriter(visited);
        rewriter.VisitModule(module);

        Assert.Contains("outer.op", visited);
        Assert.Contains("inner.op", visited);
        Assert.Contains("func.return", visited);
    }

    private sealed class RecordingRewriter : SemanticRewriter
    {
        private readonly List<string> visited;

        public RecordingRewriter(List<string> visited)
        {
            this.visited = visited;
        }

        public override Operation VisitOperation(Operation operation)
        {
            visited.Add(operation.Name);
            return base.VisitOperation(operation);
        }
    }

    [Fact]
    public void VisitModule_ReplacesNestedOperation_RebuildingEnclosingNodes()
    {
        var module = ParseAndBind(
            "\"outer.op\"(%cond) {\n" +
            "  ^bb0:\n" +
            "    \"inner.op\"() : () -> ()\n" +
            "    \"func.return\"() : () -> ()\n" +
            "} : (i1) -> ()");

        var replacement = new ReplacementOperation();
        var rewriter = new ReplaceFirstOpRewriter("inner.op", replacement);
        var rewritten = rewriter.VisitModule(module);

        Assert.NotSame(module, rewritten);
        var innerBlock = rewritten.Operations[0].Regions[0].Blocks[0];
        Assert.Same(replacement, innerBlock.Operations[0]);
        // The second op inside the block is unchanged.
        Assert.Same(
            module.Operations[0].Regions[0].Blocks[0].Operations[1],
            innerBlock.Operations[1]);
    }

    // ─── block-argument type-reference visiting ──────────────────────────────────

    [Fact]
    public void VisitTypeReference_IsCalledForBlockArguments()
    {
        var module = ParseAndBind(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32):\n" +
            "    \"func.return\"(%arg0) : (i32) -> ()\n" +
            "} : (i1) -> ()");

        var typeRefs = new List<TypeReference>();
        var rewriter = new CollectTypeRefsRewriter(typeRefs);
        rewriter.VisitModule(module);

        Assert.NotEmpty(typeRefs);
    }

    [Fact]
    public void VisitBlockArgument_PreservesWhenTypeUnchanged()
    {
        var module = ParseAndBind(
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32):\n" +
            "    \"func.return\"(%arg0) : (i32) -> ()\n" +
            "} : (i1) -> ()");

        var block = module.Operations[0].Regions[0].Blocks[0];
        var arg = block.Arguments[0];
        var rewriter = new SemanticRewriter();
        var rewritten = rewriter.VisitBlockArgument(arg);

        Assert.Same(arg, rewritten);
    }

    private sealed class CollectTypeRefsRewriter : SemanticRewriter
    {
        private readonly List<TypeReference> collected;

        public CollectTypeRefsRewriter(List<TypeReference> collected)
        {
            this.collected = collected;
        }

        public override TypeReference VisitTypeReference(TypeReference typeReference)
        {
            collected.Add(typeReference);
            return base.VisitTypeReference(typeReference);
        }
    }

    // ─── no-copy for unchanged siblings ─────────────────────────────────────────

    [Fact]
    public void VisitModule_PreservesUnchangedSiblingOperations()
    {
        var module = ParseAndBind(
            "\"first.op\"() : () -> ()\n" +
            "\"second.op\"() : () -> ()\n" +
            "\"third.op\"() : () -> ()");

        var replacement = new ReplacementOperation();
        // Only replace the second op. First and third should be the same objects.
        var rewriter = new ReplaceFirstOpRewriter("second.op", replacement);
        var rewritten = rewriter.VisitModule(module);

        Assert.NotSame(module, rewritten);
        Assert.Same(module.Operations[0], rewritten.Operations[0]);
        Assert.Same(replacement, rewritten.Operations[1]);
        Assert.Same(module.Operations[2], rewritten.Operations[2]);
    }

    // ─── RewriteChildren extensibility ──────────────────────────────────────────

    [Fact]
    public void CustomOperation_RewriteChildren_CanParticipateInTraversal()
    {
        // A custom operation that holds a nested attribute value via its own field
        // and overrides RewriteChildren to participate in rewriting.
        var inner = new SyntheticAttributeValue("original");
        var customOp = new CustomOpWithAttribute(inner);

        var replacement = new ReplacementAttributeValue();
        var rewriter = new ReplaceAttributeValueRewriter(replacement);
        var rewritten = (CustomOpWithAttribute)rewriter.VisitOperation(customOp);

        Assert.NotSame(customOp, rewritten);
        Assert.Same(replacement, rewritten.StoredAttributeValue);
    }

    private sealed class SyntheticAttributeValue : AttributeValue
    {
        public SyntheticAttributeValue(string name)
            : base(null, SourceLocation.Unknown)
        {
            Name = name;
        }

        public override string? Name { get; }
        public override AttributeDefinition? Definition => null;
    }

    private sealed class CustomOpWithAttribute : Operation
    {
        private readonly AttributeValue attributeValue;

        public CustomOpWithAttribute(AttributeValue attributeValue)
            : base(null)
        {
            this.attributeValue = attributeValue;
            StoredAttributeValue = attributeValue;
        }

        public AttributeValue StoredAttributeValue { get; }

        public override string Name => "custom.op";
        public override OperationDefinition? Definition => null;
        public override IReadOnlyList<Region> Regions => [];
        public override NamedAttributeCollection Attributes => NamedAttributeCollection.Create(new NamedAttribute("val", attributeValue));
        public override TypeReference? TypeSignatureReference => null;
        public override IReadOnlyList<ValueReference> ResultValues => [];
        public override IReadOnlyList<ValueReference> OperandValues => [];
        public override IReadOnlyList<BlockReference> SuccessorReferences => [];

        public override Operation RewriteChildren(SemanticRewriter rewriter)
        {
            var newValue = rewriter.VisitAttributeValue(attributeValue);
            if (ReferenceEquals(newValue, attributeValue))
                return this;
            return new CustomOpWithAttribute(newValue);
        }
    }
}
