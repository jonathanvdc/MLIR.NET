using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

namespace MLIR.Semantics.Types.Collections;

/// <summary>
/// Represents a builtin tensor type.
/// </summary>
public class TensorTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("tensor");

    /// <summary>
    /// Initializes a new parsed builtin tensor type reference.
    /// </summary>
    public TensorTypeReference(TensorTypeSyntax syntax, IReadOnlyList<long?> dimensions, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters)
        : this(dimensions, syntax.IsUnranked, elementType, trailingParameters, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin tensor type reference.
    /// </summary>
    public TensorTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText>? trailingParameters = null)
        : this(dimensions, isUnranked, elementType, trailingParameters ?? [], null, SourceLocation.Unknown)
    {
    }

    /// <summary>Gets the shape dimensions.</summary>
    public IReadOnlyList<long?> Dimensions { get; }
    /// <summary>Gets a value indicating whether the tensor is unranked.</summary>
    public bool IsUnranked { get; }
    /// <summary>Gets the element type.</summary>
    public TypeReference ElementType { get; }
    /// <summary>Gets trailing tensor parameters such as encoding.</summary>
    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; }

    /// <inheritdoc/>
    public override string? Name => "tensor";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private TensorTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(dimensions, isUnranked, elementType, trailingParameters), location)
    {
        Dimensions = dimensions;
        IsUnranked = isUnranked;
        ElementType = elementType;
        TrailingParameters = trailingParameters;
    }

    private static TensorTypeSyntax BuildSyntax(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters)
    {
        var dimensionSyntax = dimensions.Select(CreateDimensionSyntax).ToArray();
        var xTokens = new List<SyntaxToken>(isUnranked ? 1 : dimensionSyntax.Length);
        for (var i = 0; i < xTokens.Capacity; i++)
        {
            xTokens.Add(new SyntaxToken("x"));
        }

        var commas = new List<SyntaxToken>(trailingParameters.Count);
        for (var i = 0; i < trailingParameters.Count; i++)
        {
            commas.Add(new SyntaxToken(","));
        }

        return new TensorTypeSyntax(
            new SyntaxToken("tensor"),
            new SyntaxToken("<"),
            dimensionSyntax,
            xTokens,
            isUnranked ? new SyntaxToken("*") : null,
            elementType.Syntax ?? throw new InvalidOperationException("Tensor element types must carry syntax."),
            commas,
            trailingParameters,
            new SyntaxToken(">"));
    }

    internal static ShapedTypeDimensionSyntax CreateDimensionSyntax(long? dimension)
    {
        return dimension.HasValue
            ? new StaticShapedTypeDimensionSyntax(new SyntaxToken(dimension.Value.ToString()), dimension.Value)
            : new DynamicShapedTypeDimensionSyntax(new SyntaxToken("?"));
    }
}
