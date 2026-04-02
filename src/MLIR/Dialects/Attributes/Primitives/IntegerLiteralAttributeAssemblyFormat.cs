namespace MLIR.Dialects.Attributes.Primitives;

using System.Globalization;
using System.Numerics;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
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
        if (!TryParseSignedIntegerLiteral(context, out var rawText, out var value))
        {
            return false;
        }

        syntax = new IntegerAttributeValueSyntax(CreateSingleToken(rawText), value);
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
            return CreateSyntax(integerAttribute.Value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive integer attributes require syntax to rebuild their assembly form.");
    }

    internal static IntegerAttributeValueSyntax CreateSyntax(BigInteger value)
    {
        var text = value.ToString(CultureInfo.InvariantCulture);
        return new IntegerAttributeValueSyntax(new SyntaxToken(text), value);
    }

    internal static IntegerAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax)
    {
        if (syntax is IntegerAttributeValueSyntax integerSyntax)
        {
            return integerSyntax;
        }

        var rawText = syntax.GetRawText();
        return new IntegerAttributeValueSyntax(CreateSingleToken(rawText), BigInteger.Parse(rawText.Text, CultureInfo.InvariantCulture));
    }

    internal static bool TryParseSignedIntegerLiteral(AttributeParsingContext context, out RawSyntaxText rawText, out BigInteger value)
    {
        rawText = null!;
        value = default;
        SyntaxToken? minusToken = null;
        if (context.TryMatch(TokenKind.Minus, out var minus))
        {
            minusToken = minus;
        }

        if (!context.TryMatch(TokenKind.Integer, out var integerToken))
        {
            rawText = default!;
            value = default;
            return false;
        }

        if (minusToken.HasValue)
        {
            rawText = new RawSyntaxText([minusToken.Value, integerToken], minusToken.Value.Text + integerToken.Text);
            value = BigInteger.Parse(rawText.Text, CultureInfo.InvariantCulture);
        }
        else
        {
            rawText = new RawSyntaxText([integerToken]);
            value = BigInteger.Parse(integerToken.Text, CultureInfo.InvariantCulture);
        }

        return true;
    }

    private static SyntaxToken CreateSingleToken(RawSyntaxText rawText)
    {
        return rawText.Tokens.Count == 0 ? new SyntaxToken(rawText.Text) : new SyntaxToken(rawText.Text, rawText.Tokens[0].LeadingTrivia, rawText.Location.Line, rawText.Location.Column);
    }
}
