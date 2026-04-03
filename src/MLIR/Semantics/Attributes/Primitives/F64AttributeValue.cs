namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic 64-bit floating-point attribute value.
/// </summary>
public abstract class F64AttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="F64AttributeValue"/> class.
    /// </summary>
    public F64AttributeValue(AttributeValueConstructionContext context, double value)
        : base(context.Syntax, context.Location)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="F64AttributeValue"/> class with no associated source syntax.
    /// </summary>
    /// <param name="value">The parsed double-precision floating-point value.</param>
    protected F64AttributeValue(double value)
        : base(null, SourceLocation.Unknown)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed double-precision floating-point value.
    /// </summary>
    public double Value { get; }
}
