namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic floating-point attribute value.
/// </summary>
public class FloatingPointAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingPointAttributeValue"/> class.
    /// </summary>
    public FloatingPointAttributeValue(AttributeValueConstructionContext context, string literalText)
        : base(context.Syntax, context.Location)
    {
        Name = context.Name;
        Definition = context.Definition;
        LiteralText = literalText;
    }

    /// <inheritdoc/>
    public override string? Name { get; }

    /// <inheritdoc/>
    public override AttributeConstraintDefinition? Definition { get; }

    /// <summary>
    /// Gets the normalized literal text.
    /// </summary>
    public string LiteralText { get; }
}
