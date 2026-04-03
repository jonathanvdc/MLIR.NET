namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense double-precision floating-point array attribute value (<c>DenseF64ArrayAttr</c>).
/// </summary>
public abstract class DenseF64ArrayAttributeValue : DenseArrayAttributeValue<double>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseF64ArrayAttributeValue"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseF64ArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<double> items)
        : base(context, items)
    {
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseF64ArrayAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseF64ArrayAttributeValue(IReadOnlyList<double> items)
        : base(items)
    {
    }
}
