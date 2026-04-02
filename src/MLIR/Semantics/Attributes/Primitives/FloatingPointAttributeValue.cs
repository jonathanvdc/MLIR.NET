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
    /// Initializes a new synthetic instance of the <see cref="FloatingPointAttributeValue"/> class with no associated source syntax.
    /// </summary>
    /// <param name="literalText">The normalized literal text.</param>
    protected FloatingPointAttributeValue(string literalText)
        : base(null, SourceLocation.Unknown)
    {
        LiteralText = literalText;
    }

    /// <summary>
    /// Gets the normalized literal text.
    /// </summary>
    public string LiteralText { get; }
}
