namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic array-style attribute value.
/// </summary>
public abstract class ArrayAttributeValue : CollectionAttributeValue<AttributeValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayAttributeValue"/> class.
    /// </summary>
    protected ArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<AttributeValue> items)
        : base(context, items)
    {
    }
}
