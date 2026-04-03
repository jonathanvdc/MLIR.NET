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
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.TryMatch(TokenKind.Identifier, out var keywordToken) || keywordToken.Text != "array")
        {
            return false;
        }

        var lessThanToken = context.Expect(TokenKind.LessThan, "Expected '<' after 'array'.");
        var elementTypeSyntax = context.ParseTypeSyntax(TokenKind.Colon);
        var colonToken = context.Expect(TokenKind.Colon, "Expected ':' after the dense array element type.");

        var items = new List<AttributeValueSyntax>();
        var separators = new List<SyntaxToken>();
        if (!context.Is(TokenKind.GreaterThan))
        {
            items.Add(context.ParseAttributeValueSyntax(TokenKind.Comma, TokenKind.GreaterThan));
            while (context.TryMatch(TokenKind.Comma, out var commaToken))
            {
                separators.Add(commaToken);
                items.Add(context.ParseAttributeValueSyntax(TokenKind.Comma, TokenKind.GreaterThan));
            }
        }

        var greaterThanToken = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the dense array attribute.");
        syntax = new DenseArrayAttributeValueSyntax(
            keywordToken,
            lessThanToken,
            elementTypeSyntax,
            colonToken,
            new DelimitedSyntaxList<AttributeValueSyntax>(null, items, separators, null),
            greaterThanToken);
        return true;
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

        var separators = new List<SyntaxToken>(itemSyntax.Count > 0 ? itemSyntax.Count - 1 : 0);
        for (var i = 1; i < itemSyntax.Count; i++)
        {
            separators.Add(new SyntaxToken(","));
        }

        return new DenseArrayAttributeValueSyntax(
            new SyntaxToken("array"),
            new SyntaxToken("<"),
            GetElementTypeSyntax(attribute.Definition?.Name ?? attribute.Name),
            new SyntaxToken(":"),
            new DelimitedSyntaxList<AttributeValueSyntax>(null, itemSyntax, separators, null),
            new SyntaxToken(">"));
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

        return (DenseArrayAttributeValueSyntax)binder.ReparseAttributeValueSyntax(syntax.GetRawText(), definition);
    }
}
