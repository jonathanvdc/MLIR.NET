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
/// Binds and rebuilds the builtin <c>vector</c> type, e.g. <c>vector&lt;4xf32&gt;</c>.
/// </summary>
/// <remarks>
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// the element type so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinVectorTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        if (!context.IsKeyword("vector"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = context.ExpectKeyword("vector", "Expected 'vector'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = context.Expect(TokenKind.LessThan, "Expected '<' after 'vector'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = context.TryParseRawUntilDelimiter(TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!BuiltinShapedTypeHelpers.TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: false, minimumDimensionCount: 1, out var dimensions, out var xTokens, out _, out var elementTypeText))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeResult = context.TryParseStandaloneTypeText(elementTypeText);
        if (!elementTypeResult.IsSuccess)
        {
            return elementTypeResult;
        }

        var greaterThanResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the vector type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new VectorTypeSyntax(
            keywordResult.Value,
            lessThanResult.Value,
            dimensions,
            xTokens,
            elementTypeResult.Value,
            greaterThanResult.Value));
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, Binder binder)
    {
        if (syntax is not VectorTypeSyntax vectorSyntax)
        {
            throw new InvalidOperationException("Vector types require vector type syntax.");
        }

        return new VectorType(
            BuiltinShapedTypeHelpers.DecodeShape(vectorSyntax.Dimensions),
            binder.BindTypeReference(vectorSyntax.ElementType),
            null!,
            vectorSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is VectorTypeSyntax existing)
        {
            return existing;
        }

        IReadOnlyList<long> shape;
        TypeReference elementType;
        switch (type)
        {
            case VectorType vectorType:
                shape = vectorType.Shape;
                elementType = vectorType.ElementType;
                break;
            default:
                throw new InvalidOperationException(
                    $"Cannot rebuild assembly syntax for an unrecognized vector type reference of type {type.GetType().FullName}.");
        }

        var elementTypeSyntax = context.BuildTypeSyntax(elementType);

        var dimensionSyntax = shape.Select(BuiltinShapedTypeHelpers.CreateDimensionSyntax).ToArray();
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
}
