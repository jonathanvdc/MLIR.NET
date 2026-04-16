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
/// Binds and rebuilds the builtin <c>function</c> type, e.g. <c>(i32, f32) -> i64</c>.
/// </summary>
/// <remarks>
/// Parsing is handled by the core type parser; this format only provides binding and CST rebuild.
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// nested input and result types so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinFunctionTypeAssemblyFormat : ITypeAssemblyFormat
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
        if (syntax is not FunctionTypeSyntax functionSyntax)
        {
            throw new InvalidOperationException("Function types require function type syntax.");
        }

        var inputs = functionSyntax.InputTypes.Items.Select(binder.BindTypeReference).ToArray();
        IReadOnlyList<TypeSyntax> resultSyntaxList = functionSyntax.HasDelimitedResults
            ? functionSyntax.ResultTypes.Items
            : functionSyntax.ResultType != null ? [functionSyntax.ResultType] : [];
        var results = resultSyntaxList.Select(binder.BindTypeReference).ToArray();
        return new FunctionTypeReference(functionSyntax, inputs, results);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is FunctionTypeSyntax existing)
        {
            return existing;
        }

        if (type is not FunctionTypeReference functionType)
        {
            throw new InvalidOperationException(
                $"Cannot rebuild assembly syntax for an unrecognized function type reference of type {type.GetType().FullName}.");
        }

        var inputs = functionType.Inputs;
        var results = functionType.Results;

        var inputSyntax = inputs.Select(context.BuildTypeSyntax).ToArray();
        var inputCommas = new List<Token>(Math.Max(0, inputs.Count - 1));
        for (var i = 1; i < inputs.Count; i++)
        {
            inputCommas.Add(TokenFactory.Comma());
        }

        var inputList = new DelimitedSyntaxList<TypeSyntax>(
            TokenFactory.LParen(), inputSyntax, inputCommas, TokenFactory.RParen());

        if (results.Count == 1)
        {
            return new FunctionTypeSyntax(
                inputList,
                TokenFactory.Arrow(),
                context.BuildTypeSyntax(results[0]),
                new DelimitedSyntaxList<TypeSyntax>(null, [], [], null));
        }

        var resultSyntax = results.Select(context.BuildTypeSyntax).ToArray();
        var resultCommas = new List<Token>(Math.Max(0, results.Count - 1));
        for (var i = 1; i < results.Count; i++)
        {
            resultCommas.Add(TokenFactory.Comma());
        }

        return new FunctionTypeSyntax(
            inputList,
            TokenFactory.Arrow(),
            null,
            new DelimitedSyntaxList<TypeSyntax>(
                TokenFactory.LParen(), resultSyntax, resultCommas, TokenFactory.RParen()));
    }
}
