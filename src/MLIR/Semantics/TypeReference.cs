namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic type reference bound from concrete syntax.
/// </summary>
public abstract class TypeReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeReference"/> class.
    /// </summary>
    protected TypeReference(TypeSyntax? syntax, SourceLocation location)
    {
        Syntax = syntax;
        Location = location;
    }

    /// <summary>
    /// Gets the syntax for the type, or <see langword="null"/> if this is a synthetic type with no preserved source text.
    /// </summary>
    public TypeSyntax? Syntax { get; }

    /// <summary>
    /// Gets the canonical type name, if one was recognized.
    /// </summary>
    public abstract string? Name { get; }

    /// <summary>
    /// Gets the registered definition, if one exists.
    /// </summary>
    public abstract TypeDefinition? Definition { get; }

    /// <summary>
    /// Gets a value indicating whether the type was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

    /// <summary>
    /// Gets the source location of the type text, if known.
    /// </summary>
    public SourceLocation Location { get; }
}
