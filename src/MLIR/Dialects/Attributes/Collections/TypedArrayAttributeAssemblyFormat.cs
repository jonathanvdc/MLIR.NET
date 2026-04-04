namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Collections;
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
        var separators = new List<SyntaxToken>();
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
            ?? (ArrayAttributeValueSyntax)binder.ReparseAttributeValueSyntax(syntax.GetRawText(), definition);
        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not TypedArrayAttributeValue<TElement> typedArray)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Typed array attributes require syntax to rebuild their assembly form.");
        }

        if (attribute.Syntax is ArrayAttributeValueSyntax arraySyntax)
        {
            return arraySyntax;
        }

        var items = new List<AttributeValueSyntax>(typedArray.Items.Count);
        for (var i = 0; i < typedArray.Items.Count; i++)
        {
            items.Add(ElementToSyntax(typedArray.Items[i], context));
        }

        var separators = new List<SyntaxToken>(items.Count > 0 ? items.Count - 1 : 0);
        for (var i = 1; i < items.Count; i++)
        {
            separators.Add(new SyntaxToken(","));
        }

        return new ArrayAttributeValueSyntax(
            new SyntaxToken("["),
            items,
            separators,
            new SyntaxToken("]"));
    }

    /// <summary>
    /// Converts a single typed item to its attribute-value syntax representation.
    /// </summary>
    protected abstract AttributeValueSyntax ElementToSyntax(TElement element, ConcreteSyntaxBuilderContext context);
}
