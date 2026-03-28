namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic type reference bound from raw syntax text.
/// </summary>
public abstract class TypeReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeReference"/> class.
    /// </summary>
    protected TypeReference(RawSyntaxText syntax, string? name, TypeDefinition? definition, SourceLocation location)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Location = location;
    }

    /// <summary>
    /// Gets the raw syntax text for the type.
    /// </summary>
    public RawSyntaxText Syntax { get; }

    /// <summary>
    /// Gets the canonical type name, if one was recognized.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the registered definition, if one exists.
    /// </summary>
    public TypeDefinition? Definition { get; }

    /// <summary>
    /// Gets a value indicating whether the type was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

    /// <summary>
    /// Gets the source location of the type text, if known.
    /// </summary>
    public SourceLocation Location { get; }
}
