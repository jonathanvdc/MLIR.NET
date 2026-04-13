namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense floating-point array attribute literals such as <c>array&lt;f32: 1.0, 2.0&gt;</c>.
/// </summary>
public sealed class DenseFloatingPointArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<ApFloat>
{
    private readonly string elementTypeText;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseFloatingPointArrayAttributeAssemblyFormat"/> class.
    /// </summary>
    /// <param name="elementTypeText">The literal type text to emit when rebuilding syntax.</param>
    public DenseFloatingPointArrayAttributeAssemblyFormat(string elementTypeText)
    {
        this.elementTypeText = elementTypeText;
    }

    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(ApFloat element)
    {
        var text = FloatingPointLiteralParser.Format(element);
        return new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), element);
    }

    /// <inheritdoc/>
    protected override ApFloat ElementFromSyntax(AttributeValueSyntax syntax)
    {
        var semantics = GetSemantics();
        return syntax switch
        {
            FloatingPointAttributeValueSyntax floatingPointSyntax => floatingPointSyntax.Value.ConvertTo(semantics),
            RawAttributeValueSyntax rawSyntax => FloatingPointLiteralParser.Parse(semantics, rawSyntax.RawText.Text),
            _ => ApFloat.Zero(semantics),
        };
    }

    /// <inheritdoc/>
    protected override System.ReadOnlyMemory<byte> EncodeRawData(string? constraintName, IReadOnlyList<ApFloat> items)
    {
        if (IsF32Constraint(constraintName))
        {
            var values = new float[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                values[i] = items[i].ToSingle();
            }

            return MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        }

        var doubles = new double[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            doubles[i] = items[i].ToDouble();
        }

        return MemoryMarshal.AsBytes(doubles.AsSpan()).ToArray();
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<ApFloat> DecodeItems(string? constraintName, System.ReadOnlyMemory<byte> rawData)
    {
        if (IsF32Constraint(constraintName))
        {
            var span = MemoryMarshal.Cast<byte, float>(rawData.Span);
            var items = new List<ApFloat>(span.Length);
            for (var i = 0; i < span.Length; i++)
            {
                items.Add(ApFloat.FromDouble(FloatSemantics.IEEESingle, span[i]));
            }

            return items;
        }

        var doubles = MemoryMarshal.Cast<byte, double>(rawData.Span);
        var result = new List<ApFloat>(doubles.Length);
        for (var i = 0; i < doubles.Length; i++)
        {
            result.Add(ApFloat.FromDouble(FloatSemantics.IEEEDouble, doubles[i]));
        }

        return result;
    }

    /// <inheritdoc/>
    protected override TypeReference GetElementType(string? constraintName)
    {
        return IsF32Constraint(constraintName) ? TypeFactory.F32 : TypeFactory.F64;
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText(elementTypeText));
    }

    private bool IsF32Constraint(string? constraintName)
    {
        return string.Equals(constraintName, "DenseF32ArrayAttr", System.StringComparison.Ordinal)
            || string.Equals(elementTypeText, "f32", System.StringComparison.Ordinal);
    }

    private FloatSemantics GetSemantics()
    {
        return IsF32Constraint(null) ? FloatSemantics.IEEESingle : FloatSemantics.IEEEDouble;
    }
}
