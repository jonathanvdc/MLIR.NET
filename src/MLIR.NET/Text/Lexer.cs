namespace MLIR.Text;

using System.Collections.Generic;

/// <summary>
/// Tokenizes generic MLIR syntax while preserving leading trivia on every token.
/// </summary>
internal static class Lexer
{
    /// <summary>
    /// Lexes MLIR source text into a token stream.
    /// </summary>
    /// <param name="source">The source text to tokenize.</param>
    /// <returns>The resulting token stream, including an end-of-file token.</returns>
    /// <exception cref="ParseException">Thrown when the source contains invalid lexical syntax.</exception>
    public static IReadOnlyList<Token> Lex(string source)
    {
        var tokens = new List<Token>();
        var index = 0;
        var line = 1;
        var column = 1;

        while (true)
        {
            var triviaStart = index;

            // Trivia stays attached to the following token so the CST can round-trip
            // comments and spacing without introducing separate trivia nodes.
            while (index < source.Length)
            {
                var ch = source[index];
                if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
                {
                    Advance(ch, ref index, ref line, ref column);
                    continue;
                }

                if (ch == '/' && index + 1 < source.Length && source[index + 1] == '/')
                {
                    while (index < source.Length && source[index] != '\n')
                    {
                        Advance(source[index], ref index, ref line, ref column);
                    }

                    continue;
                }

                break;
            }

            var leadingTrivia = source.Substring(triviaStart, index - triviaStart);
            if (index >= source.Length)
            {
                tokens.Add(new Token(TokenKind.EndOfFile, leadingTrivia, string.Empty, triviaStart, index, index, line, column));
                return tokens;
            }

            var tokenStart = index;
            var tokenLine = line;
            var tokenColumn = column;
            var chAtToken = source[index];

            if (chAtToken == '%' || chAtToken == '^')
            {
                var tokenKind = chAtToken == '%' ? TokenKind.SsaName : TokenKind.BlockLabel;
                Advance(chAtToken, ref index, ref line, ref column);
                if (index >= source.Length || (!IsIdentifierStart(source[index]) && !char.IsDigit(source[index])))
                {
                    throw new ParseException(new Diagnostic($"Expected a name after '{chAtToken}'.", tokenLine, tokenColumn));
                }

                while (index < source.Length && IsIdentifierPart(source[index]))
                {
                    Advance(source[index], ref index, ref line, ref column);
                }

                tokens.Add(new Token(tokenKind, leadingTrivia, source.Substring(tokenStart, index - tokenStart), triviaStart, tokenStart, index, tokenLine, tokenColumn));
                continue;
            }

            if (IsIdentifierStart(chAtToken))
            {
                Advance(chAtToken, ref index, ref line, ref column);
                while (index < source.Length && IsIdentifierPart(source[index]))
                {
                    Advance(source[index], ref index, ref line, ref column);
                }

                tokens.Add(new Token(TokenKind.Identifier, leadingTrivia, source.Substring(tokenStart, index - tokenStart), triviaStart, tokenStart, index, tokenLine, tokenColumn));
                continue;
            }

            if (char.IsDigit(chAtToken))
            {
                Advance(chAtToken, ref index, ref line, ref column);
                while (index < source.Length && char.IsDigit(source[index]))
                {
                    Advance(source[index], ref index, ref line, ref column);
                }

                tokens.Add(new Token(TokenKind.Integer, leadingTrivia, source.Substring(tokenStart, index - tokenStart), triviaStart, tokenStart, index, tokenLine, tokenColumn));
                continue;
            }

            if (chAtToken == '"')
            {
                Advance(chAtToken, ref index, ref line, ref column);
                var escaped = false;
                while (index < source.Length)
                {
                    var current = source[index];
                    Advance(current, ref index, ref line, ref column);
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (current == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (current == '"')
                    {
                        break;
                    }
                }

                if (index == tokenStart + 1 || source[index - 1] != '"')
                {
                    throw new ParseException(new Diagnostic("Unterminated string literal.", tokenLine, tokenColumn));
                }

                tokens.Add(new Token(TokenKind.StringLiteral, leadingTrivia, source.Substring(tokenStart, index - tokenStart), triviaStart, tokenStart, index, tokenLine, tokenColumn));
                continue;
            }

            var kind = chAtToken switch
            {
                '@' => TokenKind.At,
                '#' => TokenKind.Hash,
                ':' => TokenKind.Colon,
                ',' => TokenKind.Comma,
                '=' => TokenKind.Equal,
                '(' => TokenKind.LParen,
                ')' => TokenKind.RParen,
                '{' => TokenKind.LBrace,
                '}' => TokenKind.RBrace,
                '[' => TokenKind.LBracket,
                ']' => TokenKind.RBracket,
                '<' => TokenKind.LessThan,
                '>' => TokenKind.GreaterThan,
                '?' => TokenKind.Question,
                '*' => TokenKind.Star,
                '+' => TokenKind.Plus,
                '.' => TokenKind.Dot,
                '-' => index + 1 < source.Length && source[index + 1] == '>' ? TokenKind.Arrow : TokenKind.Minus,
                _ => throw new ParseException(new Diagnostic($"Unexpected character '{chAtToken}'.", tokenLine, tokenColumn)),
            };

            Advance(chAtToken, ref index, ref line, ref column);
            if (kind == TokenKind.Arrow)
            {
                // The switch only classifies '-' as an arrow when the next character is '>',
                // so it is safe to consume the second character here.
                Advance(source[index], ref index, ref line, ref column);
            }

            tokens.Add(new Token(kind, leadingTrivia, source.Substring(tokenStart, index - tokenStart), triviaStart, tokenStart, index, tokenLine, tokenColumn));
        }
    }

    private static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_' || ch == '$';
    }

    private static bool IsIdentifierPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '$' || ch == '.' || ch == '-';
    }

    private static void Advance(char ch, ref int index, ref int line, ref int column)
    {
        index++;
        if (ch == '\n')
        {
            line++;
            column = 1;
        }
        else
        {
            column++;
        }
    }
}
