namespace MLIR.Dialects.Attributes.Primitives;

using System.Globalization;
using System.Numerics;
using MLIR;
using MLIR.Dialects;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
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
        if (!TryParseSignedIntegerLiteral(context, out var signToken, out var digitsToken, out var value))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new IntegerAttributeValueSyntax(
                signToken,
                digitsToken,
                ApInt.Parse(64, value.ToString(CultureInfo.InvariantCulture), isSigned: true)));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is not IntegerAttributeValueSyntax integerSyntax)
        {
            throw new InvalidOperationException("Expected an integer literal syntax for a primitive integer attribute.");
        }

        return definition.Factory(binder.CreateAttributeValueConstructionContext(integerSyntax, definition.Name, definition, integerSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is IntegerAttr integerAttr)
        {
            return CreateSyntax(integerAttr.Value);
        }

        // Fallback: use existing syntax when the attribute is not an IntegerAttr
        // (e.g., a user-defined test attribute or an enum constraint wrapper).
        if (attribute.Syntax is IntegerAttributeValueSyntax intSyntax)
        {
            return CreateSyntax(intSyntax.Value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive integer attributes require syntax to rebuild their assembly form.");
    }

    internal static IntegerAttributeValueSyntax CreateSyntax(ApInt value)
    {
        var text = value.ToStringSigned();
        return new IntegerAttributeValueSyntax(TokenFactory.Integer(text), value);
    }

    internal static bool TryParseSignedIntegerLiteral(
        DialectParsingContext context,
        out Token? signToken,
        out Token integerToken,
        out BigInteger value)
    {
        signToken = null;
        integerToken = default;
        value = default;
        if (context.TryMatch(TokenKind.Minus, out var minus))
        {
            signToken = minus;
        }
        else if (context.TryMatch(TokenKind.Plus, out var plus))
        {
            signToken = plus;
        }

        if (!context.TryMatch(TokenKind.Integer, out var digitsToken))
        {
            integerToken = default;
            value = default;
            return false;
        }

        if (signToken.HasValue)
        {
            value = BigInteger.Parse(signToken.Value.Text + digitsToken.Text, CultureInfo.InvariantCulture);
        }
        else
        {
            value = BigInteger.Parse(digitsToken.Text, CultureInfo.InvariantCulture);
        }

        integerToken = digitsToken;
        return true;
    }
}
