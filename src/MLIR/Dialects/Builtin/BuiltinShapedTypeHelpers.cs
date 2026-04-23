namespace MLIR.Dialects.Builtin;

using System.Collections.Generic;
using System;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Types.Collections;

/// <summary>
/// Shared helpers for the builtin shaped-type runtime bridge.
/// </summary>
/// <remarks>
/// The generated builtin shaped types model MLIR's semantic shape payload directly, where dynamic
/// dimensions use the same sentinel convention as upstream MLIR's <c>ShapedType::kDynamic</c>.
/// The handwritten assembly formats and factories still speak in terms of parsed syntax, so this
/// helper centralizes the conversion in one place while the runtime migrates away from the older
/// handwritten shaped-type wrappers.
/// </remarks>
internal static class BuiltinShapedTypeHelpers
{
    /// <summary>
    /// MLIR's sentinel value for a dynamic shaped dimension.
    /// </summary>
    internal const long DynamicDimension = -1;

    /// <summary>
    /// Decodes parsed shaped dimensions into the generated builtin representation.
    /// </summary>
    public static IReadOnlyList<long> DecodeShape(IReadOnlyList<ShapedTypeDimensionSyntax> dimensions)
    {
        var decoded = new long[dimensions.Count];
        for (var i = 0; i < dimensions.Count; i++)
        {
            decoded[i] = dimensions[i] switch
            {
                StaticShapedTypeDimensionSyntax staticDimension => staticDimension.Size,
                DynamicShapedTypeDimensionSyntax => DynamicDimension,
                _ => DynamicDimension,
            };
        }

        return decoded;
    }

    /// <summary>
    /// Converts ergonomic nullable-dimension input into the generated builtin representation.
    /// </summary>
    public static IReadOnlyList<long> EncodeShape(IReadOnlyList<long?> dimensions)
    {
        var encoded = new long[dimensions.Count];
        for (var i = 0; i < dimensions.Count; i++)
        {
            encoded[i] = dimensions[i] ?? DynamicDimension;
        }

        return encoded;
    }

    /// <summary>
    /// Creates syntax for one semantic shaped dimension.
    /// </summary>
    public static ShapedTypeDimensionSyntax CreateDimensionSyntax(long dimension)
    {
        return IsDynamicDimension(dimension)
            ? new DynamicShapedTypeDimensionSyntax(TokenFactory.Question())
            : new StaticShapedTypeDimensionSyntax(TokenFactory.Integer(dimension.ToString()), dimension);
    }

    /// <summary>
    /// Returns whether the supplied semantic shape entry is dynamic.
    /// </summary>
    public static bool IsDynamicDimension(long dimension)
    {
        return dimension == DynamicDimension;
    }

    /// <summary>
    /// Wraps one preserved trailing attribute fragment as attribute syntax.
    /// </summary>
    public static AttributeValueSyntax? DecodeOptionalTrailingAttribute(IReadOnlyList<RawSyntaxText> trailingParameters, int index)
    {
        return index < trailingParameters.Count
            ? new OpaqueAttributeValueSyntax(trailingParameters[index])
            : null;
    }

    /// <summary>
    /// Wraps one programmatic trailing attribute fragment as attribute syntax.
    /// </summary>
    public static AttributeValueSyntax? DecodeOptionalTrailingAttribute(string? text)
    {
        return string.IsNullOrEmpty(text)
            ? null
            : new OpaqueAttributeValueSyntax(new RawSyntaxText(text!));
    }

    /// <summary>
    /// Re-encodes attribute syntax as raw text for CST rebuilding.
    /// </summary>
    public static RawSyntaxText EncodeTrailingAttribute(AttributeValueSyntax attribute)
    {
        var text = attribute.ToString() ?? string.Empty;
        return attribute is OpaqueAttributeValueSyntax opaque
            ? opaque.RawText
            : new RawSyntaxText(text);
    }

    /// <summary>
    /// Parses a shaped-type body string of the form <c>dim x dim x ... x elementType</c> or
    /// <c>* x elementType</c>, splitting it into dimensions and element-type text.
    /// </summary>
    public static bool TryParseShapedTypeBody(
        string text,
        bool allowUnranked,
        int minimumDimensionCount,
        out List<ShapedTypeDimensionSyntax> dimensions,
        out List<Token> xTokens,
        out Token? unrankedToken,
        out string elementTypeText)
    {
        text = text.Trim();
        dimensions = [];
        xTokens = [];
        unrankedToken = null;
        elementTypeText = string.Empty;

        if (allowUnranked && text.StartsWith("*", StringComparison.Ordinal))
        {
            unrankedToken = TokenFactory.Star();
            var unrankedIndex = 1;
            while (unrankedIndex < text.Length && char.IsWhiteSpace(text[unrankedIndex]))
            {
                unrankedIndex++;
            }

            if (unrankedIndex >= text.Length || text[unrankedIndex] != 'x')
            {
                return false;
            }

            xTokens.Add(TokenFactory.Identifier("x"));
            unrankedIndex++;
            while (unrankedIndex < text.Length && char.IsWhiteSpace(text[unrankedIndex]))
            {
                unrankedIndex++;
            }

            elementTypeText = text.Substring(unrankedIndex);
            return elementTypeText.Length > 0;
        }

        var index = 0;
        while (index < text.Length)
        {
            if (text[index] == '?')
            {
                dimensions.Add(new DynamicShapedTypeDimensionSyntax(TokenFactory.Question()));
                index++;
            }
            else if (char.IsDigit(text[index]))
            {
                var start = index;
                while (index < text.Length && char.IsDigit(text[index]))
                {
                    index++;
                }

                var digits = text.Substring(start, index - start);
                dimensions.Add(new StaticShapedTypeDimensionSyntax(TokenFactory.Integer(digits), long.Parse(digits)));
            }
            else
            {
                break;
            }

            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length || text[index] != 'x')
            {
                return false;
            }

            xTokens.Add(TokenFactory.Identifier("x"));
            index++;
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }

        if (dimensions.Count < minimumDimensionCount)
        {
            return false;
        }

        elementTypeText = text.Substring(index).Trim();
        return elementTypeText.Length > 0;
    }
}
