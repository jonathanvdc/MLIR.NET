namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MLIR;
using MLIR.Numerics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Provides ergonomic helpers for constructing constant semantic attribute values.
/// </summary>
/// <remarks>
/// This mirrors the role of <see cref="TypeFactory"/> for semantic types and is intended
/// to be the canonical entry point for constant attribute construction in generated code.
/// </remarks>
public static class ConstantAttributeFactory
{
    /// <summary>
    /// Creates a string attribute value.
    /// </summary>
    public static StringAttr String(string value)
    {
        return new StringAttr(value, TypeFactory.None, syntax: null);
    }

    /// <summary>
    /// Creates an <c>i1</c> integer attribute for a boolean constant.
    /// </summary>
    public static IntegerAttr Bool(bool value)
    {
        var type = TypeFactory.I1;
        return new IntegerAttr(type, ApInt.FromInt64(type.Width, value ? 1 : 0), syntax: null);
    }

    /// <summary>
    /// Creates an <c>i32</c> integer attribute.
    /// </summary>
    public static IntegerAttr I32(uint value)
    {
        var apInt = ApInt.FromUInt64(TypeFactory.I32.Width, value);
        return new IntegerAttr(
            TypeFactory.I32,
            apInt,
            syntax: new IntegerAttributeValueSyntax(TokenFactory.Integer(value.ToString(System.Globalization.CultureInfo.InvariantCulture)), apInt));
    }

    /// <summary>
    /// Creates an <c>f32</c> floating-point attribute.
    /// </summary>
    public static FloatAttr F32(ApFloat value)
    {
        return new FloatAttr(
            TypeFactory.F32,
            value,
            syntax: new FloatingPointAttributeValueSyntax(new RawSyntaxText(value.ToString()), value));
    }

    /// <summary>
    /// Creates an <c>f64</c> floating-point attribute.
    /// </summary>
    public static FloatAttr F64(ApFloat value)
    {
        return new FloatAttr(
            TypeFactory.F64,
            value,
            syntax: new FloatingPointAttributeValueSyntax(new RawSyntaxText(value.ToString()), value));
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i1</c> elements.
    /// </summary>
    public static DenseArrayAttr DenseBool(ReadOnlySpan<bool> values)
    {
        return Dense(TypeFactory.I1, values);
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i8</c> elements.
    /// </summary>
    public static DenseArrayAttr DenseI8(ReadOnlySpan<sbyte> values)
    {
        return Dense(TypeFactory.I8, values);
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i16</c> elements.
    /// </summary>
    public static DenseArrayAttr DenseI16(ReadOnlySpan<short> values)
    {
        return Dense(TypeFactory.I16, values);
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i32</c> elements.
    /// </summary>
    public static DenseArrayAttr DenseI32(ReadOnlySpan<int> values)
    {
        return Dense(TypeFactory.I32, values);
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i64</c> elements.
    /// </summary>
    public static DenseArrayAttr DenseI64(ReadOnlySpan<long> values)
    {
        return Dense(TypeFactory.I64, values);
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>f32</c> elements.
    /// </summary>
    public static DenseArrayAttr DenseF32(ReadOnlySpan<float> values)
    {
        return Dense(TypeFactory.F32, values);
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>f64</c> elements.
    /// </summary>
    public static DenseArrayAttr DenseF64(ReadOnlySpan<double> values)
    {
        return Dense(TypeFactory.F64, values);
    }

    /// <summary>
    /// Creates a flat symbol-reference attribute.
    /// </summary>
    public static SymbolRefAttr FlatSymbolRef(string rootReference)
    {
        return new SymbolRefAttr(rootReference);
    }

    /// <summary>
    /// Creates a symbol-reference attribute with optional nested references.
    /// </summary>
    public static SymbolRefAttr SymbolRef(string rootReference, IReadOnlyList<string> nestedReferences)
    {
        return new SymbolRefAttr(rootReference, nestedReferences);
    }

    /// <summary>
    /// Clones a symbol-reference attribute.
    /// </summary>
    public static SymbolRefAttr SymbolRef(SymbolRefAttr reference)
    {
        return SymbolRef(reference.RootReference, reference.NestedReferences);
    }

    private static DenseArrayAttr Dense<T>(TypeReference elementType, ReadOnlySpan<T> values)
        where T : struct
    {
        return new DenseArrayAttr(elementType, values.Length, MemoryMarshal.AsBytes(values).ToArray(), syntax: null);
    }
}
