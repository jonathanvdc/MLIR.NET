namespace MLIR.Tests;

using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using Xunit;

public sealed class ConstantAttributeFactoryTests
{
    [Fact]
    public void StringCreatesSyntheticStringAttribute()
    {
        var attribute = ConstantAttributeFactory.String("hello");

        Assert.IsType<SyntheticStringAttributeValue>(attribute);
        Assert.Equal("hello", attribute.Value);
    }

    [Fact]
    public void BoolCreatesSyntheticBooleanAttribute()
    {
        var attribute = ConstantAttributeFactory.Bool(true);

        Assert.True(attribute.Value);
        Assert.Null(attribute.Syntax);
        Assert.Null(attribute.Name);
        Assert.Null(attribute.Definition);
    }

    [Fact]
    public void DenseI32CreatesDenseIntegerArrayAttribute()
    {
        int[] values = [1, 2, -3];

        var attribute = ConstantAttributeFactory.DenseI32(values);

        Assert.Equal(values.Length, attribute.Items.Count);
        Assert.Equal(-3, (int)attribute.Items[2].ToInt64());
    }

    [Fact]
    public void SymbolRefCloneReturnsEquivalentReference()
    {
        var reference = new SymbolRefAttr("root", ["nested"]);

        var clone = ConstantAttributeFactory.SymbolRef(reference);

        Assert.NotSame(reference, clone);
        Assert.Equal(reference.RootReference, clone.RootReference);
        Assert.Equal(reference.NestedReferences, clone.NestedReferences);
    }

    [Fact]
    public void FlatSymbolRefCreatesFlatReference()
    {
        var reference = ConstantAttributeFactory.FlatSymbolRef("foo");

        Assert.Equal("foo", reference.RootReference);
        Assert.Empty(reference.NestedReferences);
        Assert.True(reference.IsFlat);
    }
}
