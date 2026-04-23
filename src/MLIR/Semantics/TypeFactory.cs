namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR;
using MLIR.Dialects.Builtin;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax;

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
    public static IndexType Index { get; } = new();

    /// <summary>Gets the builtin <c>none</c> type.</summary>
    public static NoneType None { get; } = new();

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
    public static Float16Type F16 { get; } = new();
    /// <summary>Gets the builtin <c>f32</c> type.</summary>
    public static Float32Type F32 { get; } = new();
    /// <summary>Gets the builtin <c>f64</c> type.</summary>
    public static Float64Type F64 { get; } = new();
    /// <summary>Gets the builtin <c>bf16</c> type.</summary>
    public static BFloat16Type BF16 { get; } = new();
    /// <summary>Gets the builtin <c>tf32</c> type.</summary>
    public static FloatTF32Type TF32 { get; } = new();

    /// <summary>
    /// Creates a builtin signless integer type of the requested bit width.
    /// </summary>
    public static IntegerType I(int width)
    {
        return new IntegerType(width, IntegerTypeSignedness.Signless, null);
    }

    /// <summary>
    /// Creates a builtin signed integer type of the requested bit width.
    /// </summary>
    public static IntegerType SI(int width)
    {
        return new IntegerType(width, IntegerTypeSignedness.Signed, null);
    }

    /// <summary>
    /// Creates a builtin unsigned integer type of the requested bit width.
    /// </summary>
    public static IntegerType UI(int width)
    {
        return new IntegerType(width, IntegerTypeSignedness.Unsigned, null);
    }

    /// <summary>
    /// Creates a builtin tuple type.
    /// </summary>
    public static TupleType Tuple(params TypeReference[] elements)
    {
        return new TupleType(elements);
    }

    /// <summary>
    /// Creates a builtin function type.
    /// </summary>
    public static FunctionType Function(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
    {
        return new FunctionType(inputs, results);
    }

    /// <summary>
    /// Creates a ranked builtin tensor type with optional trailing parameters such as encodings.
    /// </summary>
    public static RankedTensorType Tensor(IReadOnlyList<long?> dimensions, TypeReference elementType, params string[] trailingParameters)
    {
        return new RankedTensorType(
            BuiltinShapedTypeHelpers.EncodeShape(dimensions),
            elementType,
            GetOptionalTrailingAttribute(trailingParameters, 0)!);
    }

    /// <summary>
    /// Creates an unranked builtin tensor type with optional trailing parameters such as encodings.
    /// </summary>
    public static UnrankedTensorType UnrankedTensor(TypeReference elementType, params string[] trailingParameters)
    {
        return new UnrankedTensorType(elementType);
    }

    /// <summary>
    /// Creates a builtin vector type.
    /// </summary>
    public static VectorType Vector(IReadOnlyList<long?> dimensions, TypeReference elementType)
    {
        return new VectorType(BuiltinShapedTypeHelpers.EncodeShape(dimensions), elementType, null!);
    }

    /// <summary>
    /// Creates a ranked builtin memref type with optional layout and memory-space parameters.
    /// </summary>
    public static MemRefType MemRef(IReadOnlyList<long?> dimensions, TypeReference elementType, params string[] trailingParameters)
    {
        return new MemRefType(
            BuiltinShapedTypeHelpers.EncodeShape(dimensions),
            elementType,
            GetOptionalTrailingAttribute(trailingParameters, 0)!,
            GetOptionalTrailingAttribute(trailingParameters, 1)!);
    }

    /// <summary>
    /// Creates an unranked builtin memref type with optional layout and memory-space parameters.
    /// </summary>
    public static UnrankedMemRefType UnrankedMemRef(TypeReference elementType, params string[] trailingParameters)
    {
        return new UnrankedMemRefType(elementType, GetOptionalTrailingAttribute(trailingParameters, 0)!);
    }

    private static AttributeValueSyntax? GetOptionalTrailingAttribute(IReadOnlyList<string> trailingParameters, int index)
    {
        return index < trailingParameters.Count
            ? BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(trailingParameters[index])
            : null;
    }
}
