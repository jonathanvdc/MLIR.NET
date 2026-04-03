namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic dense boolean-array attribute value (<c>DenseBoolArrayAttr</c>).
/// </summary>
public abstract class DenseBooleanArrayAttributeValue : DenseArrayAttributeValue<bool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenseBooleanArrayAttributeValue"/> class
    /// from a parsed construction context.
    /// </summary>
    protected DenseBooleanArrayAttributeValue(AttributeValueConstructionContext context, IReadOnlyList<bool> items)
        : base(context, items)
    {
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="DenseBooleanArrayAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected DenseBooleanArrayAttributeValue(IReadOnlyList<bool> items)
        : base(items)
    {
    }
}
