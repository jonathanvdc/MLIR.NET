namespace MLIR.Syntax;

using System.Collections;
using System.Text;

/// <summary>
/// Combines preserved tokens and nested raw syntax fragments into a new raw syntax projection.
/// </summary>
internal static class SyntaxTextComposer
{
    public static RawSyntaxText Compose(params object?[] parts)
    {
        var tokens = new List<SyntaxToken>();
        var text = new StringBuilder();
        var wroteAny = false;

        foreach (var part in parts)
        {
            Append(part, tokens, text, ref wroteAny);
        }

        return tokens.Count == 0
            ? new RawSyntaxText(string.Empty)
            : new RawSyntaxText(tokens, text.ToString());
    }

    private static void Append(object? part, List<SyntaxToken> tokens, StringBuilder text, ref bool wroteAny)
    {
        switch (part)
        {
            case null:
                return;
            case SyntaxToken token:
                tokens.Add(token);
                text.Append(wroteAny ? token.FullText : token.Text);
                wroteAny = true;
                return;
            case RawSyntaxText rawText:
                foreach (var token in rawText.Tokens)
                {
                    tokens.Add(token);
                }

                text.Append(wroteAny ? rawText.FullText : rawText.Text);
                wroteAny = true;
                return;
            case TypeSyntax typeSyntax:
                Append(typeSyntax.GetRawText(), tokens, text, ref wroteAny);
                return;
            case ShapedTypeDimensionSyntax dimensionSyntax:
                Append(dimensionSyntax.GetRawText(), tokens, text, ref wroteAny);
                return;
            case IEnumerable<SyntaxToken> syntaxTokens:
                foreach (var syntaxToken in syntaxTokens)
                {
                    Append(syntaxToken, tokens, text, ref wroteAny);
                }

                return;
            case IEnumerable<TypeSyntax> typeSyntaxes:
                foreach (var typeSyntax in typeSyntaxes)
                {
                    Append(typeSyntax, tokens, text, ref wroteAny);
                }

                return;
            case IEnumerable<ShapedTypeDimensionSyntax> dimensions:
                foreach (var dimension in dimensions)
                {
                    Append(dimension, tokens, text, ref wroteAny);
                }

                return;
            case IEnumerable<RawSyntaxText> rawSyntaxTexts:
                foreach (var raw in rawSyntaxTexts)
                {
                    Append(raw, tokens, text, ref wroteAny);
                }

                return;
            case IEnumerable enumerable when part is not string:
                foreach (var item in enumerable)
                {
                    Append(item, tokens, text, ref wroteAny);
                }

                return;
            default:
                throw new InvalidOperationException($"Cannot compose raw syntax from part of type '{part.GetType().FullName}'.");
        }
    }
}
