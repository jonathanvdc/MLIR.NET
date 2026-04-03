namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense double-precision floating-point array attribute value (<c>DenseF64ArrayAttr</c>).
/// </summary>
public abstract class DenseDoublePrecisionArrayAttributeValue : DenseArrayAttributeValue<double>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseDoublePrecisionArrayAttributeValue"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseDoublePrecisionArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<double> items)
        : base(context, items)
    {
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseDoublePrecisionArrayAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseDoublePrecisionArrayAttributeValue(IReadOnlyList<double> items)
        : base(items)
    {
    }
}
