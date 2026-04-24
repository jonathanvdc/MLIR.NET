namespace TableGen.Text;

using System.Collections.Generic;
using System.Text;
using MLIR.Text;

/// <summary>
/// Converts raw TableGen source text into a stream of tokens for the parser.
/// </summary>
internal static class Lexer
{
    /// <summary>
    /// Lexes a TableGen source string.
    /// </summary>
    /// <param name="sourceDocument">The source document to tokenize.</param>
    /// <returns>The token sequence, including a trailing end-of-file token.</returns>
    public static IReadOnlyList<Token> Lex(SourceDocument sourceDocument)
    {
        var lexer = new LexerImpl(sourceDocument);
        return lexer.Lex();
    }

    /// <summary>
    /// Stateful lexer implementation that tracks the current character position and source location.
    /// </summary>
    private sealed class LexerImpl
    {
        /// <summary>
        /// Stores the source document being tokenized.
        /// </summary>
        private readonly SourceDocument sourceDocument;

        /// <summary>
        /// Stores the source text being tokenized.
        /// </summary>
        private readonly string source;

        /// <summary>
        /// Accumulates the emitted tokens.
        /// </summary>
        private readonly List<Token> tokens = new List<Token>();

        /// <summary>
        /// Tracks the current character offset.
        /// </summary>
        private int position;

        /// <summary>
        /// Tracks the current 1-based source line number.
        /// </summary>
        private int line = 1;

        /// <summary>
        /// Tracks the current 1-based source column number.
        /// </summary>
        private int column = 1;

        /// <summary>
        /// Initializes the lexer implementation.
        /// </summary>
        /// <param name="sourceDocument">The source document to tokenize.</param>
        public LexerImpl(SourceDocument sourceDocument)
        {
            this.sourceDocument = sourceDocument;
            source = sourceDocument.Text;
        }

        /// <summary>
        /// Tokenizes the entire source string.
        /// </summary>
        /// <returns>The emitted token sequence, including a trailing end-of-file token.</returns>
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
                    // Identifiers and keywords share the same spelling rules; keyword classification happens after reading.
                    var text = ReadWhile(static c => char.IsLetterOrDigit(c) || c == '_');
                    tokens.Add(new Token(GetKeywordKind(text), text, tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (char.IsDigit(current))
                {
                    // TableGen integer-like tokens can degrade back to identifiers when letters or underscores appear.
                    var text = ReadWhile(static c => char.IsLetterOrDigit(c) || c == '_');
                    var kind = TokenKind.Integer;
                    foreach (var ch in text)
                    {
                        if (char.IsLetter(ch) || ch == '_')
                        {
                            kind = TokenKind.Identifier;
                            break;
                        }
                    }

                    tokens.Add(new Token(kind, text, tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '-' && char.IsDigit(Peek(1)))
                {
                    // Negative integer literals are lexed as one token to keep the parser simple.
                    Advance();
                    var text = source.Substring(tokenStart, position - tokenStart) + ReadWhile(char.IsDigit);
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

                if (current == '!')
                {
                    Advance();
                    var operatorName = ReadWhile(static c => char.IsLetterOrDigit(c) || c == '_');
                    if (operatorName.Length == 0)
                    {
                        throw Error("Expected a bang operator name after '!'.");
                    }

                    tokens.Add(new Token(TokenKind.BangKeyword, operatorName, tokenStart, tokenLine, tokenColumn));
                    continue;
                }

                Advance();
                tokens.Add(new Token(GetPunctuationKind(current), current.ToString(), tokenStart, tokenLine, tokenColumn));
            }

            tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, position, line, column));
            return tokens;
        }

        /// <summary>
        /// Gets a value indicating whether the lexer has consumed all source characters.
        /// </summary>
        private bool IsAtEnd => position >= source.Length;

        /// <summary>
        /// Gets the current character under the lexer cursor.
        /// </summary>
        private char Current => source[position];

        /// <summary>
        /// Peeks ahead in the source without advancing the cursor.
        /// </summary>
        /// <param name="offset">The relative offset from the current position.</param>
        /// <returns>The peeked character or <c>'\0'</c> when out of bounds.</returns>
        private char Peek(int offset)
        {
            var index = position + offset;
            return index < source.Length ? source[index] : '\0';
        }

        /// <summary>
        /// Advances the lexer by one character while updating line and column counters.
        /// </summary>
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

        /// <summary>
        /// Skips whitespace, comments, and preprocessor directive lines that the parser should not see.
        /// </summary>
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

                if (Current == '#' &&
                    position + 1 < source.Length &&
                    char.IsLetter(source[position + 1]) &&
                    IsAtDirectiveStart())
                {
                    // Directive handling happens in the preprocessor; the lexer simply hides any residual lines.
                    while (!IsAtEnd && Current != '\n')
                    {
                        Advance();
                    }

                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Reads characters while the supplied predicate returns <see langword="true"/>.
        /// </summary>
        /// <param name="predicate">The continuation predicate.</param>
        /// <returns>The consumed substring.</returns>
        private string ReadWhile(System.Func<char, bool> predicate)
        {
            var start = position;
            while (!IsAtEnd && predicate(Current))
            {
                Advance();
            }

            return source.Substring(start, position - start);
        }

        /// <summary>
        /// Determines whether the current <c>#</c> begins a line-oriented preprocessor directive.
        /// </summary>
        /// <returns><see langword="true"/> when the current position is effectively at the start of a logical line.</returns>
        private bool IsAtDirectiveStart()
        {
            for (var i = position - 1; i >= 0; i--)
            {
                if (source[i] == '\n')
                {
                    return true;
                }

                if (!char.IsWhiteSpace(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads a quoted string literal and decodes the small escape set supported by the lexer.
        /// </summary>
        /// <returns>The decoded string contents.</returns>
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

        /// <summary>
        /// Reads a <c>[{ ... }]</c> code block literal, preserving its contents exactly.
        /// </summary>
        /// <returns>The raw code block contents without the wrapping delimiters.</returns>
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

        /// <summary>
        /// Classifies an identifier-like token as either a keyword or a regular identifier.
        /// </summary>
        /// <param name="text">The raw token text.</param>
        /// <returns>The appropriate token kind.</returns>
        private TokenKind GetKeywordKind(string text)
        {
            return text switch
            {
                "class" => TokenKind.ClassKeyword,
                "def" => TokenKind.DefKeyword,
                "defvar" => TokenKind.DefVarKeyword,
                "let" => TokenKind.LetKeyword,
                "in" => TokenKind.InKeyword,
                "include" => TokenKind.IncludeKeyword,
                "assert" => TokenKind.AssertKeyword,
                _ => TokenKind.Identifier,
            };
        }

        /// <summary>
        /// Maps a punctuation character to its token kind.
        /// </summary>
        /// <param name="c">The punctuation character.</param>
        /// <returns>The corresponding token kind.</returns>
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
                '#' => TokenKind.Hash,
                '.' => TokenKind.Dot,
                '?' => TokenKind.QuestionMark,
                _ => throw Error($"Unexpected character '{c}'."),
            };
        }

        /// <summary>
        /// Creates a parse exception at the lexer's current source position.
        /// </summary>
        /// <param name="message">The diagnostic message.</param>
        /// <returns>The constructed parse exception.</returns>
        private ParseException Error(string message)
        {
            return new ParseException(new Diagnostic(message, line, column, sourceDocument.FileName));
        }
    }
}
