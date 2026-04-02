namespace MLIR.Semantics.Attributes.Collections;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic elements attribute value.
/// </summary>
public abstract class ElementsAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElementsAttributeValue"/> class.
    /// </summary>
    protected ElementsAttributeValue(AttributeValueConstructionContext context, AttributeValue payload, TypeSyntax typeSyntax)
        : base(context.Syntax, context.Location)
    {
        Payload = payload;
        TypeSyntax = typeSyntax;
    }

    /// <summary>
    /// Gets the decoded payload.
    /// </summary>
    public AttributeValue Payload { get; }

    /// <summary>
    /// Gets the trailing type syntax.
    /// </summary>
    public TypeSyntax TypeSyntax { get; }
}
