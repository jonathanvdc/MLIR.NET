namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a type reference whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownTypeReference : TypeReference
{
    private readonly string? rawText;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownTypeReference"/> class.
    /// </summary>
    public UnknownTypeReference(TypeSyntax? syntax, string? name, TypeDefinition? definition)
        : base(syntax)
    {
        Name = name;
        Definition = definition;
        rawText = syntax?.ToString();
    }

    /// <inheritdoc/>
    public override string? Name { get; }

    /// <inheritdoc/>
    public override TypeDefinition? Definition { get; }

    /// <inheritdoc/>
    protected override bool SemanticEqualsValue(TypeReference other)
    {
        var otherUnknown = (UnknownTypeReference)other;
        return string.Equals(Name, otherUnknown.Name, System.StringComparison.Ordinal)
            && string.Equals(rawText, otherUnknown.rawText, System.StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    protected override int GetSemanticHashCodeValue()
    {
        unchecked
        {
            return ((Name != null ? System.StringComparer.Ordinal.GetHashCode(Name) : 0) * 397)
                ^ (rawText != null ? System.StringComparer.Ordinal.GetHashCode(rawText) : 0);
        }
    }
}
