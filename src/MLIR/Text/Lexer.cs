namespace MLIR.Text;

using MLIR.Text;

using System.Collections.Generic;
using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Tokenizes generic MLIR syntax while preserving leading trivia on every token.
/// </summary>
internal static class Lexer
{
    /// <summary>
    /// Core lexing routine. Produces <see cref="Token"/> instances backed by
    /// <paramref name="document"/> so that every token carries document-relative offset
    /// information for on-demand line/column resolution.
    /// </summary>
    internal static ParseResult<IReadOnlyList<Token>> TryLexCore(string source, SourceDocument document)
    {
        var tokens = new List<Token>();
        var index = 0;

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
                    index++;
                    continue;
                }

                if (ch == '/' && index + 1 < source.Length && source[index + 1] == '/')
                {
                    while (index < source.Length && source[index] != '\n')
                    {
                        index++;
                    }

                    continue;
                }

                break;
            }

            var leadingTrivia = source.Substring(triviaStart, index - triviaStart);
            if (index >= source.Length)
            {
                tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, leadingTrivia, document, index, 0));
                return ParseResult<IReadOnlyList<Token>>.Success(tokens);
            }

            var tokenStart = index;
            var chAtToken = source[index];

            if (chAtToken == '@' && index + 1 < source.Length && IsSymbolNameStart(source[index + 1]))
            {
                index++;
                if (source[index] == '"')
                {
                    var stringEndResult = TryConsumeStringLiteral(source, document, tokenStart, ref index);
                    if (!stringEndResult.IsSuccess)
                    {
                        return ParseResult<IReadOnlyList<Token>>.Failure(stringEndResult.Diagnostic!);
                    }
                }
                else
                {
                    index++;
                    while (index < source.Length && IsIdentifierPart(source[index]))
                    {
                        index++;
                    }
                }

                tokens.Add(new Token(TokenKind.SymbolName, source.Substring(tokenStart, index - tokenStart), leadingTrivia, document, tokenStart, index - tokenStart));
                continue;
            }

            if (chAtToken == '%' || chAtToken == '^')
            {
                var tokenKind = chAtToken == '%' ? TokenKind.SsaName : TokenKind.BlockLabel;
                index++;
                if (index >= source.Length || (!IsIdentifierStart(source[index]) && !char.IsDigit(source[index])))
                {
                    var location = new SourceLocation(document, tokenStart, 1);
                    return ParseResult<IReadOnlyList<Token>>.Failure(new Diagnostic($"Expected a name after '{chAtToken}'.", location));
                }

                while (index < source.Length && IsIdentifierPart(source[index]))
                {
                    index++;
                }

                tokens.Add(new Token(tokenKind, source.Substring(tokenStart, index - tokenStart), leadingTrivia, document, tokenStart, index - tokenStart));
                continue;
            }

            if (IsIdentifierStart(chAtToken))
            {
                index++;
                while (index < source.Length && IsIdentifierPart(source[index]))
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.Identifier, source.Substring(tokenStart, index - tokenStart), leadingTrivia, document, tokenStart, index - tokenStart));
                continue;
            }

            if (char.IsDigit(chAtToken))
            {
                index++;
                while (index < source.Length && char.IsDigit(source[index]))
                {
                    index++;
                }

                tokens.Add(new Token(TokenKind.Integer, source.Substring(tokenStart, index - tokenStart), leadingTrivia, document, tokenStart, index - tokenStart));
                continue;
            }

            if (chAtToken == '"')
            {
                var stringEndResult = TryConsumeStringLiteral(source, document, tokenStart, ref index);
                if (!stringEndResult.IsSuccess)
                {
                    return ParseResult<IReadOnlyList<Token>>.Failure(stringEndResult.Diagnostic!);
                }

                tokens.Add(new Token(TokenKind.StringLiteral, source.Substring(tokenStart, index - tokenStart), leadingTrivia, document, tokenStart, index - tokenStart));
                continue;
            }

            var kind = chAtToken switch
            {
                '@' => TokenKind.At,
                '#' => TokenKind.Hash,
                '!' => TokenKind.Bang,
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
                '|' => TokenKind.Pipe,
                '-' => index + 1 < source.Length && source[index + 1] == '>' ? TokenKind.Arrow : TokenKind.Minus,
                _ => TokenKind.EndOfFile,
            };

            if (kind == TokenKind.EndOfFile)
            {
                var location = new SourceLocation(document, tokenStart, 1);
                return ParseResult<IReadOnlyList<Token>>.Failure(new Diagnostic($"Unexpected character '{chAtToken}'.", location));
            }

            index++;
            if (kind == TokenKind.Arrow)
            {
                // The switch only classifies '-' as an arrow when the next character is '>',
                // so it is safe to consume the second character here.
                index++;
            }

            tokens.Add(new Token(kind, source.Substring(tokenStart, index - tokenStart), leadingTrivia, document, tokenStart, index - tokenStart));
        }
    }

    /// <summary>
    /// Tries to lex MLIR source text into a token stream without throwing on lexical failures.
    /// </summary>
    /// <param name="source">The source text to tokenize.</param>
    /// <param name="tokens">The resulting token stream when lexing succeeds.</param>
    /// <param name="diagnostic">The diagnostic that describes the lexical failure, if any.</param>
    /// <returns><see langword="true"/> when lexing succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryLex(string source, out IReadOnlyList<Token> tokens, out Diagnostic? diagnostic)
    {
        source ??= string.Empty;
        var document = new StringDocument(string.Empty, source);
        var result = TryLexCore(source, document);
        if (result.IsSuccess)
        {
            tokens = result.Value;
            diagnostic = null;
            return true;
        }

        tokens = [];
        diagnostic = result.Diagnostic;
        return false;
    }

    /// <summary>
    /// Lexes MLIR source text into a token stream.
    /// </summary>
    /// <param name="source">The source text to tokenize.</param>
    /// <returns>The resulting token stream, including an end-of-file token.</returns>
    /// <exception cref="ParseException">Thrown when the source contains invalid lexical syntax.</exception>
    public static IReadOnlyList<Token> Lex(string source)
    {
        source ??= string.Empty;
        var document = new StringDocument(string.Empty, source);
        var result = TryLexCore(source, document);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private static ParseResult<bool> TryConsumeStringLiteral(string source, SourceDocument document, int tokenStart, ref int index)
    {
        index++;
        var escaped = false;
        while (index < source.Length)
        {
            var current = source[index];
            index++;
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
                return ParseResult<bool>.Success(true);
            }
        }

        var location = new SourceLocation(document, tokenStart, 1);
        return ParseResult<bool>.Failure(new Diagnostic("Unterminated string literal.", location));
    }

    private static bool IsSymbolNameStart(char ch)
    {
        return ch == '"' || IsIdentifierStart(ch);
    }

    private static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_' || ch == '$';
    }

    private static bool IsIdentifierPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '$' || ch == '.' || ch == '-';
    }
}
