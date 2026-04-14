namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Base class for parsing array attribute literals whose items are strongly typed.
/// </summary>
public abstract class TypedArrayAttributeAssemblyFormat<TElement> : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.LBracket, out var openToken))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var items = new List<AttributeValueSyntax>();
        var separators = new List<Token>();
        if (!context.Is(TokenKind.RBracket))
        {
            var firstItemResult = context.TryParseAttributeValueSyntax(TokenKind.Comma, TokenKind.RBracket);
            if (!firstItemResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(firstItemResult.Diagnostic!);
            }

            items.Add(firstItemResult.Value);
            while (context.TryMatch(TokenKind.Comma, out var commaToken))
            {
                separators.Add(commaToken);
                var itemResult = context.TryParseAttributeValueSyntax(TokenKind.Comma, TokenKind.RBracket);
                if (!itemResult.IsSuccess)
                {
                    return ParseResult<AttributeValueSyntax>.Failure(itemResult.Diagnostic!);
                }

                items.Add(itemResult.Value);
            }
        }

        var closeTokenResult = context.Expect(TokenKind.RBracket, "Expected ']' to close the typed array attribute.");
        if (!closeTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(closeTokenResult.Diagnostic!);
        }

        return ParseResult<AttributeValueSyntax>.Success(new ArrayAttributeValueSyntax(openToken, items, separators, closeTokenResult.Value));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = syntax as ArrayAttributeValueSyntax
            ?? throw new InvalidOperationException("Unexpected syntax for typed array attribute. Expected an array attribute literal such as '[1, 2]'.");
        return MLIR.Semantics.Attributes.Collections.ArrayAttrConstraintHelpers.BindFromSyntax(normalizedSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute.Syntax is ArrayAttributeValueSyntax arraySyntax)
        {
            return arraySyntax;
        }

        if (attribute is not ArrayAttr arrayAttr)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Array attributes require ArrayAttr storage or reusable syntax to rebuild their assembly form.");
        }

        var items = new List<AttributeValueSyntax>(arrayAttr.Value.Count);
        for (var i = 0; i < arrayAttr.Value.Count; i++)
        {
            items.Add(context.BuildAttributeValueSyntax(arrayAttr.Value[i]));
        }

        var separators = new List<Token>(items.Count > 0 ? items.Count - 1 : 0);
        for (var i = 1; i < items.Count; i++)
        {
            separators.Add(TokenFactory.Comma());
        }

        return new ArrayAttributeValueSyntax(
            TokenFactory.LBracket(),
            items,
            separators,
            TokenFactory.RBracket());
    }
}
