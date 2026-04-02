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
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.TryMatch(TokenKind.Identifier, out var identifier))
        {
            return false;
        }

        if (identifier.Text == "true")
        {
            syntax = new BooleanAttributeValueSyntax(identifier, true);
            return true;
        }

        if (identifier.Text == "false")
        {
            syntax = new BooleanAttributeValueSyntax(identifier, false);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = syntax is BooleanAttributeValueSyntax booleanSyntax
            ? booleanSyntax
            : new BooleanAttributeValueSyntax(new SyntaxToken(syntax.GetRawText().Text), syntax.GetRawText().Text == "true");
        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
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
