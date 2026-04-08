using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

namespace MLIR.Semantics.Types.Collections;

/// <summary>
/// Represents a builtin memref type.
/// </summary>
public class MemRefTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("memref");

    /// <summary>
    /// Initializes a new parsed builtin memref type reference.
    /// </summary>
    public MemRefTypeReference(MemRefTypeSyntax syntax, IReadOnlyList<long?> dimensions, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters)
        : this(dimensions, syntax.IsUnranked, elementType, trailingParameters, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin memref type reference.
    /// </summary>
    public MemRefTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText>? trailingParameters = null)
        : this(dimensions, isUnranked, elementType, trailingParameters ?? [], null, SourceLocation.Unknown)
    {
    }

    /// <summary>Gets the shape dimensions.</summary>
    public IReadOnlyList<long?> Dimensions { get; }
    /// <summary>Gets a value indicating whether the memref is unranked.</summary>
    public bool IsUnranked { get; }
    /// <summary>Gets the element type.</summary>
    public TypeReference ElementType { get; }
    /// <summary>Gets trailing memref parameters such as layout and memory space.</summary>
    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; }

    /// <inheritdoc/>
    public override string? Name => "memref";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private MemRefTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(dimensions, isUnranked, elementType, trailingParameters), location)
    {
        Dimensions = dimensions;
        IsUnranked = isUnranked;
        ElementType = elementType;
        TrailingParameters = trailingParameters;
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(MemRefTypeReference);

    /// <inheritdoc/>
    protected override bool SemanticEqualsValue(TypeReference other)
    {
        var otherMemRef = (MemRefTypeReference)other;
        return IsUnranked == otherMemRef.IsUnranked
            && ElementType == otherMemRef.ElementType
            && HaveSameDimensions(Dimensions, otherMemRef.Dimensions)
            && HaveSameTrailingParameters(TrailingParameters, otherMemRef.TrailingParameters);
    }

    /// <inheritdoc/>
    protected override int GetSemanticHashCodeValue()
    {
        unchecked
        {
            var hash = (GetSequenceHashCode(Dimensions) * 397) ^ ElementType.GetHashCode();
            hash = (hash * 397) ^ IsUnranked.GetHashCode();
            for (var i = 0; i < TrailingParameters.Count; i++)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(TrailingParameters[i].Text);
            }

            return hash;
        }
    }

    private static MemRefTypeSyntax BuildSyntax(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters)
    {
        var dimensionSyntax = dimensions.Select(TensorTypeReference.CreateDimensionSyntax).ToArray();
        var xTokens = new List<Token>(isUnranked ? 1 : dimensionSyntax.Length);
        for (var i = 0; i < xTokens.Capacity; i++)
        {
            xTokens.Add(TokenFactory.Identifier("x"));
        }

        var commas = new List<Token>(trailingParameters.Count);
        for (var i = 0; i < trailingParameters.Count; i++)
        {
            commas.Add(TokenFactory.Comma());
        }

        return new MemRefTypeSyntax(
            TokenFactory.Identifier("memref"),
            TokenFactory.LessThan(),
            dimensionSyntax,
            xTokens,
            isUnranked ? TokenFactory.Star() : null,
            elementType.Syntax ?? throw new InvalidOperationException("MemRef element types must carry syntax."),
            commas,
            trailingParameters,
            TokenFactory.GreaterThan());
    }

    private static bool HaveSameDimensions(IReadOnlyList<long?> left, IReadOnlyList<long?> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool HaveSameTrailingParameters(IReadOnlyList<RawSyntaxText> left, IReadOnlyList<RawSyntaxText> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!StringComparer.Ordinal.Equals(left[i].Text, right[i].Text))
            {
                return false;
            }
        }

        return true;
    }
}
