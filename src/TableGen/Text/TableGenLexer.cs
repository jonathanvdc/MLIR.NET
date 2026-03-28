namespace TableGen.Text;

using System.Collections.Generic;
using System.Text;

internal static class TableGenLexer
{
    public static IReadOnlyList<TableGenToken> Lex(string source)
    {
        var lexer = new Lexer(source);
        return lexer.Lex();
    }

    private sealed class Lexer
    {
        private readonly string source;
        private readonly List<TableGenToken> tokens = new List<TableGenToken>();
        private int position;
        private int line = 1;
        private int column = 1;

        public Lexer(string source)
        {
            this.source = source;
        }

        public IReadOnlyList<TableGenToken> Lex()
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
                    tokens.Add(new TableGenToken(GetKeywordKind(text), text, tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (char.IsDigit(current))
                {
                    var text = ReadWhile(char.IsDigit);
                    tokens.Add(new TableGenToken(TableGenTokenKind.Integer, text, tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '"')
                {
                    tokens.Add(new TableGenToken(TableGenTokenKind.String, ReadStringLiteral(), tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '[' && Peek(1) == '{')
                {
                    tokens.Add(new TableGenToken(TableGenTokenKind.CodeBlock, ReadCodeBlockLiteral(), tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                Advance();
                tokens.Add(new TableGenToken(GetPunctuationKind(current), current.ToString(), tokenStart, tokenLine, tokenColumn));
            }

            tokens.Add(new TableGenToken(TableGenTokenKind.EndOfFile, string.Empty, position, line, column));
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

        private TableGenTokenKind GetKeywordKind(string text)
        {
            return text switch
            {
                "class" => TableGenTokenKind.ClassKeyword,
                "def" => TableGenTokenKind.DefKeyword,
                "let" => TableGenTokenKind.LetKeyword,
                "in" => TableGenTokenKind.InKeyword,
                _ => TableGenTokenKind.Identifier,
            };
        }

        private TableGenTokenKind GetPunctuationKind(char c)
        {
            return c switch
            {
                ':' => TableGenTokenKind.Colon,
                ';' => TableGenTokenKind.Semicolon,
                ',' => TableGenTokenKind.Comma,
                '=' => TableGenTokenKind.Equal,
                '<' => TableGenTokenKind.LessThan,
                '>' => TableGenTokenKind.GreaterThan,
                '{' => TableGenTokenKind.LBrace,
                '}' => TableGenTokenKind.RBrace,
                '(' => TableGenTokenKind.LParen,
                ')' => TableGenTokenKind.RParen,
                '[' => TableGenTokenKind.LBracket,
                ']' => TableGenTokenKind.RBracket,
                '$' => TableGenTokenKind.Dollar,
                _ => throw Error($"Unexpected character '{c}'."),
            };
        }

        private TableGenParseException Error(string message)
        {
            return new TableGenParseException(new TableGenDiagnostic(message, line, column));
        }
    }
}
