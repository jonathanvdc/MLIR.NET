namespace MLIR.Dialects.Attributes;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes;
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
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        var typeSyntax = context.ParseTypeSyntax(TokenKind.Comma);
        syntax = new TypeAttributeValueSyntax(typeSyntax);
        return true;
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var typeSyntax = syntax is TypeAttributeValueSyntax typeAttributeSyntax
            ? typeAttributeSyntax.TypeSyntax
            : new RawTypeSyntax(syntax.GetRawText());
        return definition.Factory(new AttributeValueConstructionContext(new TypeAttributeValueSyntax(typeSyntax), definition.Name, definition, syntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is TypeAttributeValue typeAttribute)
        {
            return new TypeAttributeValueSyntax(typeAttribute.TypeSyntax);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Type attributes require syntax to rebuild their assembly form.");
    }
}
