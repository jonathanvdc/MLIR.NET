namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic 32-bit floating-point attribute value.
/// </summary>
public abstract class F32AttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="F32AttributeValue"/> class.
    /// </summary>
    public F32AttributeValue(AttributeValueConstructionContext context, float value)
        : base(context.Syntax, context.Location)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="F32AttributeValue"/> class with no associated source syntax.
    /// </summary>
    /// <param name="value">The parsed single-precision floating-point value.</param>
    protected F32AttributeValue(float value)
        : base(null, SourceLocation.Unknown)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed single-precision floating-point value.
    /// </summary>
    public float Value { get; }
}
