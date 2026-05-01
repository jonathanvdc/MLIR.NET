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
    /// <returns>The lexing result, including a trailing end-of-file token on success.</returns>
    public static ParseResult<IReadOnlyList<Token>> Lex(SourceDocument sourceDocument)
    {
        return new LexerImpl(sourceDocument).Lex();
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
        /// Initializes the lexer implementation.
        /// </summary>
        /// <param name="sourceDocument">The source document to tokenize.</param>
        public LexerImpl(SourceDocument sourceDocument)
        {
            this.sourceDocument = sourceDocument;
            source = sourceDocument.GetText(0, sourceDocument.Length);
        }

        /// <summary>
        /// Tokenizes the entire source string.
        /// </summary>
        /// <returns>The emitted token sequence, including a trailing end-of-file token.</returns>
        public ParseResult<IReadOnlyList<Token>> Lex()
        {
            while (!IsAtEnd)
            {
                var triviaDiagnostic = SkipTrivia();
                if (triviaDiagnostic is not null)
                {
                    return ParseResult<IReadOnlyList<Token>>.Failure(triviaDiagnostic);
                }

                if (IsAtEnd)
                {
                    break;
                }

                var tokenStart = position;
                var current = Current;

                if (char.IsLetter(current) || current == '_')
                {
                    // Identifiers and keywords share the same spelling rules; keyword classification happens after reading.
                    var text = ReadWhile(static c => char.IsLetterOrDigit(c) || c == '_');
                    AddToken(GetKeywordKind(text), text, tokenStart);
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

                    AddToken(kind, text, tokenStart);
                    continue;
                }

                if (current == '-' && char.IsDigit(Peek(1)))
                {
                    // Negative integer literals are lexed as one token to keep the parser simple.
                    Advance();
                    var text = source.Substring(tokenStart, position - tokenStart) + ReadWhile(char.IsDigit);
                    AddToken(TokenKind.Integer, text, tokenStart);
                    continue;
                }

                if (current == '"')
                {
                    var literal = ReadStringLiteral();
                    if (!literal.IsSuccess)
                    {
                        return ParseResult<IReadOnlyList<Token>>.Failure(literal.Diagnostic!);
                    }

                    AddToken(TokenKind.String, literal.Value, tokenStart);
                    continue;
                }

                if (current == '[' && Peek(1) == '{')
                {
                    var literal = ReadCodeBlockLiteral();
                    if (!literal.IsSuccess)
                    {
                        return ParseResult<IReadOnlyList<Token>>.Failure(literal.Diagnostic!);
                    }

                    AddToken(TokenKind.CodeBlock, literal.Value, tokenStart);
                    continue;
                }

                if (current == '!')
                {
                    Advance();
                    var operatorName = ReadWhile(static c => char.IsLetterOrDigit(c) || c == '_');
                    if (operatorName.Length == 0)
                    {
                        return Error<IReadOnlyList<Token>>("Expected a bang operator name after '!'.");
                    }

                    AddToken(TokenKind.BangKeyword, operatorName, tokenStart);
                    continue;
                }

                var punctuation = GetPunctuationKind(current);
                if (!punctuation.IsSuccess)
                {
                    return ParseResult<IReadOnlyList<Token>>.Failure(punctuation.Diagnostic!);
                }

                Advance();
                AddToken(punctuation.Value, current.ToString(), tokenStart);
            }

            tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, new SourceLocation(sourceDocument, position, 0)));
            return ParseResult<IReadOnlyList<Token>>.Success(tokens);
        }

        /// <summary>
        /// Adds a token whose span runs from <paramref name="tokenStart"/> to the current lexer position.
        /// </summary>
        private void AddToken(TokenKind kind, string text, int tokenStart)
        {
            tokens.Add(new Token(kind, text, new SourceLocation(sourceDocument, tokenStart, position - tokenStart)));
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
        /// Advances the lexer by one character.
        /// </summary>
        private void Advance()
        {
            position++;
        }

        /// <summary>
        /// Skips whitespace, comments, and preprocessor directive lines that the parser should not see.
        /// </summary>
        private Diagnostic? SkipTrivia()
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
                        return ErrorDiagnostic("Unterminated block comment.");
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

            return null;
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
        private ParseResult<string> ReadStringLiteral()
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
                        return Error<string>("Unterminated string literal.");
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
                return Error<string>("Unterminated string literal.");
            }

            Advance();
            return ParseResult<string>.Success(builder.ToString());
        }

        /// <summary>
        /// Reads a <c>[{ ... }]</c> code block literal, preserving its contents exactly.
        /// </summary>
        /// <returns>The raw code block contents without the wrapping delimiters.</returns>
        private ParseResult<string> ReadCodeBlockLiteral()
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
                    return ParseResult<string>.Success(builder.ToString());
                }

                builder.Append(Current);
                Advance();
            }

            return Error<string>("Unterminated code block literal.");
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
        private ParseResult<TokenKind> GetPunctuationKind(char c)
        {
            var kind = c switch
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
                _ => (TokenKind?)null,
            };

            return kind.HasValue
                ? ParseResult<TokenKind>.Success(kind.Value)
                : Error<TokenKind>($"Unexpected character '{c}'.");
        }

        /// <summary>
        /// Creates a parse failure at the lexer's current source position.
        /// </summary>
        /// <param name="message">The diagnostic message.</param>
        /// <returns>The parse result containing the diagnostic.</returns>
        private ParseResult<T> Error<T>(string message)
        {
            return ParseResult<T>.Failure(ErrorDiagnostic(message));
        }

        /// <summary>
        /// Creates a diagnostic at the lexer's current source position.
        /// </summary>
        /// <param name="message">The diagnostic message.</param>
        /// <returns>The constructed diagnostic.</returns>
        private Diagnostic ErrorDiagnostic(string message)
        {
            var start = Math.Min(position, sourceDocument.Length);
            var length = start < sourceDocument.Length ? 1 : 0;
            return new Diagnostic(message, new SourceLocation(sourceDocument, start, length));
        }
    }
}
