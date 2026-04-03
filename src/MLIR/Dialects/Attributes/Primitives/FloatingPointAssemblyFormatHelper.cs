namespace MLIR.Dialects.Attributes.Primitives;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;

internal static class FloatingPointAssemblyFormatHelper
{
    public static bool TryParseDecimalLiteral(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;

        var tokens = new List<SyntaxToken>();
        if (context.TryMatch(TokenKind.Plus, out var plusToken))
        {
            tokens.Add(plusToken);
        }
        else if (context.TryMatch(TokenKind.Minus, out var minusToken))
        {
            tokens.Add(minusToken);
        }

        if (TryParseSpecialLiteral(context, tokens, out syntax))
        {
            return true;
        }

        if (TryParseHexLiteral(context, tokens, out syntax))
        {
            return true;
        }

        if (!TryParseDecimalLiteralBody(context, tokens))
        {
            return false;
        }

        if (!TryParseExponent(context, tokens))
        {
            return false;
        }

        var literalText = string.Concat(tokens.Select(static token => token.Text));
        if (literalText.IndexOf('.') < 0 && literalText.IndexOf('e') < 0 && literalText.IndexOf('E') < 0)
        {
            return false;
        }

        syntax = new FloatingPointAttributeValueSyntax(new RawSyntaxText(tokens, literalText), literalText);
        return true;
    }

    private static bool TryParseSpecialLiteral(AttributeParsingContext context, List<SyntaxToken> tokens, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.Is(TokenKind.Identifier))
        {
            return false;
        }

        if (!context.TryMatch(TokenKind.Identifier, out var identifierToken))
        {
            return false;
        }

        if (!IsSpecialLiteral(identifierToken.Text))
        {
            return false;
        }

        tokens.Add(identifierToken);
        var literalText = string.Concat(tokens.Select(static token => token.Text));
        syntax = new FloatingPointAttributeValueSyntax(new RawSyntaxText(tokens, literalText), literalText);
        return true;
    }

    private static bool TryParseHexLiteral(AttributeParsingContext context, List<SyntaxToken> tokens, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.Is(TokenKind.Integer))
        {
            return false;
        }

        if (!context.TryMatch(TokenKind.Integer, out var zeroToken))
        {
            return false;
        }

        tokens.Add(zeroToken);
        if (!context.Is(TokenKind.Identifier))
        {
            return false;
        }

        if (!context.TryMatch(TokenKind.Identifier, out var hexToken))
        {
            return false;
        }

        if (!IsHexPrefixToken(hexToken.Text))
        {
            return false;
        }

        tokens.Add(hexToken);
        var literalText = string.Concat(tokens.Select(static token => token.Text));
        syntax = new FloatingPointAttributeValueSyntax(new RawSyntaxText(tokens, literalText), literalText);
        return true;
    }

    private static bool TryParseDecimalLiteralBody(AttributeParsingContext context, List<SyntaxToken> tokens)
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

    private static bool TryParseExponent(AttributeParsingContext context, List<SyntaxToken> tokens)
    {
        if (!context.Is(TokenKind.Identifier))
        {
            return true;
        }

        if (!context.TryMatch(TokenKind.Identifier, out var exponentMarker))
        {
            return false;
        }

        if (exponentMarker.Text.Length == 0 || (exponentMarker.Text[0] != 'e' && exponentMarker.Text[0] != 'E'))
        {
            return false;
        }

        if (exponentMarker.Text.Length > 1)
        {
            if (exponentMarker.Text.Substring(1).Any(ch => !char.IsDigit(ch)))
            {
                return false;
            }

            tokens.Add(exponentMarker);
            return true;
        }

        tokens.Add(exponentMarker);
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

    public static FloatingPointAttributeValueSyntax BuildSyntax(RawSyntaxText rawText, string literalText)
    {
        return new FloatingPointAttributeValueSyntax(rawText, literalText);
    }
}
