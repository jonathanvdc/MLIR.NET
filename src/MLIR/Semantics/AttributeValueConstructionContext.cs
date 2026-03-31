namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Carries the shared semantic state needed to construct a typed attribute value node.
/// </summary>
public sealed class AttributeValueConstructionContext
{
    internal AttributeValueConstructionContext(AttributeValueSyntax syntax, string? name, AttributeDefinition definition, SourceLocation location)
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
    /// Gets the registered attribute definition.
    /// </summary>
    public AttributeDefinition Definition { get; }

    /// <summary>
    /// Gets the source location of the attribute value, if known.
    /// </summary>
    public SourceLocation Location { get; }
}
