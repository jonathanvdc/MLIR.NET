namespace MLIR.Dialects.Builtin;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Semantics;
using MLIR.Semantics.Types.Collections;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds the builtin <c>memref</c> type, e.g. <c>memref&lt;2x3xf32&gt;</c>.
/// </summary>
/// <remarks>
/// Parsing is handled by the core type parser; this format only provides binding and CST rebuild.
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// the element type so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinMemRefTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        // Parsing is handled by the core type parser, not by dialect custom syntax.
        return ParseResult<TypeSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)
    {
        if (syntax is not MemRefTypeSyntax memRefSyntax)
        {
            throw new InvalidOperationException("MemRef types require memref type syntax.");
        }

        return new MemRefTypeReference(
            memRefSyntax,
            DecodeDimensions(memRefSyntax.Dimensions),
            binder.BindTypeReference(memRefSyntax.ElementType),
            memRefSyntax.TrailingParameters);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is MemRefTypeSyntax existing)
        {
            return existing;
        }

        if (type is not MemRefTypeReference memRefType)
        {
            throw new InvalidOperationException(
                $"Cannot rebuild assembly syntax for an unrecognized memref type reference of type {type.GetType().FullName}.");
        }

        var dimensions = memRefType.Dimensions;
        var isUnranked = memRefType.IsUnranked;
        var elementTypeSyntax = context.BuildTypeSyntax(memRefType.ElementType);
        var trailingParameters = memRefType.TrailingParameters;

        var dimensionSyntax = dimensions.Select(TensorTypeReference.CreateDimensionSyntax).ToArray();
        var xTokens = new List<Token>(isUnranked ? 1 : dimensionSyntax.Length);
        for (var i = 0; i < xTokens.Capacity; i++)
        {
            xTokens.Add(TokenFactory.Identifier("x"));
        }

        var commas = new List<Token>(trailingParameters.Count);
        for (var i = 0; i < trailingParameters.Count; i++)
        {
            commas.Add(TokenFactory.Comma());
        }

        return new MemRefTypeSyntax(
            TokenFactory.Identifier("memref"),
            TokenFactory.LessThan(),
            dimensionSyntax,
            xTokens,
            isUnranked ? TokenFactory.Star() : null,
            elementTypeSyntax,
            commas,
            trailingParameters,
            TokenFactory.GreaterThan());
    }

    private static IReadOnlyList<long?> DecodeDimensions(IReadOnlyList<ShapedTypeDimensionSyntax> dimensions)
    {
        var decoded = new long?[dimensions.Count];
        for (var i = 0; i < dimensions.Count; i++)
        {
            decoded[i] = dimensions[i] is StaticShapedTypeDimensionSyntax staticDim ? staticDim.Size : null;
        }

        return decoded;
    }
}
