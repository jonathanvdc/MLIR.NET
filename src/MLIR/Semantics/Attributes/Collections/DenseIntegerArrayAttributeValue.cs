namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Numerics;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense integer-array attribute value (e.g. <c>DenseI32ArrayAttr</c>).
/// </summary>
public abstract class DenseIntegerArrayAttributeValue : DenseArrayAttributeValue<ApInt>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseIntegerArrayAttributeValue"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseIntegerArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<ApInt> items)
        : base(context, items)
    {
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseIntegerArrayAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseIntegerArrayAttributeValue(IReadOnlyList<ApInt> items)
        : base(items)
    {
    }
}
