using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

namespace MLIR.Semantics.Types.Collections;

/// <summary>
/// Represents a builtin vector type.
/// </summary>
public class VectorTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("vector");

    /// <summary>
    /// Initializes a new parsed builtin vector type reference.
    /// </summary>
    public VectorTypeReference(VectorTypeSyntax syntax, IReadOnlyList<long?> dimensions, TypeReference elementType)
        : this(dimensions, elementType, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin vector type reference.
    /// </summary>
    public VectorTypeReference(IReadOnlyList<long?> dimensions, TypeReference elementType)
        : this(dimensions, elementType, null, SourceLocation.Unknown)
    {
    }

    /// <summary>Gets the shape dimensions.</summary>
    public IReadOnlyList<long?> Dimensions { get; }
    /// <summary>Gets the element type.</summary>
    public TypeReference ElementType { get; }

    /// <inheritdoc/>
    public override string? Name => "vector";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private VectorTypeReference(IReadOnlyList<long?> dimensions, TypeReference elementType, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(dimensions, elementType), location)
    {
        Dimensions = dimensions;
        ElementType = elementType;
    }

    private static VectorTypeSyntax BuildSyntax(IReadOnlyList<long?> dimensions, TypeReference elementType)
    {
        var dimensionSyntax = dimensions.Select(TensorTypeReference.CreateDimensionSyntax).ToArray();
        var xTokens = new List<SyntaxToken>(dimensionSyntax.Length);
        for (var i = 0; i < dimensionSyntax.Length; i++)
        {
            xTokens.Add(new SyntaxToken("x"));
        }

        return new VectorTypeSyntax(
            new SyntaxToken("vector"),
            new SyntaxToken("<"),
            dimensionSyntax,
            xTokens,
            elementType.Syntax ?? throw new InvalidOperationException("Vector element types must carry syntax."),
            new SyntaxToken(">"));
    }
}
