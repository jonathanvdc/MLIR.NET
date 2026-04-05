namespace MLIR.Dialects.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses single-precision floating-point attribute literals.
/// </summary>
public sealed class F32AttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(context);
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is FloatingPointAttributeValueSyntax floatSyntax)
        {
            return definition.Factory(new AttributeValueConstructionContext(floatSyntax, definition.Name, definition, floatSyntax.Location));
        }
        else if (syntax is IntegerAttributeValueSyntax integerSyntax)
        {
            // Allow integer literals to be implicitly converted to single-precision floating-point attributes.
            return definition.Factory(new AttributeValueConstructionContext(integerSyntax, definition.Name, definition, integerSyntax.Location));
        }
        else
        {
            throw new InvalidOperationException("Expected a floating-point or integer literal syntax for a single-precision floating-point attribute.");
        }
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is F32AttributeValue f32Attribute)
        {
            var text = FloatingPointLiteralParser.FormatSingle(f32Attribute.Value);
            return FloatingPointAssemblyFormatHelper.BuildSyntax(new RawSyntaxText(text), text);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Single-precision floating-point attributes require syntax to rebuild their assembly form.");
    }
}
