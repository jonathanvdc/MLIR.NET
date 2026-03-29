namespace TableGen.Text;

using System.Collections.Generic;
using System.Text;

internal static class Lexer
{
    public static IReadOnlyList<Token> Lex(string source)
    {
        var lexer = new LexerImpl(source);
        return lexer.Lex();
    }

    private sealed class LexerImpl
    {
        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private int position;
        private int line = 1;
        private int column = 1;

        public LexerImpl(string source)
        {
            this.source = source;
        }

        public IReadOnlyList<Token> Lex()
        {
            while (!IsAtEnd)
            {
                SkipTrivia();
                if (IsAtEnd)
                {
                    break;
                }

                var tokenStart = position;
                var tokenLine = line;
                var tokenColumn = column;
                var current = Current;

                if (char.IsLetter(current) || current == '_')
                {
                    var text = ReadWhile(static c => char.IsLetterOrDigit(c) || c == '_');
                    tokens.Add(new Token(GetKeywordKind(text), text, tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (char.IsDigit(current))
                {
                    var text = ReadWhile(char.IsDigit);
                    tokens.Add(new Token(TokenKind.Integer, text, tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '"')
                {
                    tokens.Add(new Token(TokenKind.String, ReadStringLiteral(), tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '[' && Peek(1) == '{')
                {
                    tokens.Add(new Token(TokenKind.CodeBlock, ReadCodeBlockLiteral(), tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                Advance();
                tokens.Add(new Token(GetPunctuationKind(current), current.ToString(), tokenStart, tokenLine, tokenColumn));
            }

            tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, position, line, column));
            return tokens;
        }

        private bool IsAtEnd => position >= source.Length;

        private char Current => source[position];

        private char Peek(int offset)
        {
            var index = position + offset;
            return index < source.Length ? source[index] : '\0';
        }

        private void Advance()
        {
            if (source[position] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }

            position++;
        }

        private void SkipTrivia()
        {
            while (!IsAtEnd)
            {
                if (char.IsWhiteSpace(Current))
                {
                    Advance();
                    continue;
                }

                if (Current == '/' && Peek(1) == '/')
                {
                    while (!IsAtEnd && Current != '\n')
                    {
                        Advance();
                    }

                    continue;
                }

                if (Current == '/' && Peek(1) == '*')
                {
                    Advance();
                    Advance();
                    while (!IsAtEnd && !(Current == '*' && Peek(1) == '/'))
                    {
                        Advance();
                    }

                    if (IsAtEnd)
                    {
                        throw Error("Unterminated block comment.");
                    }

                    Advance();
                    Advance();
                    continue;
                }

                break;
            }
        }

        private string ReadWhile(System.Func<char, bool> predicate)
        {
            var start = position;
            while (!IsAtEnd && predicate(Current))
            {
                Advance();
            }

            return source.Substring(start, position - start);
        }

        private string ReadStringLiteral()
        {
            var builder = new StringBuilder();
            Advance();

            while (!IsAtEnd && Current != '"')
            {
                if (Current == '\\')
                {
                    Advance();
                    if (IsAtEnd)
                    {
                        throw Error("Unterminated string literal.");
                    }

                    builder.Append(Current switch
                    {
                        '\\' => '\\',
                        '"' => '"',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => Current,
                    });
                    Advance();
                    continue;
                }

                builder.Append(Current);
                Advance();
            }

            if (IsAtEnd)
            {
                throw Error("Unterminated string literal.");
            }

            Advance();
            return builder.ToString();
        }

        private string ReadCodeBlockLiteral()
        {
            var builder = new StringBuilder();
            Advance();
            Advance();

            while (!IsAtEnd)
            {
                if (Current == '}' && Peek(1) == ']')
                {
                    Advance();
                    Advance();
                    return builder.ToString();
                }

                builder.Append(Current);
                Advance();
            }

            throw Error("Unterminated code block literal.");
        }

        private TokenKind GetKeywordKind(string text)
        {
            return text switch
            {
                "class" => TokenKind.ClassKeyword,
                "def" => TokenKind.DefKeyword,
                "let" => TokenKind.LetKeyword,
                "in" => TokenKind.InKeyword,
                _ => TokenKind.Identifier,
            };
        }

        private TokenKind GetPunctuationKind(char c)
        {
            return c switch
            {
                ':' => TokenKind.Colon,
                ';' => TokenKind.Semicolon,
                ',' => TokenKind.Comma,
                '=' => TokenKind.Equal,
                '<' => TokenKind.LessThan,
                '>' => TokenKind.GreaterThan,
                '{' => TokenKind.LBrace,
                '}' => TokenKind.RBrace,
                '(' => TokenKind.LParen,
                ')' => TokenKind.RParen,
                '[' => TokenKind.LBracket,
                ']' => TokenKind.RBracket,
                '$' => TokenKind.Dollar,
                _ => throw Error($"Unexpected character '{c}'."),
            };
        }

        private ParseException Error(string message)
        {
            return new ParseException(new Diagnostic(message, line, column));
        }
    }
}
