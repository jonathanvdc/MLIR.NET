namespace MLIR.Dialects.Attributes.Primitives;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        var tokens = new List<SyntaxToken>();
        if (context.TryMatch(TokenKind.Minus, out var minus))
        {
            tokens.Add(minus);
        }

        if (!context.TryMatch(TokenKind.Integer, out var integerPart))
        {
            return false;
        }

        tokens.Add(integerPart);
        if (!context.TryMatch(TokenKind.Dot, out var dot))
        {
            return false;
        }

        tokens.Add(dot);
        if (!context.TryMatch(TokenKind.Integer, out var fractionalPart))
        {
            return false;
        }

        tokens.Add(fractionalPart);
        var literalText = string.Concat(tokens.Select(static token => token.Text));
        syntax = new FloatingPointAttributeValueSyntax(new RawSyntaxText(tokens, literalText), literalText);
        return true;
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
        if (attribute is F32AttributeValue f32Attribute)
        {
            var text = f32Attribute.Value.ToString("R", CultureInfo.InvariantCulture);
            return new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), text);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Single-precision floating-point attributes require syntax to rebuild their assembly form.");
    }
}
