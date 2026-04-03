namespace MLIR.Dialects.Attributes.Primitives;

using System.Globalization;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
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
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(context, out syntax);
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
        if (attribute is F64AttributeValue f64Attribute)
        {
            var text = FloatingPointLiteralParser.FormatDouble(f64Attribute.Value);
            return FloatingPointAssemblyFormatHelper.BuildSyntax(new RawSyntaxText(text), text);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Double-precision floating-point attributes require syntax to rebuild their assembly form.");
    }
}
