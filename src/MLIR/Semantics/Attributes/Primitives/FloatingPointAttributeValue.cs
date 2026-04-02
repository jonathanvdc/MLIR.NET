namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic floating-point attribute value.
/// </summary>
public abstract class FloatingPointAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingPointAttributeValue"/> class.
    /// </summary>
    public FloatingPointAttributeValue(AttributeValueConstructionContext context, string literalText)
        : base(context.Syntax, context.Location)
    {
        LiteralText = literalText;
    }

    /// <summary>
    /// Gets the normalized literal text.
    /// </summary>
    public string LiteralText { get; }
}
