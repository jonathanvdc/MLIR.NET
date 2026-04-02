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
    public AttributeValueConstructionContext(AttributeValueSyntax syntax, string? name, AttributeConstraintDefinition definition, SourceLocation location)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Location = location;
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
}
