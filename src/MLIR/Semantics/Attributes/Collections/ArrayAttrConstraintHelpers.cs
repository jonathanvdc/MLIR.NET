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
    public static ArrayAttr BindFromSyntax(AttributeValueSyntax? syntax)
    {
        return syntax is ArrayAttributeValueSyntax arraySyntax
            ? new ArrayAttr(StructuredAttributeSemanticDecoder.DecodeItems(arraySyntax.Items.Items), arraySyntax)
            : new ArrayAttr(Array.Empty<AttributeValue>(), syntax);
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
            ? StructuredAttributeSemanticDecoder.DecodeItems(arraySyntax.Items.Items)
            : Array.Empty<AttributeValue>();
    }
}
