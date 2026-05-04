namespace MLIR.Dialects.Attributes.Collections;

using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses elements attribute literals such as <c>dense&lt;[1, 2]&gt; : tensor&lt;2xi32&gt;</c>.
/// </summary>
public sealed class ElementsAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var keywordToken) || keywordToken.Text != "dense")
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var lessThanTokenResult = context.Expect(TokenKind.LessThan, "Expected '<' after 'dense'.");
        if (!lessThanTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(lessThanTokenResult.Diagnostic!);
        }

        var payloadResult = context.TryParseAttributeValueSyntax(TokenKind.GreaterThan);
        if (!payloadResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(payloadResult.Diagnostic!);
        }

        var greaterThanTokenResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the dense payload.");
        if (!greaterThanTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(greaterThanTokenResult.Diagnostic!);
        }

        var colonTokenResult = context.Expect(TokenKind.Colon, "Expected ':' before the elements type.");
        if (!colonTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(colonTokenResult.Diagnostic!);
        }

        var typeSyntaxResult = context.TryParseTypeSyntax(TokenKind.Comma, TokenKind.RBrace);
        if (!typeSyntaxResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(typeSyntaxResult.Diagnostic!);
        }

        return ParseResult<AttributeValueSyntax>.Success(new ElementsAttributeValueSyntax(
            keywordToken,
            lessThanTokenResult.Value,
            payloadResult.Value,
            greaterThanTokenResult.Value,
            colonTokenResult.Value,
            typeSyntaxResult.Value));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, Binder binder)
    {
        var normalizedSyntax = NormalizeSyntax(syntax);
        var payload = binder.BindAttributeValue(normalizedSyntax.Payload, expectedDefinition: null);
        var type = binder.BindTypeReference(normalizedSyntax.TypeSyntax);

        return new DenseTypedElementsAttr(type, payload, syntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not DenseTypedElementsAttr elementsAttribute)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Dense elements attributes require syntax to rebuild their assembly form.");
        }

        return new ElementsAttributeValueSyntax(
            TokenFactory.Identifier("dense"),
            TokenFactory.LessThan(),
            context.BuildAttributeValueSyntax(elementsAttribute.RawData),
            TokenFactory.GreaterThan(),
            TokenFactory.Colon(),
            context.BuildTypeSyntax(elementsAttribute.Type));
    }

    private static ElementsAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is ElementsAttributeValueSyntax elementsSyntax)
        {
            return elementsSyntax;
        }

        throw new System.InvalidOperationException("Unexpected syntax for elements attribute. Expected an elements attribute literal such as 'dense<[1, 2]> : tensor<2xi32>'.");
    }
}
