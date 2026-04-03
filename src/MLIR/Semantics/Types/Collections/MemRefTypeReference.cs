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

    private static MemRefTypeSyntax BuildSyntax(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters)
    {
        var dimensionSyntax = dimensions.Select(TensorTypeReference.CreateDimensionSyntax).ToArray();
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

        return new MemRefTypeSyntax(
            new SyntaxToken("memref"),
            new SyntaxToken("<"),
            dimensionSyntax,
            xTokens,
            isUnranked ? new SyntaxToken("*") : null,
            elementType.Syntax ?? throw new InvalidOperationException("MemRef element types must carry syntax."),
            commas,
            trailingParameters,
            new SyntaxToken(">"));
    }
}
