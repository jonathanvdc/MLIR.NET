namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using MLIR;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Semantics.Attributes;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Provides syntax-only decoding helpers for structured attribute payloads.
/// </summary>
public static class StructuredAttributeSemanticDecoder
{
    /// <summary>
    /// Decodes a structured attribute item list into semantic attribute values.
    /// </summary>
    public static IReadOnlyList<AttributeValue> DecodeItems(IReadOnlyList<AttributeValueSyntax> items)
    {
        var result = new List<AttributeValue>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            result.Add(DecodeValue(items[i]));
        }

        return result;
    }

    /// <summary>
    /// Decodes a list of attribute-value syntax nodes into a list of <see cref="ApInt"/> values.
    /// </summary>
    public static IReadOnlyList<ApInt> DecodeIntegerItems(IReadOnlyList<AttributeValueSyntax> items)
    {
        var result = new List<ApInt>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            result.Add(DecodeIntegerValue(items[i]));
        }

        return result;
    }

    /// <summary>
    /// Decodes a list of attribute-value syntax nodes into a list of <see cref="bool"/> values.
    /// </summary>
    public static IReadOnlyList<bool> DecodeBooleanItems(IReadOnlyList<AttributeValueSyntax> items)
    {
        var result = new List<bool>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            result.Add(DecodeBooleanValue(items[i]));
        }

        return result;
    }

    /// <summary>
    /// Decodes a list of attribute-value syntax nodes into a list of floating-point values under
    /// the specified semantics.
    /// </summary>
    public static IReadOnlyList<ApFloat> DecodeFloatingPointItems(IReadOnlyList<AttributeValueSyntax> items, FloatSemantics semantics)
    {
        var result = new List<ApFloat>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            result.Add(DecodeFloatingPointValue(items[i], semantics));
        }

        return result;
    }

    /// <summary>
    /// Decodes a structured named-attribute list into a semantic attribute collection.
    /// </summary>
    public static NamedAttributeCollection DecodeAttributes(IReadOnlyList<NamedAttributeSyntax> attributes)
    {
        var result = new List<NamedAttribute>(attributes.Count);
        for (var i = 0; i < attributes.Count; i++)
        {
            result.Add(new NamedAttribute(attributes[i], DecodeValue(attributes[i].ValueSyntax)));
        }

        return new NamedAttributeCollection(result);
    }

    /// <summary>
    /// Decodes a structured attribute value syntax node into its semantic representation.
    /// </summary>
    public static AttributeValue DecodeValue(AttributeValueSyntax syntax)
    {
        return syntax switch
        {
            BooleanAttributeValueSyntax booleanSyntax =>
                new IntegerAttr(TypeFactory.I1, ApInt.FromInt64(1, booleanSyntax.Value ? 1 : 0), booleanSyntax),
            IntegerAttributeValueSyntax integerSyntax =>
                new IntegerAttr(TypeFactory.I(integerSyntax.Value.BitWidth), integerSyntax.Value, integerSyntax),
            FloatingPointAttributeValueSyntax floatingPointSyntax =>
                new FloatAttr(TypeFactory.F64, floatingPointSyntax.Value, floatingPointSyntax),
            StringAttributeValueSyntax stringSyntax =>
                new StringAttr(stringSyntax.Value, TypeFactory.None, stringSyntax),
            UnitAttributeValueSyntax unitSyntax => new UnitAttr(unitSyntax),
            TypeAttributeValueSyntax typeSyntax => new TypeAttr(new UnknownTypeReference(typeSyntax.TypeSyntax, null, null, typeSyntax.TypeSyntax.Location), typeSyntax),
            DenseArrayAttributeValueSyntax denseArraySyntax => DecodeDenseArrayValue(denseArraySyntax),
            ArrayAttributeValueSyntax arraySyntax => new ArrayAttr(DecodeItems(arraySyntax.Items.Items), arraySyntax),
            DictionaryAttributeValueSyntax dictionarySyntax => new DictionaryAttr(DecodeAttributes(dictionarySyntax.Attributes.Items), dictionarySyntax),
            ElementsAttributeValueSyntax elementsSyntax => DecodeDenseTypedElementsValue(elementsSyntax),
            TypedAttributeValueSyntax typedSyntax => DecodeTypedValue(typedSyntax),
            RawAttributeValueSyntax rawSyntax => DecodeRawValue(rawSyntax),
            _ => new UnknownAttributeValue(syntax, null, null, syntax.Location),
        };
    }

    private static AttributeValue DecodeDenseArrayValue(DenseArrayAttributeValueSyntax syntax)
    {
        var typeText = syntax.ElementTypeSyntax.ToString();
        if (TryGetFloatingPointSemantics(typeText, out var semantics))
        {
            return typeText == "f32"
                ? ConstantAttributeFactory.DenseF32(ToFloatArray(DecodeFloatingPointItems(syntax.Items.Items, semantics)), syntax)
                : ConstantAttributeFactory.DenseF64(ToDoubleArray(DecodeFloatingPointItems(syntax.Items.Items, semantics)), syntax);
        }

        return typeText switch
        {
            "i1" => ConstantAttributeFactory.DenseBool(ToBoolArray(DecodeBooleanItems(syntax.Items.Items)), syntax),
            "i8" => ConstantAttributeFactory.DenseI8(ToSByteArray(DecodeIntegerItems(syntax.Items.Items)), syntax),
            "i16" => ConstantAttributeFactory.DenseI16(ToInt16Array(DecodeIntegerItems(syntax.Items.Items)), syntax),
            "i32" => ConstantAttributeFactory.DenseI32(ToInt32Array(DecodeIntegerItems(syntax.Items.Items)), syntax),
            "i64" => ConstantAttributeFactory.DenseI64(ToInt64Array(DecodeIntegerItems(syntax.Items.Items)), syntax),
            _ => throw new System.NotSupportedException($"Unsupported dense array element type '{typeText}'."),
        };
    }

    private static ApInt DecodeIntegerValue(AttributeValueSyntax syntax)
    {
        if (syntax is IntegerAttributeValueSyntax intSyntax)
        {
            return intSyntax.Value;
        }

        if (syntax is RawAttributeValueSyntax rawSyntax)
        {
            return ApInt.Parse(64, rawSyntax.RawText.Text, isSigned: true);
        }

        return ApInt.Zero(64);
    }

    private static bool DecodeBooleanValue(AttributeValueSyntax syntax)
    {
        if (syntax is BooleanAttributeValueSyntax boolSyntax)
        {
            return boolSyntax.Value;
        }

        if (syntax is RawAttributeValueSyntax rawSyntax)
        {
            return rawSyntax.RawText.Text == "true";
        }

        return false;
    }

    private static ApFloat DecodeFloatingPointValue(AttributeValueSyntax syntax, FloatSemantics semantics)
    {
        if (syntax is FloatingPointAttributeValueSyntax fpSyntax)
        {
            return fpSyntax.Value.ConvertTo(semantics);
        }

        if (syntax is RawAttributeValueSyntax rawSyntax)
        {
            return FloatingPointLiteralParser.Parse(semantics, rawSyntax.RawText.Text);
        }

        return ApFloat.Zero(semantics);
    }

    private static AttributeValue DecodeRawValue(RawAttributeValueSyntax syntax)
    {
        var text = syntax.RawText.Text;
        if (text == "true" || text == "false")
        {
            var boolSyntax = new BooleanAttributeValueSyntax(TokenFactory.Identifier(text), text == "true");
            return new IntegerAttr(TypeFactory.I1, ApInt.FromInt64(1, text == "true" ? 1 : 0), boolSyntax);
        }

        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
        {
            var value = StringLiteralAttributeAssemblyFormat.Unescape(text);
            var strSyntax = new StringAttributeValueSyntax(TokenFactory.StringLiteral(text), value);
            return new StringAttr(value, TypeFactory.None, strSyntax);
        }

        if (BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
        {
            var intSyntax = new IntegerAttributeValueSyntax(
                TokenFactory.Integer(text),
                ApInt.Parse(64, integerValue.ToString(CultureInfo.InvariantCulture), isSigned: true));
            return new IntegerAttr(TypeFactory.I64, intSyntax.Value, intSyntax);
        }

        if (LooksLikeFloatingPointLiteral(text))
        {
            var fpValue = FloatingPointLiteralParser.Parse(text);
            var fpSyntax = new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), fpValue);
            return new FloatAttr(TypeFactory.F64, fpValue, fpSyntax);
        }

        return new UnknownAttributeValue(syntax, null, null, syntax.Location);
    }

    private static bool LooksLikeFloatingPointLiteral(string text)
    {
        return FloatingPointLiteralParser.TryParseDouble(text, out _);
    }

    private static bool TryGetFloatingPointSemantics(string typeText, out FloatSemantics semantics)
    {
        switch (typeText)
        {
            case "f16":
                semantics = FloatSemantics.IEEEHalf;
                return true;
            case "bf16":
                semantics = FloatSemantics.BFloat16;
                return true;
            case "f32":
                semantics = FloatSemantics.IEEESingle;
                return true;
            case "f64":
                semantics = FloatSemantics.IEEEDouble;
                return true;
            case "tf32":
                semantics = FloatSemantics.TF32;
                return true;
            case "f80":
                semantics = FloatSemantics.IEEEExtended80;
                return true;
            case "f128":
                semantics = FloatSemantics.IEEEQuadruple;
                return true;
            default:
                semantics = FloatSemantics.IEEEDouble;
                return false;
        }
    }

    private static AttributeValue DecodeDenseTypedElementsValue(ElementsAttributeValueSyntax syntax)
    {
        return new DenseTypedElementsAttr(
            new UnknownTypeReference(syntax.TypeSyntax, null, null, syntax.TypeSyntax.Location),
            DecodeValue(syntax.Payload),
            syntax);
    }

    private static AttributeValue DecodeTypedValue(TypedAttributeValueSyntax syntax)
    {
        var payload = DecodeValue(syntax.AttributeSyntax);
        if (payload is UnknownAttributeValue)
        {
            return new UnknownAttributeValue(syntax, null, null, syntax.Location);
        }

        return payload switch
        {
            IntegerAttr integer => new IntegerAttr(integer.Type, integer.Value, syntax),
            FloatAttr floatingPoint => new FloatAttr(floatingPoint.Type, floatingPoint.Value, syntax),
            StringAttr str => new StringAttr(str.Value, str.Type, syntax),
            UnitAttr => new UnitAttr(syntax),
            TypeAttr type => new TypeAttr(type.Value, syntax),
            ArrayAttr array => new ArrayAttr(array.Value, syntax),
            DictionaryAttr dictionary => new DictionaryAttr(dictionary.Value, syntax),
            DenseTypedElementsAttr elements => new DenseTypedElementsAttr(elements.Type, elements.RawData, syntax),
            _ => payload,
        };
    }

    private static bool[] ToBoolArray(IReadOnlyList<bool> items)
    {
        if (items is bool[] array)
        {
            return array;
        }

        if (items is List<bool> list)
        {
            return list.ToArray();
        }

        var result = new bool[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            result[i] = items[i];
        }

        return result;
    }

    private static sbyte[] ToSByteArray(IReadOnlyList<ApInt> items)
    {
        var result = new sbyte[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            result[i] = (sbyte)items[i].ToInt64();
        }

        return result;
    }

    private static short[] ToInt16Array(IReadOnlyList<ApInt> items)
    {
        var result = new short[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            result[i] = (short)items[i].ToInt64();
        }

        return result;
    }

    private static int[] ToInt32Array(IReadOnlyList<ApInt> items)
    {
        var result = new int[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            result[i] = (int)items[i].ToInt64();
        }

        return result;
    }

    private static long[] ToInt64Array(IReadOnlyList<ApInt> items)
    {
        var result = new long[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            result[i] = items[i].ToInt64();
        }

        return result;
    }

    private static float[] ToFloatArray(IReadOnlyList<ApFloat> items)
    {
        var result = new float[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            result[i] = items[i].ToSingle();
        }

        return result;
    }

    private static double[] ToDoubleArray(IReadOnlyList<ApFloat> items)
    {
        var result = new double[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            result[i] = items[i].ToDouble();
        }

        return result;
    }
}
