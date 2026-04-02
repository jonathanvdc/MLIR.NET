namespace MLIR.Dialects.Attributes.Collections;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Collections;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses elements attribute literals such as <c>dense&lt;[1, 2]&gt; : tensor&lt;2xi32&gt;</c>.
/// </summary>
public sealed class ElementsAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.TryMatch(TokenKind.Identifier, out var keywordToken) || keywordToken.Text != "dense")
        {
            return false;
        }

        var lessThanToken = context.Expect(TokenKind.LessThan, "Expected '<' after 'dense'.");
        var payload = context.ParseAttributeValueSyntax(TokenKind.GreaterThan);
        var greaterThanToken = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the dense payload.");
        var colonToken = context.Expect(TokenKind.Colon, "Expected ':' before the elements type.");
        var typeSyntax = context.ParseTypeSyntax(TokenKind.Comma, TokenKind.RBrace);
        syntax = new ElementsAttributeValueSyntax(keywordToken, lessThanToken, payload, greaterThanToken, colonToken, typeSyntax);
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
        if (attribute is not ElementsAttributeValue elementsAttribute)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Elements attributes require syntax to rebuild their assembly form.");
        }

        return new ElementsAttributeValueSyntax(
            new SyntaxToken("dense"),
            new SyntaxToken("<"),
            context.BuildAttributeValueSyntax(elementsAttribute.Payload),
            new SyntaxToken(">"),
            new SyntaxToken(":"),
            elementsAttribute.TypeSyntax);
    }

    private static ElementsAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is ElementsAttributeValueSyntax elementsSyntax)
        {
            return elementsSyntax;
        }

        return (ElementsAttributeValueSyntax)binder.ReparseAttributeValueSyntax(syntax.GetRawText(), definition);
    }
}
