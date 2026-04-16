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
    public static TypeDefinition TypeDefinition { get; } = new("tensor", new MLIR.Dialects.Builtin.BuiltinTensorTypeAssemblyFormat());

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
        : base(syntax, location)
    {
        Dimensions = dimensions;
        IsUnranked = isUnranked;
        ElementType = elementType;
        TrailingParameters = trailingParameters;
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(TensorTypeReference);

    /// <inheritdoc/>
    protected override bool SemanticEqualsValue(TypeReference other)
    {
        var otherTensor = (TensorTypeReference)other;
        return IsUnranked == otherTensor.IsUnranked
            && ElementType == otherTensor.ElementType
            && HaveSameDimensions(Dimensions, otherTensor.Dimensions)
            && HaveSameTrailingParameters(TrailingParameters, otherTensor.TrailingParameters);
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

    internal static ShapedTypeDimensionSyntax CreateDimensionSyntax(long? dimension)
    {
        return dimension.HasValue
            ? new StaticShapedTypeDimensionSyntax(TokenFactory.Integer(dimension.Value.ToString()), dimension.Value)
            : new DynamicShapedTypeDimensionSyntax(TokenFactory.Question());
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
