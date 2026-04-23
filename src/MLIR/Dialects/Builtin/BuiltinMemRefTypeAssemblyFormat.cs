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
