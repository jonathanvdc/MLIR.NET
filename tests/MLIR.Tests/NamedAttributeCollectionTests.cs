using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Syntax;
using Xunit;

namespace MLIR.Semantics.Tests;

public sealed class NamedAttributeCollectionTests
{
    [Fact]
    public void Empty_HasNoItems()
    {
        var collection = NamedAttributeCollection.Empty;

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_FromEnumerable_PreservesOrder()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var c = CreateAttribute("c");

        var collection = new NamedAttributeCollection(new[] { a, b, c });

        Assert.Equal(3, collection.Count);
        Assert.Same(a, collection[0]);
        Assert.Same(b, collection[1]);
        Assert.Same(c, collection[2]);
    }

    [Fact]
    public void Constructor_FromArray_DefensivelyCopiesInput()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var c = CreateAttribute("c");

        var array = new[] { a, b };
        var collection = new NamedAttributeCollection(array);

        array[0] = c;

        Assert.Same(a, collection[0]);
        Assert.Same(b, collection[1]);
    }

    [Fact]
    public void Constructor_ThrowsOnDuplicateNames()
    {
        var a1 = CreateAttribute("a");
        var a2 = CreateAttribute("a");

        Assert.Throws<ArgumentException>(() => new NamedAttributeCollection(new[] { a1, a2 }));
    }

    [Fact]
    public void Create_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => NamedAttributeCollection.Create(null!));
    }

    [Fact]
    public void IntIndexer_ReturnsItemAtIndex()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var collection = new NamedAttributeCollection(new[] { a, b });

        Assert.Same(a, collection[0]);
        Assert.Same(b, collection[1]);
    }

    [Fact]
    public void StringIndexer_ReturnsMatchingAttribute()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var collection = new NamedAttributeCollection(new[] { a, b });

        Assert.Same(b, collection["b"]);
    }

    [Fact]
    public void StringIndexer_ThrowsWhenMissing()
    {
        var collection = new NamedAttributeCollection(new[] { CreateAttribute("a") });

        Assert.Throws<KeyNotFoundException>(() => collection["missing"]);
    }

    [Fact]
    public void Contains_ReturnsTrueWhenPresent()
    {
        var collection = new NamedAttributeCollection(new[] { CreateAttribute("a") });

        Assert.True(collection.Contains("a"));
        Assert.False(collection.Contains("b"));
    }

    [Fact]
    public void Contains_ThrowsOnNull()
    {
        var collection = NamedAttributeCollection.Empty;

        Assert.Throws<ArgumentNullException>(() => collection.Contains(null!));
    }

    [Fact]
    public void IndexOf_ReturnsIndexWhenPresent()
    {
        var collection = new NamedAttributeCollection(new[]
        {
            CreateAttribute("a"),
            CreateAttribute("b"),
            CreateAttribute("c"),
        });

        Assert.Equal(0, collection.IndexOf("a"));
        Assert.Equal(1, collection.IndexOf("b"));
        Assert.Equal(2, collection.IndexOf("c"));
        Assert.Equal(-1, collection.IndexOf("d"));
    }

    [Fact]
    public void IndexOf_ThrowsOnNull()
    {
        var collection = NamedAttributeCollection.Empty;

        Assert.Throws<ArgumentNullException>(() => collection.IndexOf(null!));
    }

    [Fact]
    public void TryGet_ReturnsTrueAndAttributeWhenPresent()
    {
        var b = CreateAttribute("b");
        var collection = new NamedAttributeCollection(new[]
        {
            CreateAttribute("a"),
            b,
        });

        var found = collection.TryGet("b", out var attribute);

        Assert.True(found);
        Assert.Same(b, attribute);
    }

    [Fact]
    public void TryGet_ReturnsFalseWhenMissing()
    {
        var collection = new NamedAttributeCollection(new[] { CreateAttribute("a") });

        var found = collection.TryGet("b", out var attribute);

        Assert.False(found);
        Assert.Equal(default, attribute);
    }

    [Fact]
    public void TryGet_ThrowsOnNull()
    {
        var collection = NamedAttributeCollection.Empty;

        Assert.Throws<ArgumentNullException>(() => collection.TryGet(null!, out _));
    }

    [Fact]
    public void Enumeration_PreservesOrder()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var c = CreateAttribute("c");

        var collection = new NamedAttributeCollection(new[] { a, b, c });

        Assert.Equal(new[] { a, b, c }, collection.ToArray());
    }

    [Fact]
    public void Insert_InsertsAtBeginning()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var x = CreateAttribute("x");
        var collection = new NamedAttributeCollection(new[] { a, b });

        var result = collection.Insert(0, x);

        Assert.NotSame(collection, result);
        Assert.Equal(new[] { x, a, b }, result.ToArray());
    }

    [Fact]
    public void Insert_InsertsInMiddle()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var x = CreateAttribute("x");
        var collection = new NamedAttributeCollection(new[] { a, b });

        var result = collection.Insert(1, x);

        Assert.Equal(new[] { a, x, b }, result.ToArray());
    }

    [Fact]
    public void Insert_InsertsAtEnd()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var x = CreateAttribute("x");
        var collection = new NamedAttributeCollection(new[] { a, b });

        var result = collection.Insert(2, x);

        Assert.Equal(new[] { a, b, x }, result.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Insert_ThrowsWhenIndexOutOfRange_ForEmptyCollection(int index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NamedAttributeCollection.Empty.Insert(index, CreateAttribute("x")));
    }

    [Fact]
    public void Insert_ThrowsWhenNameAlreadyExists()
    {
        var collection = new NamedAttributeCollection(new[]
        {
            CreateAttribute("a"),
            CreateAttribute("b"),
        });

        Assert.Throws<ArgumentException>(() => collection.Insert(1, CreateAttribute("a")));
    }

    [Fact]
    public void Add_AppendsAttribute()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var collection = new NamedAttributeCollection(new[] { a });

        var result = collection.Add(b);

        Assert.Equal(new[] { a, b }, result.ToArray());
    }

    [Fact]
    public void Add_ThrowsWhenNameAlreadyExists()
    {
        var collection = new NamedAttributeCollection(new[] { CreateAttribute("a") });

        Assert.Throws<ArgumentException>(() => collection.Add(CreateAttribute("a")));
    }

    [Fact]
    public void Remove_RemovesMatchingAttribute()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var c = CreateAttribute("c");
        var collection = new NamedAttributeCollection(new[] { a, b, c });

        var result = collection.Remove("b");

        Assert.NotSame(collection, result);
        Assert.Equal(new[] { a, c }, result.ToArray());
    }

    [Fact]
    public void Remove_ReturnsSameInstanceWhenMissing()
    {
        var collection = new NamedAttributeCollection(new[] { CreateAttribute("a") });

        var result = collection.Remove("b");

        Assert.Same(collection, result);
    }

    [Fact]
    public void Remove_ThrowsOnNull()
    {
        var collection = NamedAttributeCollection.Empty;

        Assert.Throws<ArgumentNullException>(() => collection.Remove(null!));
    }

    [Fact]
    public void RemoveAt_RemovesMatchingIndex()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var c = CreateAttribute("c");
        var collection = new NamedAttributeCollection(new[] { a, b, c });

        var result = collection.RemoveAt(1);

        Assert.Equal(new[] { a, c }, result.ToArray());
    }

    [Fact]
    public void RemoveAt_ReturnsEmptySingletonWhenRemovingLastItem()
    {
        var collection = new NamedAttributeCollection(new[] { CreateAttribute("a") });

        var result = collection.RemoveAt(0);

        Assert.Same(NamedAttributeCollection.Empty, result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void RemoveAt_ThrowsWhenIndexOutOfRange_ForEmptyCollection(int index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NamedAttributeCollection.Empty.RemoveAt(index));
    }

    [Fact]
    public void Set_ReplacesExistingAttribute()
    {
        var a1 = CreateAttribute("a");
        var a2 = CreateAttribute("a");
        var b = CreateAttribute("b");
        var collection = new NamedAttributeCollection(new[] { a1, b });

        var result = collection.Set(a2);

        Assert.NotSame(collection, result);
        Assert.Same(a2, result[0]);
        Assert.Same(b, result[1]);
    }

    [Fact]
    public void Set_ReturnsSameInstanceWhenAttributeIsUnchanged()
    {
        var a = CreateAttribute("a");
        var collection = new NamedAttributeCollection(new[] { a });

        var result = collection.Set(a);

        Assert.Same(collection, result);
    }

    [Fact]
    public void Set_ThrowsWhenMissing()
    {
        var collection = new NamedAttributeCollection(new[] { CreateAttribute("a") });

        Assert.Throws<ArgumentException>(() => collection.Set(CreateAttribute("b")));
    }

    [Fact]
    public void SetOrAdd_ReplacesExistingAttribute()
    {
        var a1 = CreateAttribute("a");
        var a2 = CreateAttribute("a");
        var b = CreateAttribute("b");
        var collection = new NamedAttributeCollection(new[] { a1, b });

        var result = collection.SetOrAdd(a2);

        Assert.NotSame(collection, result);
        Assert.Same(a2, result[0]);
        Assert.Same(b, result[1]);
    }

    [Fact]
    public void SetOrAdd_AddsMissingAttribute()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var collection = new NamedAttributeCollection(new[] { a });

        var result = collection.SetOrAdd(b);

        Assert.Equal(new[] { a, b }, result.ToArray());
    }

    [Fact]
    public void SetOrAdd_ReturnsSameInstanceWhenAttributeIsUnchanged()
    {
        var a = CreateAttribute("a");
        var collection = new NamedAttributeCollection(new[] { a });

        var result = collection.SetOrAdd(a);

        Assert.Same(collection, result);
    }

    [Fact]
    public void MutatingOperations_DoNotModifyOriginalCollection()
    {
        var a = CreateAttribute("a");
        var b = CreateAttribute("b");
        var c = CreateAttribute("c");
        var original = new NamedAttributeCollection(new[] { a, b });

        _ = original.Add(c);
        _ = original.Remove("a");
        _ = original.Set(CreateAttribute("a"));

        Assert.Equal(new[] { a, b }, original.ToArray());
    }

    private static NamedAttribute CreateAttribute(string name)
    {
        return new NamedAttribute(name, new UnknownAttributeValue(new RawAttributeValueSyntax(new RawSyntaxText(name)), null, null, SourceLocation.Unknown));
    }
}