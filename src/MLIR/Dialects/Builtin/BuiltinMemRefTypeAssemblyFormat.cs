namespace MLIR.Dialects.Builtin;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds the builtin <c>memref</c> type, e.g. <c>memref&lt;2x3xf32&gt;</c>.
/// </summary>
/// <remarks>
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// the element type so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinMemRefTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(ParsingContext context)
    {
        if (!context.IsKeyword("memref"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = context.ExpectKeyword("memref", "Expected 'memref'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = context.Expect(TokenKind.LessThan, "Expected '<' after 'memref'.");
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

        var greaterThanResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the memref type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new MemRefTypeSyntax(
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
        if (syntax is not MemRefTypeSyntax memRefSyntax)
        {
            throw new InvalidOperationException("MemRef types require memref type syntax.");
        }

        var elementType = binder.BindTypeReference(memRefSyntax.ElementType);
        return memRefSyntax.IsUnranked
            ? new UnrankedMemRefType(
                elementType,
                BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(memRefSyntax.TrailingParameters, 0)!,
                memRefSyntax)
            : new MemRefType(
                BuiltinShapedTypeHelpers.DecodeShape(memRefSyntax.Dimensions),
                elementType,
                BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(memRefSyntax.TrailingParameters, 0)!,
                BuiltinShapedTypeHelpers.DecodeOptionalTrailingAttribute(memRefSyntax.TrailingParameters, 1)!,
                memRefSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is MemRefTypeSyntax existing)
        {
            return existing;
        }

        IReadOnlyList<long> shape;
        bool isUnranked;
        TypeReference elementType;
        var trailingParameters = new List<RawSyntaxText>();
        switch (type)
        {
            case MemRefType memRefType:
                shape = memRefType.Shape;
                isUnranked = false;
                elementType = memRefType.ElementType;
                if (memRefType.Layout != null)
                {
                    trailingParameters.Add(BuiltinShapedTypeHelpers.EncodeTrailingAttribute(memRefType.Layout));
                }

                if (memRefType.MemorySpace != null)
                {
                    trailingParameters.Add(BuiltinShapedTypeHelpers.EncodeTrailingAttribute(memRefType.MemorySpace));
                }

                break;
            case UnrankedMemRefType unrankedMemRefType:
                shape = [];
                isUnranked = true;
                elementType = unrankedMemRefType.ElementType;
                if (unrankedMemRefType.MemorySpace != null)
                {
                    trailingParameters.Add(BuiltinShapedTypeHelpers.EncodeTrailingAttribute(unrankedMemRefType.MemorySpace));
                }

                break;
            default:
                throw new InvalidOperationException(
                    $"Cannot rebuild assembly syntax for an unrecognized memref type reference of type {type.GetType().FullName}.");
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
}
