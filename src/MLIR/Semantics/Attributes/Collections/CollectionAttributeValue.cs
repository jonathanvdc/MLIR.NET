namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic collection-style attribute value.
/// </summary>
public abstract class CollectionAttributeValue<TElement> : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionAttributeValue{TElement}"/> class.
    /// </summary>
    protected CollectionAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<TElement> items)
        : base(context.Syntax, context.Location)
    {
        Items = items;
    }

    /// <summary>
    /// Gets the decoded items.
    /// </summary>
    public IReadOnlyList<TElement> Items { get; }
}
