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

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(VectorTypeReference);

    /// <inheritdoc/>
    protected override bool SemanticEqualsValue(TypeReference other)
    {
        var otherVector = (VectorTypeReference)other;
        if (ElementType != otherVector.ElementType || Dimensions.Count != otherVector.Dimensions.Count)
        {
            return false;
        }

        for (var i = 0; i < Dimensions.Count; i++)
        {
            if (Dimensions[i] != otherVector.Dimensions[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    protected override int GetSemanticHashCodeValue()
    {
        unchecked
        {
            return (GetSequenceHashCode(Dimensions) * 397) ^ ElementType.GetHashCode();
        }
    }

    private static VectorTypeSyntax BuildSyntax(IReadOnlyList<long?> dimensions, TypeReference elementType)
    {
        var dimensionSyntax = dimensions.Select(TensorTypeReference.CreateDimensionSyntax).ToArray();
        var xTokens = new List<Token>(dimensionSyntax.Length);
        for (var i = 0; i < dimensionSyntax.Length; i++)
        {
            xTokens.Add(TokenFactory.Identifier("x"));
        }

        return new VectorTypeSyntax(
            TokenFactory.Identifier("vector"),
            TokenFactory.LessThan(),
            dimensionSyntax,
            xTokens,
            elementType.Syntax ?? throw new InvalidOperationException("Vector element types must carry syntax."),
            TokenFactory.GreaterThan());
    }
}
