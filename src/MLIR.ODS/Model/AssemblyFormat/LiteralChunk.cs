using System;
using System.Collections.Generic;

namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A keyword, punctuation token, or whitespace literal surrounded by backticks.
/// Examples: `(`, `,`, `->`, `\n`, `foo`.
/// </summary>
public sealed class LiteralChunk : Chunk
{
    /// <summary>
    /// The parsed literal value(s).
    /// </summary>
    public IReadOnlyList<Literal> Value { get; }

    /// <summary>
    /// Creates a literal chunk.
    /// </summary>
    public LiteralChunk(IReadOnlyList<Literal> value)
    {
        Value = value;
    }
}

/// <summary>
/// Represents a compiled form of a <see cref="LiteralChunk"/>, suitable for
/// matching against tokenized MLIR input.
/// </summary>
public abstract class Literal
{
    /// <summary>
    /// Parses the raw text extracted from a backtick-delimited ODS literal into a list of
    /// <see cref="Literal"/> instances.
    /// </summary>
    /// <remarks>
    /// Supported constructs:
    /// <list type="bullet">
    ///   <item>Empty string → <see cref="EmptyLiteral"/></item>
    ///   <item><c>\n</c> escape → <see cref="NewlineLiteral"/></item>
    ///   <item>One or more spaces → <see cref="WhitespaceLiteral"/></item>
    ///   <item><c>-&gt;</c> and other punctuation → <see cref="PunctuationLiteral"/></item>
    ///   <item>Identifier-like text → <see cref="KeywordLiteral"/></item>
    /// </list>
    /// </remarks>
    /// <param name="text">The raw content between the backticks.</param>
    /// <returns>A non-empty list of <see cref="Literal"/> instances.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="text"/> contains an unexpected character.</exception>
    public static IReadOnlyList<Literal> Parse(string text)
    {
        if (text.Length == 0)
            return new[] { (Literal)new EmptyLiteral() };

        var result = new List<Literal>();
        int pos = 0;
        while (pos < text.Length)
        {
            char c = text[pos];

            // Newline escape sequence: \n
            if (c == '\\' && pos + 1 < text.Length && text[pos + 1] == 'n')
            {
                result.Add(new NewlineLiteral());
                pos += 2;
                continue;
            }

            // Consecutive spaces → single WhitespaceLiteral
            if (c == ' ')
            {
                int start = pos;
                while (pos < text.Length && text[pos] == ' ')
                    pos++;
                result.Add(new WhitespaceLiteral(text.Substring(start, pos - start)));
                continue;
            }

            // Arrow: -> (must be checked before single-char Minus)
            if (c == '-' && pos + 1 < text.Length && text[pos + 1] == '>')
            {
                result.Add(new PunctuationLiteral(Text.TokenKind.Arrow));
                pos += 2;
                continue;
            }

            // Single-character punctuation
            Text.TokenKind punctKind;
            if (TryGetPunctuationKind(c, out punctKind))
            {
                result.Add(new PunctuationLiteral(punctKind));
                pos++;
                continue;
            }

            // Keyword / identifier
            if (IsIdentifierStart(c))
            {
                int start = pos;
                while (pos < text.Length && IsIdentifierChar(text[pos]))
                    pos++;
                result.Add(new KeywordLiteral(text.Substring(start, pos - start)));
                continue;
            }

            throw new FormatException(
                $"Unexpected character '{c}' in literal at position {pos}.");
        }

        return result;
    }

    private static bool TryGetPunctuationKind(char c, out Text.TokenKind kind)
    {
        switch (c)
        {
            case ',': kind = Text.TokenKind.Comma;       return true;
            case '(': kind = Text.TokenKind.LParen;      return true;
            case ')': kind = Text.TokenKind.RParen;      return true;
            case '[': kind = Text.TokenKind.LBracket;    return true;
            case ']': kind = Text.TokenKind.RBracket;    return true;
            case '{': kind = Text.TokenKind.LBrace;      return true;
            case '}': kind = Text.TokenKind.RBrace;      return true;
            case '<': kind = Text.TokenKind.LessThan;    return true;
            case '>': kind = Text.TokenKind.GreaterThan; return true;
            case '?': kind = Text.TokenKind.Question;    return true;
            case '*': kind = Text.TokenKind.Star;        return true;
            case '+': kind = Text.TokenKind.Plus;        return true;
            case '-': kind = Text.TokenKind.Minus;       return true;
            case '.': kind = Text.TokenKind.Dot;         return true;
            case ':': kind = Text.TokenKind.Colon;       return true;
            case '=': kind = Text.TokenKind.Equal;       return true;
            case '@': kind = Text.TokenKind.At;          return true;
            case '#': kind = Text.TokenKind.Hash;        return true;
            default:  kind = default;                    return false;
        }
    }

    private static bool IsIdentifierStart(char c) =>
        char.IsLetter(c) || c == '_';

    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';
}

/// <summary>
/// A literal corresponding to a single punctuation token (e.g. <c>(</c>, <c>,</c>, <c>-&gt;</c>).
/// </summary>
public sealed class PunctuationLiteral(Text.TokenKind tokenKind) : Literal
{
    /// <summary>
    /// The specific punctuation token kind expected in the input stream.
    /// </summary>
    public Text.TokenKind TokenKind { get; } = tokenKind;
}

/// <summary>
/// A literal corresponding to an identifier-like keyword that must match exactly
/// (e.g. <c>foo</c> in <c>`foo`</c>).
/// </summary>
public sealed class KeywordLiteral(string spelling) : Literal
{
    /// <summary>
    /// The exact string spelling that must appear in the input.
    /// </summary>
    public string Spelling { get; } = spelling;
}

/// <summary>
/// A literal representing a newline (<c>\n</c>) in the assembly format.
/// </summary>
public sealed class NewlineLiteral : Literal
{
}

/// <summary>
/// A literal that matches no tokens. Used as a placeholder or for optional constructs
/// that may compile to nothing.
/// </summary>
public sealed class EmptyLiteral : Literal
{
}

/// <summary>
/// A literal representing one or more space characters in the assembly format output.
/// </summary>
public sealed class WhitespaceLiteral(string spaces) : Literal
{
    /// <summary>
    /// The whitespace string (one or more space characters).
    /// </summary>
    public string Spaces { get; } = spaces;
}
