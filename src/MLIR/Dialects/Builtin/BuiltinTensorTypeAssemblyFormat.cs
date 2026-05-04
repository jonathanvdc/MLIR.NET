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
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// the element type so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinTensorTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        if (!context.IsKeyword("tensor"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = context.ExpectKeyword("tensor", "Expected 'tensor'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = context.Expect(TokenKind.LessThan, "Expected '<' after 'tensor'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = context.TryParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!BuiltinShapedTypeHelpers.TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeResult = context.TryParseStandaloneTypeText(elementTypeText);
        if (!elementTypeResult.IsSuccess)
        {
            return elementTypeResult;
        }

        var trailingCommaTokens = new List<Token>();
        var trailingParameters = new List<RawSyntaxText>();
        while (context.TryMatch(TokenKind.Comma, out var commaToken))
        {
            trailingCommaTokens.Add(commaToken);
            var trailingResult = context.TryParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan);
            if (!trailingResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(trailingResult.Diagnostic!);
            }

            trailingParameters.Add(trailingResult.Value);
        }

        var greaterThanResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the tensor type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new TensorTypeSyntax(
            keywordResult.Value,
            lessThanResult.Value,
            dimensions,
            xTokens,
            unrankedToken,
            elementTypeResult.Value,
            trailingCommaTokens,
            trailingParameters,
            greaterThanResult.Value));
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, Binder binder)
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
