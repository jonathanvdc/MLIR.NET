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
    /// <summary>
    /// Encodes integer elements into a raw dense payload with the element width selected by
    /// <paramref name="constraintName"/>.
    /// </summary>
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
    /// <summary>
    /// Decodes a raw dense payload into integer elements with bit width determined by
    /// <paramref name="constraintName"/>.
    /// </summary>
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

    /// <summary>
    /// Converts a list of <see cref="ApInt"/> values to a byte payload using the concrete
    /// primitive integer representation <typeparamref name="TInteger"/>.
    /// </summary>
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

    /// <summary>
    /// Converts a raw dense byte payload to a list of <see cref="ApInt"/> values using
    /// <typeparamref name="TInteger"/> as the storage element representation.
    /// </summary>
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

    /// <summary>
    /// Resolves the fixed integer bit width associated with a dense integer constraint name.
    /// Defaults to 32-bit integers for unknown or null constraint names.
    /// </summary>
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
