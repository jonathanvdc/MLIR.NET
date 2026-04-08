namespace MLIR.Dialects.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Numerics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses double-precision floating-point attribute literals.
/// </summary>
public sealed class F64AttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(context, FloatSemantics.IEEEDouble);
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
            // Allow integer literals to be implicitly converted to double-precision floating-point attributes.
            var convertedSyntax = FloatingPointAssemblyFormatHelper.BuildSyntax(
                new RawSyntaxText(integerSyntax.SignToken.HasValue
                    ? [integerSyntax.SignToken.Value, integerSyntax.IntegerToken]
                    : [integerSyntax.IntegerToken]),
                FloatingPointLiteralParser.Parse(
                    FloatSemantics.IEEEDouble,
                    integerSyntax.SignToken.HasValue
                        ? integerSyntax.SignToken.Value.Text + integerSyntax.IntegerToken.Text
                        : integerSyntax.IntegerToken.Text));
            return definition.Factory(new AttributeValueConstructionContext(convertedSyntax, definition.Name, definition, integerSyntax.Location));
        }
        else
        {
            throw new InvalidOperationException("Expected a floating-point or integer literal syntax for a double-precision floating-point attribute.");
        }
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is FloatingPointAttributeValue floatingPointAttribute)
        {
            var value = floatingPointAttribute.Value;
            var text = FloatingPointLiteralParser.Format(value);
            return FloatingPointAssemblyFormatHelper.BuildSyntax(new RawSyntaxText(text), value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Double-precision floating-point attributes require syntax to rebuild their assembly form.");
    }
}
