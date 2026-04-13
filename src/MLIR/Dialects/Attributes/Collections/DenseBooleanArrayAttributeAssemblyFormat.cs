namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense boolean-array attribute literals such as <c>array&lt;i1: true, false&gt;</c>.
/// </summary>
public sealed class DenseBooleanArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<bool>
{
    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(bool element)
    {
        var text = element ? "true" : "false";
        return new BooleanAttributeValueSyntax(TokenFactory.Identifier(text), element);
    }

    /// <inheritdoc/>
    protected override bool ElementFromSyntax(AttributeValueSyntax syntax)
    {
        return syntax switch
        {
            BooleanAttributeValueSyntax booleanSyntax => booleanSyntax.Value,
            RawAttributeValueSyntax rawSyntax => rawSyntax.RawText.Text == "true",
            _ => false,
        };
    }

    /// <inheritdoc/>
    protected override System.ReadOnlyMemory<byte> EncodeRawData(string? constraintName, IReadOnlyList<bool> items)
    {
        var values = new bool[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            values[i] = items[i];
        }

        return MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<bool> DecodeItems(string? constraintName, System.ReadOnlyMemory<byte> rawData)
    {
        var span = MemoryMarshal.Cast<byte, bool>(rawData.Span);
        var values = new bool[span.Length];
        span.CopyTo(values);
        return values;
    }

    /// <inheritdoc/>
    protected override TypeReference GetElementType(string? constraintName)
    {
        return TypeFactory.I1;
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText("i1"));
    }
}
