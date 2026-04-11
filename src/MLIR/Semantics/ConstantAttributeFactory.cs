namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Numerics;
using MLIR.Semantics.Attributes.Collections;
using MLIR.Semantics.Attributes.Primitives;

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
    /// Creates a synthetic string attribute value.
    /// </summary>
    public static SyntheticStringAttributeValue String(string value)
    {
        return new SyntheticStringAttributeValue(value);
    }

    /// <summary>
    /// Creates a synthetic boolean attribute value.
    /// </summary>
    public static BooleanAttributeValue Bool(bool value)
    {
        return new SyntheticBooleanAttributeValue(value);
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i1</c> elements.
    /// </summary>
    public static DenseBooleanArrayAttributeValue DenseBool(ReadOnlySpan<bool> values)
    {
        return new SyntheticDenseBooleanArrayAttributeValue(ToList(values));
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i8</c> elements.
    /// </summary>
    public static DenseIntegerArrayAttributeValue DenseI8(ReadOnlySpan<sbyte> values)
    {
        return new SyntheticDenseIntegerArrayAttributeValue(ToSignedApIntList(values, bitWidth: 8));
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i16</c> elements.
    /// </summary>
    public static DenseIntegerArrayAttributeValue DenseI16(ReadOnlySpan<short> values)
    {
        return new SyntheticDenseIntegerArrayAttributeValue(ToSignedApIntList(values, bitWidth: 16));
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i32</c> elements.
    /// </summary>
    public static DenseIntegerArrayAttributeValue DenseI32(ReadOnlySpan<int> values)
    {
        return new SyntheticDenseIntegerArrayAttributeValue(ToSignedApIntList(values, bitWidth: 32));
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>i64</c> elements.
    /// </summary>
    public static DenseIntegerArrayAttributeValue DenseI64(ReadOnlySpan<long> values)
    {
        return new SyntheticDenseIntegerArrayAttributeValue(ToSignedApIntList(values, bitWidth: 64));
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>f32</c> elements.
    /// </summary>
    public static DenseFloatingPointArrayAttributeValue DenseF32(ReadOnlySpan<float> values)
    {
        return new SyntheticDenseFloatingPointArrayAttributeValue(ToSingleApFloatList(values));
    }

    /// <summary>
    /// Creates a dense-array attribute with <c>f64</c> elements.
    /// </summary>
    public static DenseFloatingPointArrayAttributeValue DenseF64(ReadOnlySpan<double> values)
    {
        return new SyntheticDenseFloatingPointArrayAttributeValue(ToDoubleApFloatList(values));
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

    private static IReadOnlyList<bool> ToList(ReadOnlySpan<bool> values)
    {
        var result = new bool[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = values[i];
        }

        return result;
    }

    private static IReadOnlyList<ApInt> ToSignedApIntList(ReadOnlySpan<sbyte> values, int bitWidth)
    {
        var result = new ApInt[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ApInt.FromInt64(bitWidth, values[i]);
        }

        return result;
    }

    private static IReadOnlyList<ApInt> ToSignedApIntList(ReadOnlySpan<short> values, int bitWidth)
    {
        var result = new ApInt[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ApInt.FromInt64(bitWidth, values[i]);
        }

        return result;
    }

    private static IReadOnlyList<ApInt> ToSignedApIntList(ReadOnlySpan<int> values, int bitWidth)
    {
        var result = new ApInt[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ApInt.FromInt64(bitWidth, values[i]);
        }

        return result;
    }

    private static IReadOnlyList<ApInt> ToSignedApIntList(ReadOnlySpan<long> values, int bitWidth)
    {
        var result = new ApInt[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ApInt.FromInt64(bitWidth, values[i]);
        }

        return result;
    }

    private static IReadOnlyList<ApFloat> ToSingleApFloatList(ReadOnlySpan<float> values)
    {
        var result = new ApFloat[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ApFloat.FromSingle(FloatSemantics.IEEESingle, values[i]);
        }

        return result;
    }

    private static IReadOnlyList<ApFloat> ToDoubleApFloatList(ReadOnlySpan<double> values)
    {
        var result = new ApFloat[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ApFloat.FromDouble(FloatSemantics.IEEEDouble, values[i]);
        }

        return result;
    }

    private sealed class SyntheticBooleanAttributeValue : BooleanAttributeValue
    {
        public SyntheticBooleanAttributeValue(bool value)
            : base(value)
        {
        }

        public override string? Name => null;

        public override AttributeConstraintDefinition? Definition => null;
    }

    private sealed class SyntheticDenseBooleanArrayAttributeValue : DenseBooleanArrayAttributeValue
    {
        public SyntheticDenseBooleanArrayAttributeValue(IReadOnlyList<bool> items)
            : base(items)
        {
        }

        public override string? Name => null;

        public override AttributeConstraintDefinition? Definition => null;
    }

    private sealed class SyntheticDenseIntegerArrayAttributeValue : DenseIntegerArrayAttributeValue
    {
        public SyntheticDenseIntegerArrayAttributeValue(IReadOnlyList<ApInt> items)
            : base(items)
        {
        }

        public override string? Name => null;

        public override AttributeConstraintDefinition? Definition => null;
    }

    private sealed class SyntheticDenseFloatingPointArrayAttributeValue : DenseFloatingPointArrayAttributeValue
    {
        public SyntheticDenseFloatingPointArrayAttributeValue(IReadOnlyList<ApFloat> items)
            : base(items)
        {
        }

        public override string? Name => null;

        public override AttributeConstraintDefinition? Definition => null;
    }
}
