namespace MLIR.Tests;

using MLIR.Semantics;
using Xunit;

public sealed class ConstantAttributeFactoryTests
{
    [Fact]
    public void StringCreatesStringAttribute()
    {
        var attribute = ConstantAttributeFactory.String("hello");

        Assert.IsType<StringAttr>(attribute);
        Assert.Equal("hello", attribute.Value);
        Assert.Equal(TypeFactory.None, attribute.Type);
    }

    [Fact]
    public void BoolCreatesI1IntegerAttribute()
    {
        var attribute = ConstantAttributeFactory.Bool(true);

        Assert.IsType<IntegerAttr>(attribute);
        Assert.Equal(TypeFactory.I1, attribute.Type);
        Assert.Equal(1ul, attribute.Value.ToUInt64());
    }

    [Fact]
    public void DenseI32CreatesDenseArrayAttribute()
    {
        int[] values = [1, 2, -3];

        var attribute = ConstantAttributeFactory.DenseI32(values);

        Assert.IsType<DenseArrayAttr>(attribute);
        Assert.Equal(TypeFactory.I32, attribute.ElementType);
        Assert.Equal(values.Length, attribute.Size);
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
