#nullable enable

namespace MLIR.Semantics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents an immutable, ordered collection of named attributes.
/// </summary>
/// <remarks>
/// <para>
/// This collection preserves the source ordering of attributes while also providing
/// convenient lookup by attribute name.
/// </para>
/// <para>
/// Attribute names must be unique within a collection. Any operation that would violate
/// this invariant throws an exception rather than producing an invalid collection.
/// </para>
/// <para>
/// Instances of this type are immutable. Methods that conceptually modify the collection,
/// such as insertion, removal, or replacement, return a new <see cref="NamedAttributeCollection"/>
/// when a change is required and return the existing instance when the requested operation
/// would leave the collection unchanged.
/// </para>
/// </remarks>
public sealed class NamedAttributeCollection : IReadOnlyList<NamedAttribute>
{
    /// <summary>
    /// Gets an empty <see cref="NamedAttributeCollection"/>.
    /// </summary>
    public static NamedAttributeCollection Empty { get; } =
        new NamedAttributeCollection(Array.Empty<NamedAttribute>());

    private readonly NamedAttribute[] items;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedAttributeCollection"/> class.
    /// </summary>
    /// <param name="attributes">The attributes to include in the collection.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="attributes"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if multiple attributes have the same name.
    /// </exception>
    public NamedAttributeCollection(IEnumerable<NamedAttribute> attributes)
        : this(attributes is null
            ? throw new ArgumentNullException(nameof(attributes))
            : Enumerable.ToArray(attributes))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedAttributeCollection"/> class.
    /// </summary>
    /// <param name="attributes">The attributes to include in the collection.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if multiple attributes have the same name.
    /// </exception>
    public NamedAttributeCollection(NamedAttribute[] attributes)
    {
        items = attributes is null ? Array.Empty<NamedAttribute>() : (NamedAttribute[])attributes.Clone();
        ValidateUniqueNames(items);
    }

    /// <summary>
    /// Gets the number of attributes in the collection.
    /// </summary>
    public int Count => items.Length;

    /// <summary>
    /// Gets the attribute at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the attribute to retrieve.</param>
    /// <returns>The attribute at the specified index.</returns>
    public NamedAttribute this[int index] => items[index];

    /// <summary>
    /// Gets the attribute with the specified name.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The matching attribute.</returns>
    public NamedAttribute this[string name] => GetRequired(name);

    /// <summary>
    /// Creates a <see cref="NamedAttributeCollection"/> from the specified attributes.
    /// </summary>
    /// <param name="attributes">The attributes to include in the collection.</param>
    /// <returns>A new <see cref="NamedAttributeCollection"/>.</returns>
    public static NamedAttributeCollection Create(params NamedAttribute[] attributes)
    {
        if (attributes is null)
            throw new ArgumentNullException(nameof(attributes));
        return new NamedAttributeCollection(attributes);
    }

    /// <summary>
    /// Returns a new collection with the specified attribute inserted at the given index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the attribute.</param>
    /// <param name="attribute">The attribute to insert.</param>
    /// <returns>
    /// A new <see cref="NamedAttributeCollection"/> containing the inserted attribute, or this instance
    /// if the insertion would not change the collection.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="index"/> is less than zero or greater than <see cref="Count"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if another attribute with the same name already exists in the collection.
    /// </exception>
    public NamedAttributeCollection Insert(int index, NamedAttribute attribute)
    {
        if (index < 0 || index > items.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        int existingIndex = IndexOf(attribute.Name);
        if (existingIndex >= 0)
        {
            if (existingIndex == index && items[existingIndex].Equals(attribute))
            {
                return this;
            }

            throw new ArgumentException(
                $"The collection already contains an attribute named '{attribute.Name}'.",
                nameof(attribute));
        }

        var newItems = new NamedAttribute[items.Length + 1];
        Array.Copy(items, 0, newItems, 0, index);
        newItems[index] = attribute;
        Array.Copy(items, index, newItems, index + 1, items.Length - index);
        return new NamedAttributeCollection(newItems);
    }

    /// <summary>
    /// Returns a new collection with the specified attribute appended.
    /// </summary>
    /// <param name="attribute">The attribute to append.</param>
    /// <returns>
    /// A new <see cref="NamedAttributeCollection"/> containing the appended attribute, or this instance
    /// if the append would not change the collection.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if another attribute with the same name already exists in the collection.
    /// </exception>
    public NamedAttributeCollection Add(NamedAttribute attribute)
    {
        return Insert(items.Length, attribute);
    }

    /// <summary>
    /// Returns a new collection with the specified attribute removed.
    /// </summary>
    /// <param name="name">The name of the attribute to remove.</param>
    /// <returns>
    /// A new <see cref="NamedAttributeCollection"/> without the specified attribute, or this instance
    /// if no matching attribute exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public NamedAttributeCollection Remove(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        int index = IndexOf(name);
        if (index < 0)
        {
            return this;
        }

        return RemoveAt(index);
    }

