namespace MLIR.Semantics.Attributes.Collections;

using System;
using System.Collections.Generic;
using MLIR.Dialects.Builtin;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;

/// <summary>
/// Shared helpers for generated typed-array attribute constraints that use <see cref="ArrayAttr"/> storage.
/// </summary>
public static class ArrayAttrConstraintHelpers
{
    /// <summary>
    /// Builds an <see cref="ArrayAttr"/> from parsed array syntax.
    /// </summary>
    public static ArrayAttr BindFromSyntax(AttributeValueSyntax? syntax, Binder? binder = null)
    {
        return syntax is ArrayAttributeValueSyntax arraySyntax
            ? new ArrayAttr(BindItemsFromSyntax(arraySyntax.Items.Items, binder), arraySyntax)
            : new ArrayAttr(Array.Empty<AttributeValue>(), syntax);
    }

    /// <summary>
    /// Binds array item syntax through the normal attribute binder.
    /// </summary>
    /// <param name="items">The item syntax nodes in the array.</param>
    /// <param name="binder">The binder to use, or <see langword="null"/> to use a syntax-only binder.</param>
    /// <returns>The bound item values.</returns>
    public static IReadOnlyList<AttributeValue> BindItemsFromSyntax(IReadOnlyList<AttributeValueSyntax> items, Binder? binder = null)
    {
        binder ??= new Binder(null);
        return binder.BindAttributeValues(items);
    }

    /// <summary>
    /// Encodes a typed list into <see cref="ArrayAttr"/> storage.
    /// </summary>
    public static ArrayAttr Create<TElement>(IReadOnlyList<TElement> items, Func<TElement, AttributeValue> encoder)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (encoder is null)
        {
            throw new ArgumentNullException(nameof(encoder));
        }

        var storageItems = new List<AttributeValue>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            storageItems.Add(encoder(items[i]));
        }

        return new ArrayAttr(storageItems);
    }

    /// <summary>
    /// Decodes typed items from an array-style attribute value.
    /// </summary>
    public static IReadOnlyList<TElement> GetItems<TElement>(AttributeValue attribute, Func<AttributeValue, TElement> decoder)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        if (decoder is null)
        {
            throw new ArgumentNullException(nameof(decoder));
        }

        var storageItems = attribute is ArrayAttr arrayAttr
            ? arrayAttr.Value
            : DecodeStorageItemsFromSyntax(attribute.Syntax);

        var items = new List<TElement>(storageItems.Count);
        for (var i = 0; i < storageItems.Count; i++)
        {
            items.Add(decoder(storageItems[i]));
        }

        return items;
    }

    private static IReadOnlyList<AttributeValue> DecodeStorageItemsFromSyntax(AttributeValueSyntax? syntax)
    {
        return syntax is ArrayAttributeValueSyntax arraySyntax
            ? BindItemsFromSyntax(arraySyntax.Items.Items)
            : Array.Empty<AttributeValue>();
    }
}
