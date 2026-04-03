namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Carries the shared semantic state needed to construct a typed type-reference node.
/// </summary>
public sealed class TypeReferenceConstructionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeReferenceConstructionContext"/> class.
    /// </summary>
    /// <param name="syntax">The type syntax.</param>
    /// <param name="name">The canonical type name, if one was recognized.</param>
    /// <param name="definition">The registered type definition.</param>
    /// <param name="location">The source location of the type syntax.</param>
    public TypeReferenceConstructionContext(TypeSyntax? syntax, string? name, TypeDefinition definition, SourceLocation location)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Location = location;
    }

    /// <summary>
    /// Gets the raw syntax text for the type.
    /// </summary>
    public TypeSyntax? Syntax { get; }

    /// <summary>
    /// Gets the canonical type name, if one was recognized.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the registered type definition.
    /// </summary>
    public TypeDefinition Definition { get; }

    /// <summary>
    /// Gets the source location of the type text, if known.
    /// </summary>
    public SourceLocation Location { get; }
}
