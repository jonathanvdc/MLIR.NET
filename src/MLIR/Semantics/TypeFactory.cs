namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR;
using MLIR.Semantics.Types.Collections;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

/// <summary>
/// Provides ergonomic factory helpers for programmatic semantic type construction.
/// </summary>
/// <remarks>
/// These helpers build real semantic type values directly, so callers do not need to fabricate
/// syntax nodes just to construct well-typed operations in memory.
/// </remarks>
public static class TypeFactory
{
    /// <summary>Gets the builtin <c>index</c> type.</summary>
    public static IndexTypeReference Index { get; } = new();

    /// <summary>Gets the builtin <c>none</c> type.</summary>
    public static NoneTypeReference None { get; } = new();

    /// <summary>Gets the builtin signless <c>i1</c> type.</summary>
    public static IntegerType I1 { get; } = I(1);
    /// <summary>Gets the builtin signless <c>i8</c> type.</summary>
    public static IntegerType I8 { get; } = I(8);
    /// <summary>Gets the builtin signless <c>i16</c> type.</summary>
    public static IntegerType I16 { get; } = I(16);
    /// <summary>Gets the builtin signless <c>i32</c> type.</summary>
    public static IntegerType I32 { get; } = I(32);
    /// <summary>Gets the builtin signless <c>i64</c> type.</summary>
    public static IntegerType I64 { get; } = I(64);
    /// <summary>Gets the builtin signless <c>i128</c> type.</summary>
    public static IntegerType I128 { get; } = I(128);

    /// <summary>Gets the builtin <c>f16</c> type.</summary>
    public static FloatTypeReference F16 { get; } = F("f16");
    /// <summary>Gets the builtin <c>f32</c> type.</summary>
    public static FloatTypeReference F32 { get; } = F("f32");
    /// <summary>Gets the builtin <c>f64</c> type.</summary>
    public static FloatTypeReference F64 { get; } = F("f64");
    /// <summary>Gets the builtin <c>bf16</c> type.</summary>
    public static FloatTypeReference BF16 { get; } = F("bf16");
    /// <summary>Gets the builtin <c>tf32</c> type.</summary>
    public static FloatTypeReference TF32 { get; } = F("tf32");

    /// <summary>
    /// Creates a builtin signless integer type of the requested bit width.
    /// </summary>
    public static IntegerType I(int width)
    {
        return new IntegerType(width, IntegerTypeSignedness.Signless);
    }

    /// <summary>
    /// Creates a builtin signed integer type of the requested bit width.
    /// </summary>
    public static IntegerType SI(int width)
    {
        return new IntegerType(width, IntegerTypeSignedness.Signed);
    }

    /// <summary>
    /// Creates a builtin unsigned integer type of the requested bit width.
    /// </summary>
    public static IntegerType UI(int width)
    {
        return new IntegerType(width, IntegerTypeSignedness.Unsigned);
    }

    /// <summary>
    /// Creates a builtin floating-point type by canonical MLIR spelling.
    /// </summary>
    public static FloatTypeReference F(string name)
    {
        return new FloatTypeReference(name);
    }

    /// <summary>
    /// Creates a builtin tuple type.
    /// </summary>
    public static TupleTypeReference Tuple(params TypeReference[] elements)
    {
        return new TupleTypeReference(elements);
    }

    /// <summary>
    /// Creates a builtin function type.
    /// </summary>
    public static FunctionTypeReference Function(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
    {
        return new FunctionTypeReference(inputs, results);
    }

    /// <summary>
    /// Creates a ranked builtin tensor type with optional trailing parameters such as encodings.
    /// </summary>
    public static TensorTypeReference Tensor(IReadOnlyList<long?> dimensions, TypeReference elementType, params string[] trailingParameters)
    {
        return new TensorTypeReference(dimensions, false, elementType, ToRawSyntaxTexts(trailingParameters));
    }

    /// <summary>
    /// Creates an unranked builtin tensor type with optional trailing parameters such as encodings.
    /// </summary>
    public static TensorTypeReference UnrankedTensor(TypeReference elementType, params string[] trailingParameters)
    {
        return new TensorTypeReference([], true, elementType, ToRawSyntaxTexts(trailingParameters));
    }

    /// <summary>
    /// Creates a builtin vector type.
    /// </summary>
    public static VectorTypeReference Vector(IReadOnlyList<long?> dimensions, TypeReference elementType)
    {
        return new VectorTypeReference(dimensions, elementType);
    }

    /// <summary>
    /// Creates a ranked builtin memref type with optional layout and memory-space parameters.
    /// </summary>
    public static MemRefTypeReference MemRef(IReadOnlyList<long?> dimensions, TypeReference elementType, params string[] trailingParameters)
    {
        return new MemRefTypeReference(dimensions, false, elementType, ToRawSyntaxTexts(trailingParameters));
    }

    /// <summary>
    /// Creates an unranked builtin memref type with optional layout and memory-space parameters.
    /// </summary>
    public static MemRefTypeReference UnrankedMemRef(TypeReference elementType, params string[] trailingParameters)
    {
        return new MemRefTypeReference([], true, elementType, ToRawSyntaxTexts(trailingParameters));
    }

    private static IReadOnlyList<RawSyntaxText> ToRawSyntaxTexts(IReadOnlyList<string> values)
    {
        var results = new RawSyntaxText[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            results[i] = new RawSyntaxText(values[i]);
        }

        return results;
    }
}
