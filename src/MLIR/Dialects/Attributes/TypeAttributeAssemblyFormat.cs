namespace MLIR.Dialects.Attributes;

using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses type attributes whose payload is a nested type syntax node.
/// </summary>
public sealed class TypeAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        return context.TryParseTypeSyntax(TokenKind.Comma)
            .Map<AttributeValueSyntax>(static typeSyntax => new TypeAttributeValueSyntax(typeSyntax));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        var typeSyntax = syntax is TypeAttributeValueSyntax typeAttributeSyntax
            ? typeAttributeSyntax.TypeSyntax
            : throw new InvalidOperationException("Unexpected syntax for type attribute. Expected a type attribute literal such as 'i32'.");
        var normalizedSyntax = new TypeAttributeValueSyntax(typeSyntax);
        return new TypeAttr(binder.BindTypeReference(typeSyntax), normalizedSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is TypeAttr typeAttribute)
        {
            var typeSyntax = typeAttribute.Value.Syntax ?? context.BuildTypeSyntax(typeAttribute.Value);
            return new TypeAttributeValueSyntax(typeSyntax);
        }

        return attribute.Syntax ?? throw new InvalidOperationException("Type attributes require syntax to rebuild their assembly form.");
    }
}
