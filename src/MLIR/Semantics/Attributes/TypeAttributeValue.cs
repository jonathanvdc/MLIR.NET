namespace MLIR.Semantics.Attributes;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic type attribute value.
/// </summary>
public abstract class TypeAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeAttributeValue"/> class.
    /// </summary>
    protected TypeAttributeValue(AttributeValueConstructionContext context, TypeSyntax typeSyntax)
        : base(context.Syntax, context.Location)
    {
        TypeSyntax = typeSyntax;
    }

    /// <summary>
    /// Initializes a new synthetic instance of the <see cref="TypeAttributeValue"/> class
    /// with no associated source syntax.
    /// </summary>
    protected TypeAttributeValue(TypeSyntax typeSyntax)
        : base(null, SourceLocation.Unknown)
    {
        TypeSyntax = typeSyntax;
    }

    /// <summary>
    /// Gets the referenced type syntax.
    /// </summary>
    public TypeSyntax TypeSyntax { get; }
}
