namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic boolean attribute value.
/// </summary>
public abstract class BooleanAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanAttributeValue"/> class.
    /// </summary>
    public BooleanAttributeValue(AttributeValueConstructionContext context, bool value)
        : base(context.Syntax, context.Location)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="BooleanAttributeValue"/> class with no associated source syntax.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    protected BooleanAttributeValue(bool value)
        : base(null, SourceLocation.Unknown)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed boolean value.
    /// </summary>
    public bool Value { get; }
}
