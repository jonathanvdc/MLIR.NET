namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a type reference whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownTypeReference : TypeReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownTypeReference"/> class.
    /// </summary>
    public UnknownTypeReference(RawSyntaxText syntax, string? name, TypeDefinition? definition, SourceLocation location)
        : base(syntax, name, definition, location)
    {
    }
}
