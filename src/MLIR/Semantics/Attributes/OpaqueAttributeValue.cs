namespace MLIR.Semantics.Attributes;

using MLIR.Semantics;

/// <summary>
/// Represents an attribute value with known constraint identity but opaque internal structure.
/// </summary>
public abstract class OpaqueAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpaqueAttributeValue"/> class.
    /// </summary>
    protected OpaqueAttributeValue(AttributeValueConstructionContext context)
        : base(context.Syntax, context.Location)
    {
    }
}
