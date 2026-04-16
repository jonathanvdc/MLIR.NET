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
/// Binds and rebuilds the builtin <c>vector</c> type, e.g. <c>vector&lt;4xf32&gt;</c>.
/// </summary>
/// <remarks>
/// Parsing is handled by the core type parser; this format only provides binding and CST rebuild.
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// the element type so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinVectorTypeAssemblyFormat : ITypeAssemblyFormat
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
        if (syntax is not VectorTypeSyntax vectorSyntax)
        {
            throw new InvalidOperationException("Vector types require vector type syntax.");
        }

        return new VectorTypeReference(
            vectorSyntax,
            DecodeDimensions(vectorSyntax.Dimensions),
            binder.BindTypeReference(vectorSyntax.ElementType));
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is VectorTypeSyntax existing)
        {
            return existing;
        }

        if (type is not VectorTypeReference vectorType)
        {
            throw new InvalidOperationException(
                $"Cannot rebuild assembly syntax for an unrecognized vector type reference of type {type.GetType().FullName}.");
        }

        var dimensions = vectorType.Dimensions;
        var elementTypeSyntax = context.BuildTypeSyntax(vectorType.ElementType);

        var dimensionSyntax = dimensions.Select(TensorTypeReference.CreateDimensionSyntax).ToArray();
        var xTokens = new List<Token>(dimensionSyntax.Length);
        for (var i = 0; i < dimensionSyntax.Length; i++)
        {
            xTokens.Add(TokenFactory.Identifier("x"));
        }

        return new VectorTypeSyntax(
            TokenFactory.Identifier("vector"),
            TokenFactory.LessThan(),
            dimensionSyntax,
            xTokens,
            elementTypeSyntax,
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
