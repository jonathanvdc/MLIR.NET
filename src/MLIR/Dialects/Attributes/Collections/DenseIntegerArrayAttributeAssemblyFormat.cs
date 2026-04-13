namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense integer-array attribute literals such as <c>array&lt;i32: 1, 2&gt;</c>.
/// </summary>
public sealed class DenseIntegerArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<ApInt>
{
    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(ApInt element)
    {
        var text = element.ToStringSigned();
        return new IntegerAttributeValueSyntax(TokenFactory.Integer(text), element);
    }

    /// <inheritdoc/>
    protected override ApInt ElementFromSyntax(AttributeValueSyntax syntax)
    {
        return syntax switch
        {
            IntegerAttributeValueSyntax integerSyntax => integerSyntax.Value,
            RawAttributeValueSyntax rawSyntax => ApInt.Parse(64, rawSyntax.RawText.Text, isSigned: true),
            _ => ApInt.Zero(64),
        };
    }

    /// <inheritdoc/>
    protected override System.ReadOnlyMemory<byte> EncodeRawData(string? constraintName, IReadOnlyList<ApInt> items)
    {
        var bitWidth = GetConstraintBitWidth(constraintName);
        return bitWidth switch
        {
            8 => EncodeIntegers<sbyte>(items, static value => (sbyte)value.ToInt64()),
            16 => EncodeIntegers<short>(items, static value => (short)value.ToInt64()),
            64 => EncodeIntegers<long>(items, static value => value.ToInt64()),
            _ => EncodeIntegers<int>(items, static value => (int)value.ToInt64()),
        };
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<ApInt> DecodeItems(string? constraintName, System.ReadOnlyMemory<byte> rawData)
    {
        var bitWidth = GetConstraintBitWidth(constraintName);
        return bitWidth switch
        {
            8 => DecodeIntegers<sbyte>(rawData, static value => ApInt.FromInt64(8, value)),
            16 => DecodeIntegers<short>(rawData, static value => ApInt.FromInt64(16, value)),
            64 => DecodeIntegers<long>(rawData, static value => ApInt.FromInt64(64, value)),
            _ => DecodeIntegers<int>(rawData, static value => ApInt.FromInt64(32, value)),
        };
    }

    /// <inheritdoc/>
    protected override TypeReference GetElementType(string? constraintName)
    {
        return GetConstraintBitWidth(constraintName) switch
        {
            8 => TypeFactory.I8,
            16 => TypeFactory.I16,
            64 => TypeFactory.I64,
            _ => TypeFactory.I32,
        };
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText("i" + GetConstraintBitWidth(constraintName)));
    }

    private static System.ReadOnlyMemory<byte> EncodeIntegers<TInteger>(
        IReadOnlyList<ApInt> items,
        System.Func<ApInt, TInteger> convert)
        where TInteger : struct
    {
        var values = new TInteger[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            values[i] = convert(items[i]);
        }

        return MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
    }

    private static IReadOnlyList<ApInt> DecodeIntegers<TInteger>(
        System.ReadOnlyMemory<byte> rawData,
        System.Func<TInteger, ApInt> convert)
        where TInteger : struct
    {
        var span = MemoryMarshal.Cast<byte, TInteger>(rawData.Span);
        var items = new List<ApInt>(span.Length);
        for (var i = 0; i < span.Length; i++)
        {
            items.Add(convert(span[i]));
        }

        return items;
    }

    private static int GetConstraintBitWidth(string? constraintName)
    {
        return constraintName switch
        {
            "DenseI8ArrayAttr" => 8,
            "DenseI16ArrayAttr" => 16,
            "DenseI64ArrayAttr" => 64,
            _ => 32,
        };
    }
}
