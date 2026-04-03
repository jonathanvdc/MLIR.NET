namespace MLIR.Semantics;

using System;
using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a builtin integer type such as <c>i32</c> or <c>si64</c>.
/// </summary>
public sealed class BuiltinIntegerTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("integer");

    /// <summary>
    /// Initializes a new parsed builtin integer type reference.
    /// </summary>
    public BuiltinIntegerTypeReference(BuiltinIntegerTypeSyntax syntax)
        : this(syntax.Signedness, syntax.Width, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin integer type reference.
    /// </summary>
    public BuiltinIntegerTypeReference(IntegerTypeSignedness signedness, int width)
        : this(signedness, width, null, SourceLocation.Unknown)
    {
    }

    private BuiltinIntegerTypeReference(IntegerTypeSignedness signedness, int width, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(signedness, width), location)
    {
        Signedness = signedness;
        Width = width;
    }

    public IntegerTypeSignedness Signedness { get; }

    public int Width { get; }

    public override string? Name => Signedness switch
    {
        IntegerTypeSignedness.Signed => "si" + Width,
        IntegerTypeSignedness.Unsigned => "ui" + Width,
        _ => "i" + Width,
    };

    public override TypeDefinition? Definition => TypeDefinition;

    private static BuiltinIntegerTypeSyntax BuildSyntax(IntegerTypeSignedness signedness, int width)
    {
        var text = signedness switch
        {
            IntegerTypeSignedness.Signed => "si" + width,
            IntegerTypeSignedness.Unsigned => "ui" + width,
            _ => "i" + width,
        };
        return new BuiltinIntegerTypeSyntax(new SyntaxToken(text), signedness, width);
    }
}

/// <summary>
/// Represents a builtin floating-point type such as <c>f32</c>.
/// </summary>
public sealed class BuiltinFloatTypeReference : TypeReference
{
    public static TypeDefinition TypeDefinition { get; } = new("float");

    public BuiltinFloatTypeReference(BuiltinFloatTypeSyntax syntax)
        : this(syntax.Name, syntax, syntax.Location)
    {
    }

    public BuiltinFloatTypeReference(string name)
        : this(name, null, SourceLocation.Unknown)
    {
    }

    private BuiltinFloatTypeReference(string name, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinFloatTypeSyntax(new SyntaxToken(name)), location)
    {
        Name = name;
    }

    public override string? Name { get; }

    public override TypeDefinition? Definition => TypeDefinition;
}

/// <summary>
/// Represents the builtin <c>index</c> type.
/// </summary>
public sealed class BuiltinIndexTypeReference : TypeReference
{
    public static TypeDefinition TypeDefinition { get; } = new("index");

    public BuiltinIndexTypeReference(BuiltinIndexTypeSyntax syntax)
        : this(syntax, syntax.Location)
    {
    }

    public BuiltinIndexTypeReference()
        : this(null, SourceLocation.Unknown)
    {
    }

    private BuiltinIndexTypeReference(TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinIndexTypeSyntax(new SyntaxToken("index")), location)
    {
    }

    public override string? Name => "index";

    public override TypeDefinition? Definition => TypeDefinition;
}

/// <summary>
/// Represents a builtin tuple type.
/// </summary>
public sealed class TupleTypeReference : TypeReference
{
    public static TypeDefinition TypeDefinition { get; } = new("tuple");

    public TupleTypeReference(TupleTypeSyntax syntax, IReadOnlyList<TypeReference> elements)
        : this(elements, syntax, syntax.Location)
    {
    }

    public TupleTypeReference(IReadOnlyList<TypeReference> elements)
        : this(elements, null, SourceLocation.Unknown)
    {
    }

    private TupleTypeReference(IReadOnlyList<TypeReference> elements, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(elements), location)
    {
        Elements = elements;
    }

    public IReadOnlyList<TypeReference> Elements { get; }

    public override string? Name => "tuple";

    public override TypeDefinition? Definition => TypeDefinition;

    private static TupleTypeSyntax BuildSyntax(IReadOnlyList<TypeReference> elements)
    {
        var commas = new List<SyntaxToken>(Math.Max(0, elements.Count - 1));
        for (var i = 1; i < elements.Count; i++)
        {
            commas.Add(new SyntaxToken(","));
        }

        return new TupleTypeSyntax(
            new SyntaxToken("tuple"),
            new SyntaxToken("<"),
            elements.Select(static element => element.Syntax ?? throw new InvalidOperationException("Tuple elements must carry syntax.")).ToArray(),
            commas,
            new SyntaxToken(">"));
    }
}

/// <summary>
/// Represents a builtin function type.
/// </summary>
public sealed class FunctionTypeReference : TypeReference
{
    public static TypeDefinition TypeDefinition { get; } = new("function");

