namespace MLIR.Dialects.Builtin;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Types.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds the builtin <c>tensor</c> type, e.g. <c>tensor&lt;2x3xf32&gt;</c>.
/// </summary>
/// <remarks>
/// Parsing is handled by the core type parser; this format only provides binding and CST rebuild.
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// the element type so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinTensorTypeAssemblyFormat : ITypeAssemblyFormat
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
        if (syntax is not TensorTypeSyntax tensorSyntax)
        {
            throw new InvalidOperationException("Tensor types require tensor type syntax.");
        }

        var elementType = binder.BindTypeReference(tensorSyntax.ElementType);
        return tensorSyntax.IsUnranked
            ? new UnrankedTensorType(elementType, tensorSyntax)
            : new RankedTensorType(
                BuiltinShapedTypeHelpers.DecodeShape(tensorSyntax.Dimensions),
                elementType,
                BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(tensorSyntax.TrailingParameters, 0)!,
                tensorSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is TensorTypeSyntax existing)
        {
            return existing;
        }

        IReadOnlyList<long> shape;
        bool isUnranked;
        TypeReference elementType;
        IReadOnlyList<RawSyntaxText> trailingParameters;
        switch (type)
        {
            case RankedTensorType rankedTensor:
                shape = rankedTensor.Shape;
                isUnranked = false;
                elementType = rankedTensor.ElementType;
                trailingParameters = rankedTensor.Encoding != null
                    ? [BuiltinShapedTypeHelpers.EncodeTrailingAttribute(rankedTensor.Encoding)]
                    : [];
                break;
            case UnrankedTensorType unrankedTensor:
                shape = [];
                isUnranked = true;
                elementType = unrankedTensor.ElementType;
                trailingParameters = [];
                break;
            default:
                throw new InvalidOperationException(
                    $"Cannot rebuild assembly syntax for an unrecognized tensor type reference of type {type.GetType().FullName}.");
        }

        var elementTypeSyntax = context.BuildTypeSyntax(elementType);
        var dimensionSyntax = shape.Select(BuiltinShapedTypeHelpers.CreateDimensionSyntax).ToArray();
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

        return new TensorTypeSyntax(
            TokenFactory.Identifier("tensor"),
            TokenFactory.LessThan(),
            dimensionSyntax,
            xTokens,
            isUnranked ? TokenFactory.Star() : null,
            elementTypeSyntax,
            commas,
            trailingParameters,
            TokenFactory.GreaterThan());
    }
}
