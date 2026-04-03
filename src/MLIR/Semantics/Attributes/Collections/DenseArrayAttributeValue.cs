namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense-array attribute value whose elements are decoded as <typeparamref name="TElement"/>.
/// </summary>
public abstract class DenseArrayAttributeValue<TElement> : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseArrayAttributeValue{TElement}"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<TElement> items)
        : base(context.Syntax, context.Location)
    {
        Items = items;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseArrayAttributeValue{TElement}"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseArrayAttributeValue(IReadOnlyList<TElement> items)
        : base(null, SourceLocation.Unknown)
    {
        Items = items;
    }

    /// <summary>
    /// Gets the decoded element values.
    /// </summary>
    public IReadOnlyList<TElement> Items { get; }
}
