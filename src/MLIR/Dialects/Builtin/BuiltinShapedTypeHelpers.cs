namespace MLIR.Dialects.Builtin;

using System.Collections.Generic;
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
}
