namespace MLIR.Dialects.Attributes.Primitives;

using System.Globalization;
using System.Numerics;
using MLIR.Dialects;
using MLIR.Numerics;
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
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!TryParseSignedIntegerLiteral(context, out var rawText, out var value))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new IntegerAttributeValueSyntax(
                CreateSingleToken(rawText),
                ApInt.Parse(64, value.ToString(CultureInfo.InvariantCulture), isSigned: true)));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is not IntegerAttributeValueSyntax integerSyntax)
        {
            throw new InvalidOperationException("Expected an integer literal syntax for a primitive integer attribute.");
        }

        return definition.Factory(new AttributeValueConstructionContext(integerSyntax, definition.Name, definition, integerSyntax.Location));
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

    internal static IntegerAttributeValueSyntax CreateSyntax(ApInt value)
    {
        var text = value.ToStringSigned();
        return new IntegerAttributeValueSyntax(new SyntaxToken(text), value);
    }

    internal static bool TryParseSignedIntegerLiteral(AttributeParsingContext context, out RawSyntaxText rawText, out BigInteger value)
    {
        rawText = null!;
        value = default;
        SyntaxToken? signToken = null;
        if (context.TryMatch(TokenKind.Minus, out var minus))
        {
            signToken = minus;
        }
        else if (context.TryMatch(TokenKind.Plus, out var plus))
        {
            signToken = plus;
        }

        if (!context.TryMatch(TokenKind.Integer, out var integerToken))
        {
            rawText = default!;
            value = default;
            return false;
        }

        if (signToken.HasValue)
        {
            rawText = new RawSyntaxText([signToken.Value, integerToken], signToken.Value.Text + integerToken.Text);
            value = BigInteger.Parse(rawText.Text, CultureInfo.InvariantCulture);
        }
        else
        {
            rawText = new RawSyntaxText([integerToken]);
            value = BigInteger.Parse(integerToken.Text, CultureInfo.InvariantCulture);
        }

        return true;
    }

    internal static SyntaxToken CreateSingleToken(RawSyntaxText rawText)
    {
        return rawText.Tokens.Count == 0 ? new SyntaxToken(rawText.Text) : new SyntaxToken(rawText.Text, rawText.Tokens[0].LeadingTrivia, rawText.Location.Line, rawText.Location.Column);
    }
}