    /// <summary>
    /// Returns a new collection with the attribute at the specified index removed.
    /// </summary>
    /// <param name="index">The zero-based index of the attribute to remove.</param>
    /// <returns>A new <see cref="NamedAttributeCollection"/> without the specified attribute.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="index"/> is less than zero or greater than or equal to <see cref="Count"/>.
    /// </exception>
    public NamedAttributeCollection RemoveAt(int index)
    {
        if (index < 0 || index >= items.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (items.Length == 1)
        {
            return Empty;
        }

        var newItems = new NamedAttribute[items.Length - 1];
        Array.Copy(items, 0, newItems, 0, index);
        Array.Copy(items, index + 1, newItems, index, items.Length - index - 1);
        return new NamedAttributeCollection(newItems);
    }

    /// <summary>
    /// Returns a new collection with the specified attribute replaced.
    /// </summary>
    /// <param name="attribute">The replacement attribute.</param>
    /// <returns>
    /// A new <see cref="NamedAttributeCollection"/> containing the replacement attribute, or this instance
    /// if the replacement would not change the collection.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no attribute with the same name exists in the collection.
    /// </exception>
    public NamedAttributeCollection Set(NamedAttribute attribute)
    {
        int index = IndexOf(attribute.Name);
        if (index < 0)
        {
            throw new ArgumentException(
                $"The collection does not contain an attribute named '{attribute.Name}'.",
                nameof(attribute));
        }

        if (items[index].Equals(attribute))
        {
            return this;
        }

        var newItems = (NamedAttribute[])items.Clone();
        newItems[index] = attribute;
        return new NamedAttributeCollection(newItems);
    }

    /// <summary>
    /// Returns a new collection with the specified attribute added or replaced.
    /// </summary>
    /// <param name="attribute">The attribute to add or replace.</param>
    /// <returns>
    /// A new <see cref="NamedAttributeCollection"/> containing the added or replaced attribute, or this instance
    /// if the operation would not change the collection.
    /// </returns>
    public NamedAttributeCollection SetOrAdd(NamedAttribute attribute)
    {
        int index = IndexOf(attribute.Name);
        if (index < 0)
        {
            return Add(attribute);
        }

        if (items[index].Equals(attribute))
        {
            return this;
        }

        var newItems = (NamedAttribute[])items.Clone();
        newItems[index] = attribute;
        return new NamedAttributeCollection(newItems);
    }

    /// <summary>
    /// Determines whether the collection contains an attribute with the specified name.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>
    /// <see langword="true"/> if an attribute with the specified name exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public bool Contains(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));
        return IndexOf(name) >= 0;
    }

    /// <summary>
    /// Returns the zero-based index of the attribute with the specified name, or -1 if not found.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>
    /// The zero-based index of the matching attribute, or -1 if no such attribute exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public int IndexOf(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        for (int i = 0; i < items.Length; i++)
        {
            if (string.Equals(items[i].Name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Attempts to retrieve the attribute with the specified name.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="attribute">
    /// When this method returns, contains the matching attribute if found;
    /// otherwise, the default value of <see cref="NamedAttribute"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a matching attribute was found; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public bool TryGet(string name, out NamedAttribute attribute)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        int index = IndexOf(name);
        if (index >= 0)
        {
            attribute = items[index];
            return true;
        }

        attribute = default!;
        return false;
    }

    /// <summary>
    /// Gets the attribute with the specified name.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The matching attribute.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no attribute with the specified name exists.
    /// </exception>
    private NamedAttribute GetRequired(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        if (TryGet(name, out NamedAttribute attribute))
        {
            return attribute;
        }

        throw new KeyNotFoundException($"No attribute named '{name}' was found.");
    }

    /// <inheritdoc/>
    public IEnumerator<NamedAttribute> GetEnumerator() =>
        ((IEnumerable<NamedAttribute>)items).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void ValidateUniqueNames(NamedAttribute[] attributes)
    {
        for (int i = 0; i < attributes.Length; i++)
        {
            string name = attributes[i].Name;

            for (int j = i + 1; j < attributes.Length; j++)
            {
                if (string.Equals(name, attributes[j].Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"The attribute collection contains multiple attributes named '{name}'.",
                        nameof(attributes));
                }
            }
        }
    }
}