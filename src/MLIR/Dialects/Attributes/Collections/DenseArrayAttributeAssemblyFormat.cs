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
/// Base class for parsing dense array attribute literals such as <c>array&lt;i32: 1, 2&gt;</c>.
/// Subclasses specialise element parsing and synthesis for a concrete element type.
/// </summary>
public abstract class DenseArrayAttributeAssemblyFormat<TElement> : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var keywordToken) || keywordToken.Text != "array")
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var lessThanTokenResult = context.Expect(TokenKind.LessThan, "Expected '<' after 'array'.");
        if (!lessThanTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(lessThanTokenResult.Diagnostic!);
        }

        var elementTypeSyntaxResult = context.TryParseTypeSyntax(TokenKind.Colon);
        if (!elementTypeSyntaxResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(elementTypeSyntaxResult.Diagnostic!);
        }

        var colonTokenResult = context.Expect(TokenKind.Colon, "Expected ':' after the dense array element type.");
        if (!colonTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(colonTokenResult.Diagnostic!);
        }

        var items = new List<AttributeValueSyntax>();
        var separators = new List<Token>();
        if (!context.Is(TokenKind.GreaterThan))
        {
            var firstItemResult = context.TryParseAttributeValueSyntax(TokenKind.Comma, TokenKind.GreaterThan);
            if (!firstItemResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(firstItemResult.Diagnostic!);
            }

            items.Add(firstItemResult.Value);
            while (context.TryMatch(TokenKind.Comma, out var commaToken))
            {
                separators.Add(commaToken);
                var itemResult = context.TryParseAttributeValueSyntax(TokenKind.Comma, TokenKind.GreaterThan);
                if (!itemResult.IsSuccess)
                {
                    return ParseResult<AttributeValueSyntax>.Failure(itemResult.Diagnostic!);
                }

                items.Add(itemResult.Value);
            }
        }

        var greaterThanTokenResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the dense array attribute.");
        if (!greaterThanTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(greaterThanTokenResult.Diagnostic!);
        }

        return ParseResult<AttributeValueSyntax>.Success(new DenseArrayAttributeValueSyntax(
            keywordToken,
            lessThanTokenResult.Value,
            elementTypeSyntaxResult.Value,
            colonTokenResult.Value,
            new SeparatedSyntaxList<AttributeValueSyntax>(items, separators),
            greaterThanTokenResult.Value));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = NormalizeSyntax(syntax, definition, binder);
        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not DenseArrayAttributeValue<TElement> denseArray)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Dense array attributes require syntax to rebuild their assembly form.");
        }

        if (attribute.Syntax is DenseArrayAttributeValueSyntax denseArraySyntax)
        {
            return denseArraySyntax;
        }

        var itemSyntax = new List<AttributeValueSyntax>(denseArray.Items.Count);
        for (var i = 0; i < denseArray.Items.Count; i++)
        {
            itemSyntax.Add(ElementToSyntax(denseArray.Items[i]));
        }

        var separators = new List<Token>(itemSyntax.Count > 0 ? itemSyntax.Count - 1 : 0);
        for (var i = 1; i < itemSyntax.Count; i++)
        {
            separators.Add(TokenFactory.Comma());
        }

        return new DenseArrayAttributeValueSyntax(
            TokenFactory.Identifier("array"),
            TokenFactory.LessThan(),
            GetElementTypeSyntax(attribute.Definition?.Name ?? attribute.Name),
            TokenFactory.Colon(),
            new SeparatedSyntaxList<AttributeValueSyntax>(itemSyntax, separators),
            TokenFactory.GreaterThan());
    }

    /// <summary>
    /// Converts a single element value to its attribute-value syntax representation.
    /// </summary>
    protected abstract AttributeValueSyntax ElementToSyntax(TElement element);

    /// <summary>
    /// Returns the MLIR element-type syntax for the given constraint name (e.g. <c>i32</c>, <c>f32</c>).
    /// </summary>
    protected abstract TypeSyntax GetElementTypeSyntax(string? constraintName);

    private static DenseArrayAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is DenseArrayAttributeValueSyntax denseArraySyntax)
        {
            return denseArraySyntax;
        }

        throw new System.InvalidOperationException("Unexpected syntax for dense array attribute. Expected a dense array attribute literal such as 'array<i32: 1, 2>'.");
    }
}
