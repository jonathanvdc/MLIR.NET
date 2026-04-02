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
/// Parses dense array attribute literals such as <c>array&lt;i32: 1, 2&gt;</c>.
/// </summary>
public sealed class DenseArrayAttributeAssemblyFormat : IAttributeAssemblyFormat
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
        if (attribute is not ArrayAttributeValue arrayAttribute)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Dense array attributes require syntax to rebuild their assembly form.");
        }

        if (attribute.Syntax is DenseArrayAttributeValueSyntax denseArraySyntax)
        {
            return denseArraySyntax;
        }

        var itemSyntax = new List<AttributeValueSyntax>(arrayAttribute.Items.Count);
        for (var i = 0; i < arrayAttribute.Items.Count; i++)
        {
            itemSyntax.Add(context.BuildAttributeValueSyntax(arrayAttribute.Items[i]));
        }

        var separators = new List<SyntaxToken>(itemSyntax.Count > 0 ? itemSyntax.Count - 1 : 0);
        for (var i = 1; i < itemSyntax.Count; i++)
        {
            separators.Add(new SyntaxToken(","));
        }

        return new DenseArrayAttributeValueSyntax(
            new SyntaxToken("array"),
            new SyntaxToken("<"),
            InferElementTypeSyntax(arrayAttribute),
            new SyntaxToken(":"),
            new DelimitedSyntaxList<AttributeValueSyntax>(null, itemSyntax, separators, null),
            new SyntaxToken(">"));
    }

    private static DenseArrayAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is DenseArrayAttributeValueSyntax denseArraySyntax)
        {
            return denseArraySyntax;
        }

        return (DenseArrayAttributeValueSyntax)binder.ReparseAttributeValueSyntax(syntax.GetRawText(), definition);
    }

    private static TypeSyntax InferElementTypeSyntax(ArrayAttributeValue arrayAttribute)
    {
        var name = arrayAttribute.Definition?.Name ?? arrayAttribute.Name;
        return name switch
        {
            "DenseBoolArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i1")),
            "DenseI8ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i8")),
            "DenseI16ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i16")),
            "DenseI32ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i32")),
            "DenseI64ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i64")),
            "DenseF32ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("f32")),
            "DenseF64ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("f64")),
            _ => new RawTypeSyntax(new RawSyntaxText("unknown")),
        };
    }
}
