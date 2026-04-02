namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic boolean attribute value.
/// </summary>
public class BooleanAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanAttributeValue"/> class.
    /// </summary>
    public BooleanAttributeValue(AttributeValueConstructionContext context, bool value)
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
    /// Gets the parsed boolean value.
    /// </summary>
    public bool Value { get; }
}
