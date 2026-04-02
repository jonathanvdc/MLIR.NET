namespace MLIR.Dialects;

using System.Globalization;
using System.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses primitive integer attribute literals used by context-directed attribute constraints such as <c>I32Attr</c>.
/// </summary>
public sealed class IntegerLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.TryMatch(TokenKind.Integer, out var literalToken))
        {
            return false;
        }

        syntax = new IntegerAttributeValueSyntax(literalToken, BigInteger.Parse(literalToken.Text, CultureInfo.InvariantCulture));
        return true;
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = NormalizeSyntax(syntax);

        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is IntegerAttributeValue integerAttribute)
        {
            return new IntegerAttributeValueSyntax(
                new SyntaxToken(integerAttribute.Value.ToString(CultureInfo.InvariantCulture)),
                integerAttribute.Value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive integer attributes require syntax to rebuild their assembly form.");
    }

    private static IntegerAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax)
    {
        if (syntax is IntegerAttributeValueSyntax integerSyntax)
        {
            return integerSyntax;
        }

        var rawText = syntax.GetRawText();
        var token = rawText.Tokens.Count > 0
            ? rawText.Tokens[0]
            : new SyntaxToken(rawText.Text);
        return new IntegerAttributeValueSyntax(token, BigInteger.Parse(rawText.Text, CultureInfo.InvariantCulture));
    }
}
