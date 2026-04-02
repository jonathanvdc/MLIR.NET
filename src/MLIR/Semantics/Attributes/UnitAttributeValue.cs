namespace MLIR.Semantics.Attributes;

using MLIR.Semantics;

/// <summary>
/// Represents a semantic unit attribute value.
/// </summary>
public abstract class UnitAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitAttributeValue"/> class.
    /// </summary>
    protected UnitAttributeValue(AttributeValueConstructionContext context)
        : base(context.Syntax, context.Location)
    {
    }
}
