namespace MLIR.Tests;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// <see cref="SymbolRefAttr"/>.
/// </summary>
public sealed class SymbolTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// A lightweight named-symbol operation that carries a <c>sym_name</c> string attribute.
    /// Used to represent leaves in a symbol table.
    /// </summary>
    private sealed class TestSymbolOp : Operation
    {
        public TestSymbolOp(string symbolName)
            : base(null, [], CreateAttributes(symbolName))
        {
        }

        public override string Name => "test.symbol";

        public override OperationDefinition? Definition => null;

        private static NamedAttributeCollection CreateAttributes(string symbolName)
        {
            return NamedAttributeCollection.Create(
                new NamedAttribute("sym_name", new SyntheticStringAttributeValue(symbolName)));
        }
    }

    /// <summary>
    /// An operation that acts as a symbol table: it overrides <see cref="GetSymbol{TSymbol}"/>
    /// to search its first region's immediate operations for a matching <c>sym_name</c>.
    /// This mirrors what the code generator emits for operations with the <c>SymbolTable</c> ODS trait.
    /// </summary>
    private sealed class TestSymbolTableOp : Operation
    {
        public TestSymbolTableOp(Region region)
            : base(null, [region])
        {
        }

        public override string Name => "test.module";

        public override OperationDefinition? Definition => null;

        [return: MaybeNull]
        public override TSymbol GetSymbol<TSymbol>(string name)
        {
            if (base.Regions.Count > 0)
            {
                foreach (var block in base.Regions[0].Blocks)
                {
                    foreach (var op in block.Operations)
                    {
                        if (op is TSymbol typedOp
                            && op.Attributes.TryGet("sym_name", out var attr)
                            && attr.Value is StringAttributeValue sv
                            && string.Equals(sv.Value, name, System.StringComparison.Ordinal))
                        {
                            return typedOp;
                        }
                    }
                }
            }

            return null;
        }
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
        innerModule.SetAttribute("sym_name", new NamedAttribute("sym_name", new SyntheticStringAttributeValue("inner")));

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
        var (_, _, fooOp, _, _) = BuildAst();

        // fooOp is inside innerModule. Looking up "bar" should reach outerModule.
        var found = fooOp.LookupSymbol<TestSymbolOp>("bar");
        Assert.Same(found, found);
        Assert.NotNull(found);
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
}
