namespace MLIR.Dialects.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses primitive floating-point attribute literals.
/// </summary>
public sealed class FloatingPointLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(context);
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = syntax as FloatingPointAttributeValueSyntax
            ?? new FloatingPointAttributeValueSyntax(syntax.GetRawText(), syntax.GetRawText().Text);
        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is FloatingPointAttributeValue floatingPointAttribute)
        {
            return FloatingPointAssemblyFormatHelper.BuildSyntax(new RawSyntaxText(floatingPointAttribute.LiteralText), floatingPointAttribute.LiteralText);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive floating-point attributes require syntax to rebuild their assembly form.");
    }
}
