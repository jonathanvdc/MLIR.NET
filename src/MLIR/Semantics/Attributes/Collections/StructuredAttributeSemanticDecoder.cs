namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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
            BooleanAttributeValueSyntax booleanSyntax => new DecodedBooleanAttributeValue(booleanSyntax),
            IntegerAttributeValueSyntax integerSyntax => new DecodedIntegerAttributeValue(integerSyntax),
            FloatingPointAttributeValueSyntax floatingPointSyntax => new DecodedFloatingPointAttributeValue(floatingPointSyntax),
            StringAttributeValueSyntax stringSyntax => new DecodedStringAttributeValue(stringSyntax),
            UnitAttributeValueSyntax unitSyntax => new DecodedUnitAttributeValue(unitSyntax),
            TypeAttributeValueSyntax typeSyntax => new DecodedTypeAttributeValue(typeSyntax),
            DenseArrayAttributeValueSyntax denseArraySyntax => DecodeDenseArrayValue(denseArraySyntax),
            ArrayAttributeValueSyntax arraySyntax => new DecodedArrayAttributeValue(arraySyntax),
            DictionaryAttributeValueSyntax dictionarySyntax => new DecodedDictionaryAttributeValue(dictionarySyntax),
            ElementsAttributeValueSyntax elementsSyntax => new DecodedElementsAttributeValue(elementsSyntax),
            RawAttributeValueSyntax rawSyntax => DecodeRawValue(rawSyntax),
            _ => new UnknownAttributeValue(syntax, null, null, syntax.Location),
        };
    }

    private static AttributeValue DecodeDenseArrayValue(DenseArrayAttributeValueSyntax syntax)
    {
        var typeText = syntax.ElementTypeSyntax.ToString();
        if (TryGetFloatingPointSemantics(typeText, out var semantics))
        {
            return new DecodedDenseFloatingPointArrayAttributeValue(syntax, semantics);
        }

        return typeText switch
        {
            "i1" => new DecodedDenseBooleanArrayAttributeValue(syntax),
            "i8" or "i16" or "i32" or "i64" => new DecodedDenseIntegerArrayAttributeValue(syntax),
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
            return new DecodedBooleanAttributeValue(new BooleanAttributeValueSyntax(TokenFactory.Identifier(text), text == "true"));
        }

        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
        {
            return new DecodedStringAttributeValue(
                new StringAttributeValueSyntax(TokenFactory.StringLiteral(text), StringLiteralAttributeAssemblyFormat.Unescape(text)));
        }

        if (BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
        {
            return new DecodedIntegerAttributeValue(
                new IntegerAttributeValueSyntax(
                    TokenFactory.Integer(text),
                    ApInt.Parse(64, integerValue.ToString(CultureInfo.InvariantCulture), isSigned: true)));
        }

        if (LooksLikeFloatingPointLiteral(text))
        {
            return new DecodedFloatingPointAttributeValue(new FloatingPointAttributeValueSyntax(
                new RawSyntaxText(text),
                FloatingPointLiteralParser.Parse(text)));
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

    private sealed class DecodedBooleanAttributeValue : BooleanAttributeValue
    {
        public DecodedBooleanAttributeValue(BooleanAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), syntax.Value)
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedIntegerAttributeValue : IntegerAttributeValue
    {
        public DecodedIntegerAttributeValue(IntegerAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeIntegerValue(syntax))
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedFloatingPointAttributeValue : FloatingPointAttributeValue
    {
        public DecodedFloatingPointAttributeValue(FloatingPointAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), syntax.Value)
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedStringAttributeValue : StringAttributeValue
    {
        public DecodedStringAttributeValue(StringAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), syntax.Value)
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedUnitAttributeValue : UnitAttributeValue
    {
        public DecodedUnitAttributeValue(UnitAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location))
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedTypeAttributeValue : TypeAttributeValue
    {
        public DecodedTypeAttributeValue(TypeAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), syntax.TypeSyntax)
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedArrayAttributeValue : ArrayAttributeValue
    {
        public DecodedArrayAttributeValue(ArrayAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeItems(syntax.Items.Items))
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedDenseIntegerArrayAttributeValue : DenseIntegerArrayAttributeValue
    {
        public DecodedDenseIntegerArrayAttributeValue(DenseArrayAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeIntegerItems(syntax.Items.Items))
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedDenseBooleanArrayAttributeValue : DenseBooleanArrayAttributeValue
    {
        public DecodedDenseBooleanArrayAttributeValue(DenseArrayAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeBooleanItems(syntax.Items.Items))
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedDenseFloatingPointArrayAttributeValue : DenseFloatingPointArrayAttributeValue
    {
        public DecodedDenseFloatingPointArrayAttributeValue(DenseArrayAttributeValueSyntax syntax, FloatSemantics semantics)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeFloatingPointItems(syntax.Items.Items, semantics))
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedDictionaryAttributeValue : DictionaryAttributeValue
    {
        public DecodedDictionaryAttributeValue(DictionaryAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeAttributes(syntax.Attributes.Items))
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedElementsAttributeValue : ElementsAttributeValue
    {
        public DecodedElementsAttributeValue(ElementsAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeValue(syntax.Payload), syntax.TypeSyntax)
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }
}
