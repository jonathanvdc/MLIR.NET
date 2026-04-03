namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense single-precision floating-point array attribute value (<c>DenseF32ArrayAttr</c>).
/// </summary>
public abstract class DenseF32ArrayAttributeValue : DenseArrayAttributeValue<float>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseF32ArrayAttributeValue"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseF32ArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<float> items)
        : base(context, items)
    {
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseF32ArrayAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseF32ArrayAttributeValue(IReadOnlyList<float> items)
        : base(items)
    {
    }
}
