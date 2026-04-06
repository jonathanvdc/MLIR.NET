namespace MLIR.Tests;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using Xunit;

/// <summary>
/// Tests for the <see cref="Operation.GetSymbol{TSymbol}"/>,
/// <see cref="Operation.LookupSymbol{TSymbol}"/>, and
/// <see cref="Operation.Resolve{TSymbol}"/> methods, as well as for
/// <see cref="SymbolRefAttr"/> and <see cref="SymbolTableOperation"/>.
/// </summary>
public sealed class SymbolTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// A lightweight named-symbol operation that carries a <c>sym_name</c> string attribute.
    /// Used to represent leaves in a symbol table.
    /// Implements <see cref="ISymbolOp"/> so it is discoverable via the typed interface.
    /// </summary>
    private sealed class TestSymbolOp : Operation, ISymbolOp
    {
        public TestSymbolOp(string symbolName)
            : base(null, [], CreateAttributes(symbolName))
        {
        }

        public override string Name => "test.symbol";

        public override OperationDefinition? Definition => null;

        public string? SymbolName
        {
            get => Attributes.TryGet("sym_name", out var attr) && attr.Value is StringAttributeValue sv ? sv.Value : null;
            set => SetAttribute("sym_name", value != null ? new SyntheticStringAttributeValue(value) : null);
        }

        private static NamedAttributeCollection CreateAttributes(string symbolName)
        {
            return NamedAttributeCollection.Create(
                new NamedAttribute("sym_name", new SyntheticStringAttributeValue(symbolName)));
        }
    }

    /// <summary>
    /// An operation that acts as a symbol table by inheriting <see cref="SymbolTableOperation"/>,
    /// which provides the O(1) cached dictionary and cache-invalidation logic.
    /// This mirrors the class hierarchy the code generator produces for operations with the
    /// <c>SymbolTable</c> ODS trait.
    /// </summary>
    private sealed class TestSymbolTableOp : SymbolTableOperation
    {
        public TestSymbolTableOp(Region region)
            : base(null, [region])
        {
        }

        public override string Name => "test.module";

        public override OperationDefinition? Definition => null;
    }

    /// <summary>
    /// Builds a test AST:
    /// <code>
    /// outer-module {                 (TestSymbolTableOp)
    ///   inner-module {               (TestSymbolTableOp with sym_name = "inner")
    ///     func @foo {}               (TestSymbolOp "foo")
    ///   }
    ///   func @bar {}                 (TestSymbolOp "bar")
    ///   leaf-op {}                   (plain Operation — no sym_name)
    /// }
    /// </code>
    /// Returns the plain leaf operation so tests can call <c>LookupSymbol</c> / <c>Resolve</c>
    /// from a descendant of the symbol tables.
    /// </summary>
    private static (TestSymbolTableOp outerModule, TestSymbolTableOp innerModule, TestSymbolOp fooOp, TestSymbolOp barOp, TestSymbolOp leafSymbol) BuildAst()
    {
        // Inner module
        var fooOp = new TestSymbolOp("foo");
        var innerBlock = new Block("^bb0", [], [fooOp]);
        var innerRegion = new Region(null, [innerBlock]);
        var innerModule = new TestSymbolTableOp(innerRegion);

        // Outer block containing innerModule, barOp, and an unnamed leaf
        var barOp = new TestSymbolOp("bar");
        var leafSymbol = new TestSymbolOp("leaf");

        // Give innerModule a sym_name so it can be resolved by nested refs
        innerModule.SetAttribute("sym_name", new SyntheticStringAttributeValue("inner"));

        var outerBlock = new Block("^bb0", [], [innerModule, barOp, leafSymbol]);
        var outerRegion = new Region(null, [outerBlock]);
        var outerModule = new TestSymbolTableOp(outerRegion);

        return (outerModule, innerModule, fooOp, barOp, leafSymbol);
    }

    // ---------------------------------------------------------------------------
    // GetSymbol tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetSymbolDefaultReturnsNull()
    {
        // Operations that do not override GetSymbol should return null.
        var plain = new TestSymbolOp("foo");
        Assert.Null(plain.GetSymbol<TestSymbolOp>("foo"));
    }

    [Fact]
    public void GetSymbolFindsDirectChild()
    {
        var (outerModule, _, _, barOp, _) = BuildAst();

        var found = outerModule.GetSymbol<TestSymbolOp>("bar");
        Assert.Same(barOp, found);
    }

    [Fact]
    public void GetSymbolReturnsNullForMissingName()
    {
        var (outerModule, _, _, _, _) = BuildAst();

        Assert.Null(outerModule.GetSymbol<TestSymbolOp>("nonexistent"));
    }

    [Fact]
    public void GetSymbolIsTypeFiltered()
    {
        // Asking for a specific subtype that does not match should return null.
        var (outerModule, _, _, _, _) = BuildAst();

        // TestSymbolTableOp is also in the block (as "inner") but it IS-A TestSymbolTableOp,
        // not a TestSymbolOp with name "inner".
        Assert.Null(outerModule.GetSymbol<TestSymbolOp>("inner"));
    }

    [Fact]
    public void GetSymbolDoesNotDoDeepTraversal()
    {
        var (outerModule, _, fooOp, _, _) = BuildAst();

        // "foo" is nested inside innerModule, not a direct child of outerModule.
        Assert.Null(outerModule.GetSymbol<TestSymbolOp>("foo"));
    }

    // ---------------------------------------------------------------------------
    // LookupSymbol tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void LookupSymbolFindsSymbolInImmediateParentSymbolTable()
    {
        var (_, _, _, barOp, leafSymbol) = BuildAst();

        // leafSymbol is a direct child of outerModule; it should find "bar" in the same table.
        var found = leafSymbol.LookupSymbol<TestSymbolOp>("bar");
        Assert.Same(barOp, found);
    }

    [Fact]
    public void LookupSymbolFindsSymbolInGrandparentSymbolTable()
    {
        var (_, _, fooOp, barOp, _) = BuildAst();

        // fooOp is inside innerModule. Looking up "bar" should reach outerModule.
        var found = fooOp.LookupSymbol<TestSymbolOp>("bar");
        Assert.Same(barOp, found);
    }

    [Fact]
    public void LookupSymbolReturnsNullWhenSymbolNotFound()
    {
        var (_, _, _, _, leafSymbol) = BuildAst();

        Assert.Null(leafSymbol.LookupSymbol<TestSymbolOp>("nonexistent"));
    }

    [Fact]
    public void LookupSymbolStopsAtFirstMatch()
    {
        var (_, _, _, barOp, leafSymbol) = BuildAst();

        // "bar" is defined in outerModule; should be found without needing to climb further.
        var found = leafSymbol.LookupSymbol<TestSymbolOp>("bar");
        Assert.Same(barOp, found);
    }

    [Fact]
    public void LookupSymbolFromRootReturnsNull()
    {
        var (outerModule, _, _, _, _) = BuildAst();

        // The outer module has no parent; LookupSymbol from it should return null.
        Assert.Null(outerModule.LookupSymbol<TestSymbolOp>("bar"));
    }

    // ---------------------------------------------------------------------------
    // Resolve tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveFlatRefIsEquivalentToLookupSymbol()
    {
        var (_, _, _, barOp, leafSymbol) = BuildAst();

        var resolved = leafSymbol.Resolve<TestSymbolOp>(new SymbolRefAttr("bar"));
        Assert.Same(barOp, resolved);
    }

    [Fact]
    public void ResolveNestedRefFindsDeepSymbol()
    {
        var (_, _, fooOp, _, leafSymbol) = BuildAst();

        // "@inner::@foo" should find "foo" inside "inner".
        var resolved = leafSymbol.Resolve<TestSymbolOp>(new SymbolRefAttr("inner", ["foo"]));
        Assert.Same(fooOp, resolved);
    }

    [Fact]
    public void ResolveReturnsNullForMissingRoot()
    {
        var (_, _, _, _, leafSymbol) = BuildAst();

        Assert.Null(leafSymbol.Resolve<TestSymbolOp>(new SymbolRefAttr("nonexistent")));
    }

    [Fact]
    public void ResolveReturnsNullForMissingNestedComponent()
    {
        var (_, _, _, _, leafSymbol) = BuildAst();

        Assert.Null(leafSymbol.Resolve<TestSymbolOp>(new SymbolRefAttr("inner", ["doesnotexist"])));
    }

    // ---------------------------------------------------------------------------
    // SymbolRefAttr tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void SymbolRefAttrFlatToString()
    {
        var attr = new SymbolRefAttr("foo");
        Assert.Equal("@foo", attr.ToString());
        Assert.True(attr.IsFlat);
        Assert.Equal("foo", attr.LeafReference);
    }

    [Fact]
    public void SymbolRefAttrNestedToString()
    {
        var attr = new SymbolRefAttr("outer", ["inner"]);
        Assert.Equal("@outer::@inner", attr.ToString());
        Assert.False(attr.IsFlat);
        Assert.Equal("inner", attr.LeafReference);
        Assert.Equal("outer", attr.RootReference);
    }

    [Fact]
    public void SymbolRefAttrMultiLevelNestedToString()
    {
        var attr = new SymbolRefAttr("a", ["b", "c"]);
        Assert.Equal("@a::@b::@c", attr.ToString());
        Assert.Equal("c", attr.LeafReference);
    }

    // ---------------------------------------------------------------------------
    // SyntheticStringAttributeValue tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void SyntheticStringAttributeValueReturnsCorrectValue()
    {
        var attr = new SyntheticStringAttributeValue("hello");
        Assert.Equal("hello", attr.Value);
        Assert.Null(attr.Name);
        Assert.Null(attr.Definition);
        Assert.Null(attr.Syntax);
    }

    [Fact]
    public void OperationSymbolNameAttributeRoundTrips()
    {
        var op = new TestSymbolOp("main");

        // The TestSymbolOp uses SyntheticStringAttributeValue, so reading it back should work.
        Assert.True(op.Attributes.TryGet("sym_name", out var attr));
        var sv = Assert.IsAssignableFrom<StringAttributeValue>(attr.Value);
        Assert.Equal("main", sv.Value);
    }

    // ---------------------------------------------------------------------------
    // Cache invalidation tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void SymbolCacheIsInvalidatedWhenNewOperationIsAdded()
    {
        // Prime the cache.
        var (outerModule, _, _, _, _) = BuildAst();
        Assert.Null(outerModule.GetSymbol<TestSymbolOp>("baz"));

        // Add a new symbol op to the first block.
        var bazOp = new TestSymbolOp("baz");
        outerModule.Regions[0].Blocks[0].AddOperation(bazOp);

        // Cache should have been invalidated; the new symbol must now be found.
        var found = outerModule.GetSymbol<TestSymbolOp>("baz");
        Assert.Same(bazOp, found);
    }

    [Fact]
    public void SymbolCacheReturnsStaleDataBeforeInvalidation()
    {
        // Prime the cache by accessing Symbols.
        var (outerModule, _, _, barOp, _) = BuildAst();
        var initialCount = outerModule.Symbols.Count;
        Assert.Same(barOp, outerModule.GetSymbol<TestSymbolOp>("bar"));

        // Access again to confirm the cache is reused (same content).
        Assert.Equal(initialCount, outerModule.Symbols.Count);
        Assert.Same(barOp, outerModule.GetSymbol<TestSymbolOp>("bar"));
    }

    [Fact]
    public void SymbolCacheIsInvalidatedWhenRegionContentsChangeThroughNestedBlock()
    {
        // Build a module with an inner block, prime the cache, then add a new op to a new block.
        var fooOp = new TestSymbolOp("foo");
        var block1 = new Block("^bb0", [], [fooOp]);
        var region = new Region(null, [block1]);
        var mod = new TestSymbolTableOp(region);

        // Prime the cache.
        Assert.Same(fooOp, mod.GetSymbol<TestSymbolOp>("foo"));
        Assert.Null(mod.GetSymbol<TestSymbolOp>("bar"));

        // Add a new block with a new symbol op.
        var barOp = new TestSymbolOp("bar");
        var block2 = new Block("^bb1", [], [barOp]);
        region.AddBlock(block2);

        // The cache must have been invalidated.
        Assert.Same(barOp, mod.GetSymbol<TestSymbolOp>("bar"));
    }

    // ---------------------------------------------------------------------------
    // ISymbolOp interface tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void TestSymbolOpImplementsISymbolOp()
    {
        var op = new TestSymbolOp("hello");
        Assert.IsAssignableFrom<ISymbolOp>(op);
    }

    [Fact]
    public void ISymbolOpSymbolNameMatchesAttribute()
    {
        ISymbolOp op = new TestSymbolOp("hello");
        Assert.Equal("hello", op.SymbolName);
    }

    [Fact]
    public void SymbolTableContentsCanBeFilteredViaISymbolOpInterface()
    {
        var (outerModule, _, fooOp, barOp, _) = BuildAst();

        // Collect all ISymbolOp children of the outer module via the Symbols dictionary.
        // This demonstrates that ISymbolOp enables typed traversal without string-attribute inspection.
        var symbolOps = new List<(string Name, Operation Op)>();
        foreach (var (name, op) in outerModule.Symbols)
        {
            if (op is ISymbolOp)
            {
                symbolOps.Add((name, op));
            }
        }

        Assert.Contains(symbolOps, t => t.Name == "bar" && ReferenceEquals(t.Op, barOp));
    }

    [Fact]
    public void SymbolTableCacheIndexesISymbolOpChildrenViaInterface()
    {
        // The SymbolTableOperation cache must use ISymbolOp for typed ops.
        var fooOp = new TestSymbolOp("foo");  // implements ISymbolOp
        var block = new Block("^bb0", [], [fooOp]);
        var region = new Region(null, [block]);
        var mod = new TestSymbolTableOp(region);

        // Should be findable via the interface-backed cache.
        Assert.Same(fooOp, mod.GetSymbol<TestSymbolOp>("foo"));
        Assert.Same(fooOp, mod.Symbols["foo"]);
    }

    [Fact]
    public void SymbolTableCacheFallsBackToAttributeForNonISymbolOpChildren()
    {
        // An operation that does NOT implement ISymbolOp but carries sym_name as a raw attribute
        // should still be indexed via the attribute fallback.
        var rawOp = new RawSymbolAttributeOp("raw");
        var block = new Block("^bb0", [], [rawOp]);
        var region = new Region(null, [block]);
        var mod = new TestSymbolTableOp(region);

        Assert.Same(rawOp, mod.Symbols["raw"]);
    }

    /// <summary>
    /// An operation that carries <c>sym_name</c> as a raw attribute but does NOT implement
    /// <see cref="ISymbolOp"/>. Used to test the attribute fallback path in
    /// <see cref="SymbolTableOperation"/>.
    /// </summary>
    private sealed class RawSymbolAttributeOp : Operation
    {
        public RawSymbolAttributeOp(string symbolName)
            : base(null, [], NamedAttributeCollection.Create(
                new NamedAttribute("sym_name", new SyntheticStringAttributeValue(symbolName))))
        {
        }

        public override string Name => "test.raw";

        public override OperationDefinition? Definition => null;
    }
}
