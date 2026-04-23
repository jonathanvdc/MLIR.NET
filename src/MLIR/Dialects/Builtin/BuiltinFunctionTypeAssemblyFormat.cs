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
/// Binds and rebuilds the builtin <c>function</c> type, e.g. <c>(i32, f32) -> i64</c>.
/// </summary>
/// <remarks>
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// nested input and result types so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinFunctionTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        if (!context.Is(TokenKind.LParen))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var inputsResult = TryParseTypeList(context, TokenKind.LParen, TokenKind.RParen);
        if (!inputsResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(inputsResult.Diagnostic!);
        }

        if (!context.TryMatch(TokenKind.Arrow, out var arrowToken))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        TypeSyntax? resultType = null;
        DelimitedSyntaxList<TypeSyntax> resultTypes;
        if (context.Is(TokenKind.LParen))
        {
            var resultTypesResult = TryParseTypeList(context, TokenKind.LParen, TokenKind.RParen);
            if (!resultTypesResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(resultTypesResult.Diagnostic!);
            }

            resultTypes = resultTypesResult.Value;
        }
        else
        {
            resultTypes = new DelimitedSyntaxList<TypeSyntax>(null, [], [], null);
            var resultTypeResult = context.TryParseCurrentTypeSyntax();
            if (!resultTypeResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(resultTypeResult.Diagnostic!);
            }

            resultType = resultTypeResult.Value;
        }

        return ParseResult<TypeSyntax>.Success(new FunctionTypeSyntax(inputsResult.Value, arrowToken, resultType, resultTypes));
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
        return new FunctionType(inputs, results, functionSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is FunctionTypeSyntax existing)
        {
            return existing;
        }

        var (inputs, results) = type switch
        {
            FunctionType generated => (generated.Inputs, generated.Results),
            _ => throw new InvalidOperationException(
                $"Cannot rebuild assembly syntax for an unrecognized function type reference of type {type.GetType().FullName}.")
        };

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

    private static ParseResult<DelimitedSyntaxList<TypeSyntax>> TryParseTypeList(TypeParsingContext context, TokenKind openKind, TokenKind closeKind)
    {
        var openResult = context.Expect(openKind, $"Expected '{TokenText(openKind)}' to start the type list.");
        if (!openResult.IsSuccess)
        {
            return ParseResult<DelimitedSyntaxList<TypeSyntax>>.Failure(openResult.Diagnostic!);
        }

        var items = new List<TypeSyntax>();
        var separators = new List<Token>();
        if (!context.Is(closeKind))
        {
            while (true)
            {
                var itemResult = context.TryParseTypeSyntax(TokenKind.Comma, closeKind);
                if (!itemResult.IsSuccess)
                {
                    return ParseResult<DelimitedSyntaxList<TypeSyntax>>.Failure(itemResult.Diagnostic!);
                }

                items.Add(itemResult.Value);
                if (!context.TryMatch(TokenKind.Comma, out var commaToken))
                {
                    break;
                }

                separators.Add(commaToken);
            }
        }

        var closeResult = context.Expect(closeKind, $"Expected '{TokenText(closeKind)}' to close the type list.");
        if (!closeResult.IsSuccess)
        {
            return ParseResult<DelimitedSyntaxList<TypeSyntax>>.Failure(closeResult.Diagnostic!);
        }

        return ParseResult<DelimitedSyntaxList<TypeSyntax>>.Success(new DelimitedSyntaxList<TypeSyntax>(
            openResult.Value,
            items,
            separators,
            closeResult.Value));
    }

    private static string TokenText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.LParen => "(",
            TokenKind.RParen => ")",
            _ => kind.ToString(),
        };
    }
}
