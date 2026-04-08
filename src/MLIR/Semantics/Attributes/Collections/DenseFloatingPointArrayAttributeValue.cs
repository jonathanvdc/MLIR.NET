namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Numerics;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense floating-point array attribute value whose elements are preserved as <see cref="ApFloat"/> values.
/// </summary>
public abstract class DenseFloatingPointArrayAttributeValue : DenseArrayAttributeValue<ApFloat>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseFloatingPointArrayAttributeValue"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseFloatingPointArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<ApFloat> items)
        : base(context, items)
    {
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseFloatingPointArrayAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseFloatingPointArrayAttributeValue(IReadOnlyList<ApFloat> items)
        : base(items)
    {
    }
}
