namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Numerics;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic floating-point attribute value.
/// </summary>
public abstract class FloatingPointAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingPointAttributeValue"/> class.
    /// </summary>
    public FloatingPointAttributeValue(AttributeValueConstructionContext context, ApFloat value)
        : base(context.Syntax, context.Location)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="FloatingPointAttributeValue"/> class with no associated source syntax.
    /// </summary>
    /// <param name="value">The parsed floating-point value.</param>
    protected FloatingPointAttributeValue(ApFloat value)
        : base(null, SourceLocation.Unknown)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed floating-point value, including its semantics.
    /// </summary>
    public ApFloat Value { get; }
}
