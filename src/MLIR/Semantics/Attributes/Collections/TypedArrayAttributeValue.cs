namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic array-style attribute value whose items are strongly typed.
/// </summary>
/// <typeparam name="TElement">The item payload type.</typeparam>
public abstract class TypedArrayAttributeValue<TElement> : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedArrayAttributeValue{TElement}"/> class
    /// from a parsed construction context.
    /// </summary>
    protected TypedArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<TElement> items)
        : base(context.Syntax, context.Location)
    {
        Items = items;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="TypedArrayAttributeValue{TElement}"/> class
    /// with no associated source syntax.
    /// </summary>
    protected TypedArrayAttributeValue(IReadOnlyList<TElement> items)
        : base(null, SourceLocation.Unknown)
    {
        Items = items;
    }

    /// <summary>
    /// Gets the decoded items.
    /// </summary>
    public IReadOnlyList<TElement> Items { get; }
}
