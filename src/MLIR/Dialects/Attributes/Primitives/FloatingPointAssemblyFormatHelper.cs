namespace MLIR.Dialects.Attributes.Primitives;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Numerics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;

internal static class FloatingPointAssemblyFormatHelper
{
    public static ParseResult<AttributeValueSyntax> TryParseDecimalLiteral(AttributeParsingContext context)
    {
        return TryParseDecimalLiteral(context, FloatSemantics.IEEEDouble);
    }

    public static ParseResult<AttributeValueSyntax> TryParseDecimalLiteral(AttributeParsingContext context, FloatSemantics semantics)
    {
        var tokens = new List<Token>();
        if (context.TryMatch(TokenKind.Plus, out var plusToken))
        {
            tokens.Add(plusToken);
        }
        else if (context.TryMatch(TokenKind.Minus, out var minusToken))
        {
            tokens.Add(minusToken);
        }

        var specialLiteralResult = TryParseSpecialLiteral(context, semantics, tokens);
        if (!specialLiteralResult.IsNoMatch)
        {
            return specialLiteralResult;
        }

        var hexLiteralResult = TryParseHexLiteral(context, semantics, tokens);
        if (!hexLiteralResult.IsNoMatch)
        {
            return hexLiteralResult;
        }

        if (!TryParseDecimalLiteralBody(context, tokens))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (!TryParseExponent(context, tokens))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var literalText = string.Concat(tokens.Select(static token => token.Text));
        if (literalText.IndexOf('.') < 0 && literalText.IndexOf('e') < 0 && literalText.IndexOf('E') < 0)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new FloatingPointAttributeValueSyntax(
                new RawSyntaxText(tokens, literalText),
                FloatingPointLiteralParser.Parse(semantics, literalText)));
    }

    private static ParseResult<AttributeValueSyntax> TryParseSpecialLiteral(AttributeParsingContext context, FloatSemantics semantics, List<Token> tokens)
    {
        if (!context.TryPeekToken(0, out var kind, out var text) || kind != TokenKind.Identifier)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (!IsSpecialLiteral(text))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        context.TryMatch(TokenKind.Identifier, out var identifierToken);
        tokens.Add(identifierToken);
        var literalText = string.Concat(tokens.Select(static token => token.Text));
        return ParseResult<AttributeValueSyntax>.Success(
            new FloatingPointAttributeValueSyntax(
                new RawSyntaxText(tokens, literalText),
                FloatingPointLiteralParser.Parse(semantics, literalText)));
    }

    private static ParseResult<AttributeValueSyntax> TryParseHexLiteral(AttributeParsingContext context, FloatSemantics semantics, List<Token> tokens)
    {
        if (!context.TryPeekToken(0, out var kind, out var text) || kind != TokenKind.Integer || text != "0")
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (!context.TryPeekToken(1, out var prefixKind, out var prefixText) || prefixKind != TokenKind.Identifier || !IsHexPrefixToken(prefixText))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        context.TryMatch(TokenKind.Integer, out var zeroToken);
        tokens.Add(zeroToken);
        context.TryMatch(TokenKind.Identifier, out var hexToken);
        tokens.Add(hexToken);
        var literalText = string.Concat(tokens.Select(static token => token.Text));
        var hexDigits = literalText.Substring(2);
        var bits = ApInt.Parse(semantics.BitWidth, hexDigits, radix: 16);
        return ParseResult<AttributeValueSyntax>.Success(new FloatingPointAttributeValueSyntax(new RawSyntaxText(tokens, literalText), ApFloat.FromBits(semantics, bits)));
    }

    private static bool TryParseDecimalLiteralBody(AttributeParsingContext context, List<Token> tokens)
    {
        if (context.Is(TokenKind.Dot))
        {
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
            return true;
        }

        if (!context.TryMatch(TokenKind.Integer, out var integerPart))
        {
            return false;
        }

        tokens.Add(integerPart);
        if (context.TryMatch(TokenKind.Dot, out var dotToken))
        {
            tokens.Add(dotToken);
            if (context.TryMatch(TokenKind.Integer, out var fractionalPart))
            {
                tokens.Add(fractionalPart);
            }
        }

        return true;
    }

    private static bool TryParseExponent(AttributeParsingContext context, List<Token> tokens)
    {
        if (!context.TryPeekToken(0, out var kind, out var text) || kind != TokenKind.Identifier)
        {
            return true;
        }

        if (text.Length == 0 || (text[0] != 'e' && text[0] != 'E'))
        {
            return true;
        }

        if (text.Length > 1 && text.Substring(1).Any(ch => !char.IsDigit(ch)))
        {
            return false;
        }

        if (!context.TryMatch(TokenKind.Identifier, out var exponentMarker))
        {
            return false;
        }

        tokens.Add(exponentMarker);
        if (exponentMarker.Text.Length > 1)
        {
            return true;
        }

        if (context.TryMatch(TokenKind.Plus, out var exponentPlus))
        {
            tokens.Add(exponentPlus);
        }
        else if (context.TryMatch(TokenKind.Minus, out var exponentMinus))
        {
            tokens.Add(exponentMinus);
        }

        if (!context.TryMatch(TokenKind.Integer, out var exponentDigits))
        {
            return false;
        }

        tokens.Add(exponentDigits);
        return true;
    }

    private static bool IsSpecialLiteral(string text)
    {
        return string.Equals(text, "inf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "infinity", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "nan", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHexPrefixToken(string text)
    {
        return text.Length > 1
            && (text[0] == 'x' || text[0] == 'X')
            && text.Skip(1).All(ch => Uri.IsHexDigit(ch));
    }

    public static FloatingPointAttributeValueSyntax BuildSyntax(RawSyntaxText rawText, ApFloat value)
    {
        return new FloatingPointAttributeValueSyntax(rawText, value);
    }
}
