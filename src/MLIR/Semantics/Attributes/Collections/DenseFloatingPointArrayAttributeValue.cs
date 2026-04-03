namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense floating-point-array attribute value (e.g. <c>DenseF32ArrayAttr</c>, <c>DenseF64ArrayAttr</c>).
/// </summary>
public abstract class DenseFloatingPointArrayAttributeValue : DenseArrayAttributeValue<double>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseFloatingPointArrayAttributeValue"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseFloatingPointArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<double> items)
        : base(context, items)
    {
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseFloatingPointArrayAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseFloatingPointArrayAttributeValue(IReadOnlyList<double> items)
        : base(items)
    {
    }
}
