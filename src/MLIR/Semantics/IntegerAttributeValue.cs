namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Numerics;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic integer attribute value backed by an arbitrary-precision integer.
/// The value is resolved to a fixed-width <see cref="ApInt"/> at parse time.
/// By default, integer literals without an explicit type annotation are resolved to
/// signless 64-bit integers (<c>i64</c>), matching MLIR's default literal semantics.
/// </summary>
public abstract class IntegerAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerAttributeValue"/> class.
    /// </summary>
    /// <param name="context">The attribute construction context.</param>
    /// <param name="value">The parsed integer value.</param>
    public IntegerAttributeValue(AttributeValueConstructionContext context, ApInt value)
        : base(context.Syntax, context.Location)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="IntegerAttributeValue"/> class with no associated source syntax.
    /// </summary>
    /// <param name="value">The integer value.</param>
    protected IntegerAttributeValue(ApInt value)
        : base(null, SourceLocation.Unknown)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed integer value.
    /// </summary>
    public ApInt Value { get; }
}