    public FunctionTypeReference(FunctionTypeSyntax syntax, IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
        : this(inputs, results, syntax, syntax.Location)
    {
    }

    public FunctionTypeReference(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
        : this(inputs, results, null, SourceLocation.Unknown)
    {
    }

    private FunctionTypeReference(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(inputs, results), location)
    {
        Inputs = inputs;
        Results = results;
    }

    public IReadOnlyList<TypeReference> Inputs { get; }

    public IReadOnlyList<TypeReference> Results { get; }

    public override string? Name => "function";

    public override TypeDefinition? Definition => TypeDefinition;

    private static FunctionTypeSyntax BuildSyntax(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
    {
        var inputCommas = new List<SyntaxToken>(Math.Max(0, inputs.Count - 1));
        for (var i = 1; i < inputs.Count; i++)
        {
            inputCommas.Add(new SyntaxToken(","));
        }

        if (results.Count == 1)
        {
            return new FunctionTypeSyntax(
                new DelimitedSyntaxList<TypeSyntax>(new SyntaxToken("("), inputs.Select(GetSyntax).ToArray(), inputCommas, new SyntaxToken(")")),
                new SyntaxToken("->"),
                GetSyntax(results[0]),
                new DelimitedSyntaxList<TypeSyntax>(null, [], [], null));
        }

        var resultCommas = new List<SyntaxToken>(Math.Max(0, results.Count - 1));
        for (var i = 1; i < results.Count; i++)
        {
            resultCommas.Add(new SyntaxToken(","));
        }

        return new FunctionTypeSyntax(
            new DelimitedSyntaxList<TypeSyntax>(new SyntaxToken("("), inputs.Select(GetSyntax).ToArray(), inputCommas, new SyntaxToken(")")),
            new SyntaxToken("->"),
            null,
            new DelimitedSyntaxList<TypeSyntax>(new SyntaxToken("("), results.Select(GetSyntax).ToArray(), resultCommas, new SyntaxToken(")")));
    }

    private static TypeSyntax GetSyntax(TypeReference type)
    {
        return type.Syntax ?? throw new InvalidOperationException("Function operand types must carry syntax.");
    }
}

/// <summary>
/// Represents a builtin tensor type.
/// </summary>
public sealed class TensorTypeReference : TypeReference
{
    public static TypeDefinition TypeDefinition { get; } = new("tensor");

    public TensorTypeReference(TensorTypeSyntax syntax, IReadOnlyList<long?> dimensions, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters)
        : this(dimensions, syntax.IsUnranked, elementType, trailingParameters, syntax, syntax.Location)
    {
    }

    public TensorTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText>? trailingParameters = null)
        : this(dimensions, isUnranked, elementType, trailingParameters ?? [], null, SourceLocation.Unknown)
    {
    }

    private TensorTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(dimensions, isUnranked, elementType, trailingParameters), location)
    {
        Dimensions = dimensions;
        IsUnranked = isUnranked;
        ElementType = elementType;
        TrailingParameters = trailingParameters;
    }

    public IReadOnlyList<long?> Dimensions { get; }

    public bool IsUnranked { get; }

    public TypeReference ElementType { get; }

    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; }

    public override string? Name => "tensor";

    public override TypeDefinition? Definition => TypeDefinition;

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

/// <summary>
/// Represents a builtin vector type.
/// </summary>
public sealed class VectorTypeReference : TypeReference
{
    public static TypeDefinition TypeDefinition { get; } = new("vector");

    public VectorTypeReference(VectorTypeSyntax syntax, IReadOnlyList<long?> dimensions, TypeReference elementType)
        : this(dimensions, elementType, syntax, syntax.Location)
    {
    }

    public VectorTypeReference(IReadOnlyList<long?> dimensions, TypeReference elementType)
        : this(dimensions, elementType, null, SourceLocation.Unknown)
    {
    }

    private VectorTypeReference(IReadOnlyList<long?> dimensions, TypeReference elementType, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(dimensions, elementType), location)
    {
        Dimensions = dimensions;
        ElementType = elementType;
    }

    public IReadOnlyList<long?> Dimensions { get; }

    public TypeReference ElementType { get; }

    public override string? Name => "vector";

    public override TypeDefinition? Definition => TypeDefinition;

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

/// <summary>
/// Represents a builtin memref type.
/// </summary>
public sealed class MemRefTypeReference : TypeReference
{
    public static TypeDefinition TypeDefinition { get; } = new("memref");

    public MemRefTypeReference(MemRefTypeSyntax syntax, IReadOnlyList<long?> dimensions, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters)
        : this(dimensions, syntax.IsUnranked, elementType, trailingParameters, syntax, syntax.Location)
    {
    }

    public MemRefTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText>? trailingParameters = null)
        : this(dimensions, isUnranked, elementType, trailingParameters ?? [], null, SourceLocation.Unknown)
    {
    }

    private MemRefTypeReference(IReadOnlyList<long?> dimensions, bool isUnranked, TypeReference elementType, IReadOnlyList<RawSyntaxText> trailingParameters, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(dimensions, isUnranked, elementType, trailingParameters), location)
    {
        Dimensions = dimensions;
        IsUnranked = isUnranked;
        ElementType = elementType;
        TrailingParameters = trailingParameters;
    }

    public IReadOnlyList<long?> Dimensions { get; }

    public bool IsUnranked { get; }

    public TypeReference ElementType { get; }

    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; }

    public override string? Name => "memref";

    public override TypeDefinition? Definition => TypeDefinition;

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
