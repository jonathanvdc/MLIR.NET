namespace MLIR.Dialects.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses primitive boolean attribute literals.
/// </summary>
public sealed class BooleanLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var identifier))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (identifier.Text == "true")
        {
            return ParseResult<AttributeValueSyntax>.Success(new BooleanAttributeValueSyntax(identifier, true));
        }

        if (identifier.Text == "false")
        {
            return ParseResult<AttributeValueSyntax>.Success(new BooleanAttributeValueSyntax(identifier, false));
        }

        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is not BooleanAttributeValueSyntax booleanSyntax)
        {
            throw new InvalidOperationException("Expected a boolean literal syntax for a primitive boolean attribute.");
        }

        return definition.Factory(new AttributeValueConstructionContext(booleanSyntax, definition.Name, definition, booleanSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is BooleanAttributeValue booleanAttribute)
        {
            return new BooleanAttributeValueSyntax(new SyntaxToken(booleanAttribute.Value ? "true" : "false"), booleanAttribute.Value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive boolean attributes require syntax to rebuild their assembly form.");
    }
}
