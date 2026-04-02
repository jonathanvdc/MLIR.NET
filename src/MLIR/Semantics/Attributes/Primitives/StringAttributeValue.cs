namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a semantic string attribute value.
/// </summary>
public class StringAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringAttributeValue"/> class.
    /// </summary>
    public StringAttributeValue(AttributeValueConstructionContext context, string value)
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
    /// Gets the unescaped string value.
    /// </summary>
    public string Value { get; }
}
