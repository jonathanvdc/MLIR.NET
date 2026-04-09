namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Carries the shared semantic state needed to construct a typed attribute value node.
/// </summary>
public sealed class AttributeValueConstructionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeValueConstructionContext"/> class.
    /// </summary>
    /// <param name="syntax">The attribute-value syntax.</param>
    /// <param name="name">The canonical attribute name, if one was recognized.</param>
    /// <param name="definition">The registered attribute constraint definition.</param>
    /// <param name="location">The source location of the attribute value.</param>
    /// <param name="selfType">The semantic self-type, if one was pre-bound from nested type syntax.</param>
    /// <param name="binder">The binder that produced this context, if one is available.</param>
    public AttributeValueConstructionContext(
        AttributeValueSyntax syntax,
        string? name,
        AttributeConstraintDefinition definition,
        SourceLocation location,
        TypeReference? selfType = null,
        Binder? binder = null)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Location = location;
        SelfType = selfType;
        Binder = binder;
    }

    /// <summary>
    /// Gets the syntax for the attribute value.
    /// </summary>
    public AttributeValueSyntax Syntax { get; }

    /// <summary>
    /// Gets the canonical attribute name, if one was recognized.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the registered attribute constraint definition.
    /// </summary>
    public AttributeConstraintDefinition Definition { get; }

    /// <summary>
    /// Gets the source location of the attribute value, if known.
    /// </summary>
    public SourceLocation Location { get; }

    /// <summary>
    /// Gets the semantic self-type, if one was pre-bound from nested type syntax.
    /// This is used for attribute parameters declared with <c>AttributeSelfTypeParameter</c>.
    /// </summary>
    public TypeReference? SelfType { get; }

    /// <summary>
    /// Gets the binder that produced this context, if one is available.
    /// This is useful for generated binders that need access to semantic helpers while
    /// reconstructing parameters from syntax.
    /// </summary>
    public Binder? Binder { get; }
}
