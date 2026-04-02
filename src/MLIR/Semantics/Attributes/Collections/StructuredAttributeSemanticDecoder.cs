namespace MLIR.Semantics.Attributes.Collections;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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
            DenseArrayAttributeValueSyntax denseArraySyntax => new DecodedDenseArrayAttributeValue(denseArraySyntax),
            ArrayAttributeValueSyntax arraySyntax => new DecodedArrayAttributeValue(arraySyntax),
            DictionaryAttributeValueSyntax dictionarySyntax => new DecodedDictionaryAttributeValue(dictionarySyntax),
            ElementsAttributeValueSyntax elementsSyntax => new DecodedElementsAttributeValue(elementsSyntax),
            RawAttributeValueSyntax rawSyntax => DecodeRawValue(rawSyntax),
            _ => new UnknownAttributeValue(syntax, null, null, syntax.Location),
        };
    }

    private static AttributeValue DecodeRawValue(RawAttributeValueSyntax syntax)
    {
        var text = syntax.RawText.Text;
        if (text == "true" || text == "false")
        {
            return new DecodedBooleanAttributeValue(new BooleanAttributeValueSyntax(new SyntaxToken(text), text == "true"));
        }

        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
        {
            return new DecodedStringAttributeValue(
                new StringAttributeValueSyntax(new SyntaxToken(text), StringLiteralAttributeAssemblyFormat.Unescape(text)));
        }

        if (BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
        {
            return new DecodedIntegerAttributeValue(new IntegerAttributeValueSyntax(new SyntaxToken(text), integerValue));
        }

        if (LooksLikeFloatingPointLiteral(text))
        {
            return new DecodedFloatingPointAttributeValue(new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), text));
        }

        return new UnknownAttributeValue(syntax, null, null, syntax.Location);
    }

    private static bool LooksLikeFloatingPointLiteral(string text)
    {
        if (text.IndexOf('.') < 0)
        {
            return false;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
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
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), syntax.Value)
        {
        }

        public override string? Name => null;

        public override Dialects.AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DecodedFloatingPointAttributeValue : FloatingPointAttributeValue
    {
        public DecodedFloatingPointAttributeValue(FloatingPointAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), syntax.LiteralText)
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

    private sealed class DecodedDenseArrayAttributeValue : ArrayAttributeValue
    {
        public DecodedDenseArrayAttributeValue(DenseArrayAttributeValueSyntax syntax)
            : base(new AttributeValueConstructionContext(syntax, null!, null!, syntax.Location), DecodeItems(syntax.Items.Items))
        {
            ElementTypeSyntax = syntax.ElementTypeSyntax;
        }

        public TypeSyntax ElementTypeSyntax { get; }

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
