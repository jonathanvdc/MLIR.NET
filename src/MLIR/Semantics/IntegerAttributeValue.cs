namespace MLIR.Semantics.Attributes.Primitives;

using System.Numerics;
using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic integer attribute value backed by an arbitrary-precision integer.
/// </summary>
public abstract class IntegerAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerAttributeValue"/> class.
    /// </summary>
    /// <param name="context">The attribute construction context.</param>
    /// <param name="value">The parsed integer value.</param>
    public IntegerAttributeValue(AttributeValueConstructionContext context, BigInteger value)
        : base(context.Syntax, context.Location)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="IntegerAttributeValue"/> class with no associated source syntax.
    /// </summary>
    /// <param name="value">The integer value.</param>
    protected IntegerAttributeValue(BigInteger value)
        : base(null, SourceLocation.Unknown)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed integer value.
    /// </summary>
    public BigInteger Value { get; }
}
