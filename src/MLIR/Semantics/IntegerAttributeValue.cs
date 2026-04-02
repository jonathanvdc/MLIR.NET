namespace MLIR.Semantics.Attributes.Primitives;

using System.Numerics;
using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic integer attribute value backed by an arbitrary-precision integer.
/// </summary>
public class IntegerAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerAttributeValue"/> class.
    /// </summary>
    /// <param name="context">The attribute construction context.</param>
    /// <param name="value">The parsed integer value.</param>
    public IntegerAttributeValue(AttributeValueConstructionContext context, BigInteger value)
        : base(context.Syntax, context.Location)
    {
        Name = context.Name;
        Definition = context.Definition;
        Value = value;
    }

    /// <inheritdoc/>
    public override string? Name { get; }

    /// <inheritdoc/>
    public override AttributeConstraintDefinition? Definition { get; }

    /// <summary>
    /// Gets the parsed integer value.
    /// </summary>
    public BigInteger Value { get; }
}
