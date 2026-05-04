namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Syntax;
using MLIR.Dialects;

public sealed partial class Parser
{
    /// <summary>
    /// Determines whether the current <c>{</c> token begins a region rather than an attribute dictionary.
    /// </summary>
    /// <remarks>
    /// Both regions and attribute dictionaries start with <c>{</c>, so one token of lookahead is
    /// needed to disambiguate:
    /// <list type="bullet">
    ///   <item><description><c>{ }</c> (empty brace pair) → attribute dictionary.</description></item>
    ///   <item><description><c>{ ^label</c>, <c>{ "string"</c>, or <c>{ %ssa</c> → region.</description></item>
    ///   <item><description><c>{ identifier =</c>, <c>{ identifier :</c>, or <c>{ identifier ,</c> → attribute dictionary.</description></item>
    ///   <item><description><c>{ identifier ...</c> (any other follower) → region.</description></item>
    /// </list>
    /// The method peeks at tokens without consuming any.
    /// </remarks>
    private bool IsRegionStart()
    {
        if (!Is(TokenKind.LBrace))
        {
            return false;
        }

        // A '{' can start either a region or an attribute dictionary. Peek ahead to decide
        // which production we are looking at without consuming any tokens.
        var lookahead = position + 1;
        if (tokens[lookahead].TokenKind == TokenKind.RBrace)
        {
            return false;
        }

        if (tokens[lookahead].TokenKind == TokenKind.BlockLabel || tokens[lookahead].TokenKind == TokenKind.StringLiteral || tokens[lookahead].TokenKind == TokenKind.SsaName)
        {
            return true;
        }

        if (tokens[lookahead].TokenKind != TokenKind.Identifier)
        {
            return false;
        }

        var secondLookahead = tokens[lookahead + 1];
        return secondLookahead.TokenKind != TokenKind.Equal
            && secondLookahead.TokenKind != TokenKind.Colon
            && secondLookahead.TokenKind != TokenKind.Comma;
    }

    /// <summary>
    /// Verifies that the current token represents an operation boundary, returning a failure diagnostic
    /// when it does not. Used after every top-level or block-level operation.
    /// </summary>
    /// <param name="allowBlockStart">
    /// When <see langword="true"/>, a block label token preceded by a newline is also accepted as a boundary.
    /// </param>
    private ParseResult<bool> EnsureOperationBoundary(bool allowBlockStart)
    {
        return IsOperationBoundary(Current, allowBlockStart)
            ? ParseResult<bool>.Success(true)
            : ParseResult<bool>.Failure(CreateDiagnostic("Expected the end of the operation."));
    }

    /// <summary>
    /// Determines whether <paramref name="token"/> represents the start of a new operation (i.e.,
    /// the end of the previous one).
    /// </summary>
    /// <remarks>
    /// A token is an operation boundary when:
    /// <list type="bullet">
    ///   <item><description>It is <see cref="TokenKind.EndOfFile"/> or <c>}</c> (closing a region).</description></item>
    ///   <item><description>Its <see cref="Token.LeadingTrivia"/> contains a newline, meaning the token
    ///     begins on a new source line.</description></item>
    ///   <item><description><paramref name="allowBlockStart"/> is <see langword="true"/> and the token is
    ///     a block label (<c>^</c>) preceded by a newline.</description></item>
    /// </list>
    /// This mirrors the upstream MLIR parser's behavior: MLIR separates operations by newlines rather
    /// than by explicit delimiter tokens.
    /// </remarks>
    private bool IsOperationBoundary(Token token, bool allowBlockStart)
    {
        if (token.TokenKind == TokenKind.EndOfFile || token.TokenKind == TokenKind.RBrace)
        {
            return true;
        }

        if (allowBlockStart && token.TokenKind == TokenKind.BlockLabel && token.LeadingTrivia?.Contains('\n') == true)
        {
            return true;
        }

        return token.LeadingTrivia?.Contains('\n') == true;
    }

    /// <summary>
    /// Updates bracket-depth counters based on the supplied token kind.
    /// Called by <see cref="TryScanRawFragment"/> to track nesting so that delimiters or boundaries
    /// inside nested brackets are not treated as stopping points.
    /// </summary>
    private static void UpdateDepth(TokenKind kind, ref int depthParen, ref int depthBrace, ref int depthBracket, ref int depthAngle)
    {
        switch (kind)
        {
            case TokenKind.LParen:
                depthParen++;
                break;
            case TokenKind.RParen:
                depthParen--;
                break;
            case TokenKind.LBrace:
                depthBrace++;
                break;
            case TokenKind.RBrace:
                depthBrace--;
                break;
            case TokenKind.LBracket:
                depthBracket++;
                break;
            case TokenKind.RBracket:
                depthBracket--;
                break;
            case TokenKind.LessThan:
                depthAngle++;
                break;
            case TokenKind.GreaterThan:
                depthAngle--;
                break;
        }
    }

    /// <summary>
    /// Copies a contiguous sub-range of the token list into a new <see cref="List{T}"/>.
    /// </summary>
    /// <param name="tokens">The full token list from the lexer.</param>
    /// <param name="start">Inclusive start index in <paramref name="tokens"/>.</param>
    /// <param name="end">Exclusive end index in <paramref name="tokens"/>.</param>
    private static List<Token> CreateSyntaxTokenList(IReadOnlyList<Token> tokens, int start, int end)
    {
        var result = new List<Token>(end - start);
        for (var i = start; i < end; i++)
        {
            result.Add(tokens[i]);
        }

        return result;
    }

    /// <summary>
    /// Attempts to consume the current token if its kind matches <paramref name="kind"/>.
    /// Returns <see langword="true"/> and advances the position on a match; returns <see langword="false"/>
    /// and leaves the position unchanged otherwise.
    /// </summary>
    private bool TryMatch(TokenKind kind, out Token token)
    {
        if (Current.TokenKind != kind)
        {
            token = default;
            return false;
        }

        token = ConsumeToken();
        return true;
    }

    /// <summary>
    /// Expects the current token to have the supplied <paramref name="kind"/> and returns it as a
    /// <see cref="Token"/> on success. Returns a failure diagnostic with <paramref name="message"/>
    /// when the token does not match.
    /// </summary>
    internal ParseResult<Token> ExpectToken(TokenKind kind, string message)
    {
        if (!TryMatch(kind, out var token))
        {
            return ParseResult<Token>.Failure(CreateDiagnostic(message));
        }

        return ParseResult<Token>.Success(token);
    }

    /// <summary>Returns <see langword="true"/> when the current token has the supplied <paramref name="kind"/>.</summary>
    private bool Is(TokenKind kind)
    {
        return Current.TokenKind == kind;
    }

    /// <summary>
    /// Creates a <see cref="ParseMark"/> that captures the current token position for backtracking.
    /// Pair with <see cref="Reset"/> to restore the position when a speculative parse returns
    /// <see cref="ParseOutcome.NoMatch"/>.
    /// </summary>
    private ParseMark Mark()
    {
        return new ParseMark(position);
    }

    /// <summary>
    /// Restores the token position to the value captured by <paramref name="mark"/>.
    /// Only call after a parse step that returned <see cref="ParseOutcome.NoMatch"/>; do not
    /// reset after a hard <see cref="ParseOutcome.Error"/> because that would silently discard
    /// the committed diagnostic.
    /// </summary>
    private void Reset(ParseMark mark)
    {
        position = mark.Position;
    }

    /// <summary>
    /// Parses an optional comma-separated, delimited list.
    /// If the opening token (<paramref name="openKind"/>) is absent the method returns an empty list
    /// rather than a failure result, making the entire construct optional.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<T>> TryParseOptionalCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<ParseResult<T>> parseElement,
        string closeMessage)
        where T : IHasSourceLocation
    {
        if (!TryMatch(openKind, out var openToken))
        {
            return ParseResult<DelimitedSyntaxList<T>>.Success(EmptyDelimitedSyntaxList<T>());
        }

        return TryParseCommaSeparatedDelimitedListCore(openToken, closeKind, parseElement, closeMessage);
    }

    /// <summary>
    /// Parses a required comma-separated, delimited list. Fails with
    /// <paramref name="openMessage"/> when the opening token is absent.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<T>> TryParseRequiredCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<ParseResult<T>> parseElement,
        string openMessage,
        string closeMessage)
        where T : IHasSourceLocation
    {
        return ExpectToken(openKind, openMessage)
            .Bind(openToken => TryParseCommaSeparatedDelimitedListCore(openToken, closeKind, parseElement, closeMessage));
    }

    /// <summary>
    /// Core loop for parsing a comma-separated list after the opening token has already been consumed.
    /// Reads items separated by commas until the closing token <paramref name="closeKind"/> is found,
    /// then wraps the result in a <see cref="DelimitedSyntaxList{T}"/>.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<T>> TryParseCommaSeparatedDelimitedListCore<T>(
        Token openToken,
        TokenKind closeKind,
        Func<ParseResult<T>> parseElement,
        string closeMessage)
        where T : IHasSourceLocation
    {
        var items = new List<T>();
        var separators = new List<Token>();
        if (!TryMatch(closeKind, out var closeToken))
        {
            var itemsResult = TryParseCommaSeparatedItems(items, separators, parseElement);
            if (!itemsResult.IsSuccess)
            {
                return ParseResult<DelimitedSyntaxList<T>>.Failure(itemsResult.Diagnostic!);
            }

            var closeTokenResult = ExpectToken(closeKind, closeMessage);
            if (!closeTokenResult.TryGetValue(out closeToken))
            {
                return ParseResult<DelimitedSyntaxList<T>>.Failure(closeTokenResult.Diagnostic!);
            }
        }

        return ParseResult<DelimitedSyntaxList<T>>.Success(new DelimitedSyntaxList<T>(openToken, items, separators, closeToken));
    }

    /// <summary>
    /// Scans tokens into a <see cref="RawSyntaxText"/> node, respecting bracket nesting.
    /// </summary>
    /// <param name="delimiters">Token kinds that stop the scan at depth zero.</param>
    /// <param name="keywords">Identifier spellings that stop the scan at depth zero.</param>
    /// <param name="stopAtOperationBoundary">
    /// When <see langword="true"/>, a token whose leading trivia contains a newline also stops the scan.
    /// </param>
    /// <param name="allowEmpty">
    /// When <see langword="true"/>, an immediate stop returns an empty <see cref="RawSyntaxText"/>;
    /// otherwise the scan fails with a diagnostic.
    /// </param>
    /// <param name="eofMessage">
    /// Error message emitted when EOF is reached before any stopping condition, or
    /// <see langword="null"/> to silently stop at EOF.
    /// </param>
    private ParseResult<RawSyntaxText> TryScanRawFragment(
        TokenKind[] delimiters,
        string[] keywords,
        bool stopAtOperationBoundary,
        bool allowEmpty,
        string? eofMessage)
    {
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        while (true)
        {
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0)
            {
                if (IsAnyDelimiter(delimiters, Current.TokenKind))
                {
                    break;
                }

                if (Current.TokenKind == TokenKind.Identifier && IsAnyKeyword(keywords, Current.Text))
                {
                    break;
                }

                if (stopAtOperationBoundary && IsOperationBoundary(Current, false))
                {
                    break;
                }
            }

            if (Is(TokenKind.EndOfFile))
            {
                if (eofMessage != null)
                {
                    return ParseResult<RawSyntaxText>.Failure(CreateDiagnostic(eofMessage));
                }

                break;
            }

            UpdateDepth(Current.TokenKind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        if (position == firstTokenIndex)
        {
            return allowEmpty
                ? ParseResult<RawSyntaxText>.Success(new RawSyntaxText(new List<Token>(), string.Empty))
                : ParseResult<RawSyntaxText>.Failure(CreateDiagnostic("Expected raw syntax."));
        }

        var firstToken = tokens[firstTokenIndex];
        var lastToken = tokens[position - 1];
        var end = lastToken.TokenStart + lastToken.TokenLength;

        return ParseResult<RawSyntaxText>.Success(new RawSyntaxText(
            CreateSyntaxTokenList(tokens, firstTokenIndex, position),
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart)));
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="kind"/> appears in the <paramref name="delimiters"/> array.</summary>
    private static bool IsAnyDelimiter(TokenKind[] delimiters, TokenKind kind)
    {
        for (var i = 0; i < delimiters.Length; i++)
        {
            if (delimiters[i] == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="text"/> exactly matches any entry in <paramref name="keywords"/>.</summary>
    private static bool IsAnyKeyword(string[] keywords, string text)
    {
        for (var i = 0; i < keywords.Length; i++)
        {
            if (string.Equals(keywords[i], text, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns an empty <see cref="DelimitedSyntaxList{T}"/> with no open/close tokens.
    /// Used for optional constructs that were absent in the source, such as an empty
    /// successor list or attribute dictionary.
    /// </summary>
    private static DelimitedSyntaxList<T> EmptyDelimitedSyntaxList<T>()
        where T : IHasSourceLocation
    {
        return new DelimitedSyntaxList<T>(null, new List<T>(), new List<Token>(), null);
    }

    /// <summary>
    /// Reads a comma-separated sequence of items, appending each to <paramref name="items"/>
    /// and each consumed comma separator to <paramref name="separators"/>.
    /// Fails immediately if <paramref name="parseElement"/> returns a failure for any item.
    /// </summary>
    private ParseResult<bool> TryParseCommaSeparatedItems<T>(
        List<T> items,
        List<Token> separators,
        Func<ParseResult<T>> parseElement)
    {
        var firstItem = parseElement();
        if (!firstItem.IsSuccess)
        {
            return ParseResult<bool>.Failure(firstItem.Diagnostic!);
        }

        items.Add(firstItem.Value);
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            separators.Add(comma);
            var item = parseElement();
            if (!item.IsSuccess)
            {
                return ParseResult<bool>.Failure(item.Diagnostic!);
            }

            items.Add(item.Value);
        }

        return ParseResult<bool>.Success(true);
    }

    /// <summary>
    /// Returns the current token and advances the position by one.
    /// Every token consumption in the parser ultimately calls this method.
    /// </summary>
    private Token ConsumeToken()
    {
        var token = Current;
        position++;
        return token;
    }

    /// <summary>
    /// Lexes <paramref name="source"/> and, on success, wraps the resulting token list in a
    /// new <see cref="Parser"/> instance ready for parsing. Returns a failure result when lexing fails.
    /// </summary>
    private static ParseResult<Parser> TryCreateParser(string source, DialectRegistry? dialectRegistry)
    {
        source ??= string.Empty;
        var document = new StringDocument(string.Empty, source);
        var lexResult = Lexer.TryLex(source, document);
        if (!lexResult.IsSuccess)
        {
            return ParseResult<Parser>.Failure(lexResult.Diagnostic!);
        }

        return ParseResult<Parser>.Success(new Parser(source, lexResult.Value, dialectRegistry));
    }

    /// <summary>
    /// Creates a <see cref="Diagnostic"/> pointing at the current token position.
    /// </summary>
    internal Diagnostic CreateDiagnostic(string message)
    {
        return new Diagnostic(message, Current.Location);
    }

    /// <summary>Bridges <see cref="Is"/> for use by <see cref="ParsingContext"/>.</summary>
    internal bool IsToken(TokenKind kind)
    {
        return Is(kind);
    }

    /// <summary>Bridges <see cref="TryMatch"/> for use by <see cref="ParsingContext"/>.</summary>
    internal bool TryMatchToken(TokenKind kind, out Token token)
    {
        return TryMatch(kind, out token);
    }

    /// <summary>
    /// Peeks ahead from the current parser position without consuming any tokens.
    /// Returns <see langword="true"/> when the requested token exists.
    /// </summary>
    internal bool TryPeekToken(int offset, out Token token)
    {
        var peekIndex = position + offset;
        if (peekIndex < 0 || peekIndex >= tokens.Count)
        {
            token = default;
            return false;
        }

        token = tokens[peekIndex];
        return true;
    }

    /// <summary>
    /// Parses a comma-separated list of SSA value tokens, consuming as many as are present.
    /// Returns a successful result with an empty list when the current token is not an SSA name.
    /// Stops as soon as a non-SSA, non-comma token is encountered.
    /// Returns a failed result with a diagnostic if an SSA token that was expected to parse fails.
    /// </summary>
    internal ParseResult<SeparatedSyntaxList<Token>> TryParseSsaTokenList()
    {
        var items = new List<Token>();
        var separators = new List<Token>();
        while (Is(TokenKind.SsaName))
        {
            var tokenResult = TryParseSsaToken();
            if (!tokenResult.IsSuccess)
            {
                return ParseResult<SeparatedSyntaxList<Token>>.Failure(tokenResult.Diagnostic!);
            }

            items.Add(tokenResult.Value);
            if (!TryMatch(TokenKind.Comma, out var comma))
            {
                break;
            }

            separators.Add(comma);
        }

        return ParseResult<SeparatedSyntaxList<Token>>.Success(new SeparatedSyntaxList<Token>(items, separators));
    }

    /// <summary>
    /// Parses an optional keyword-prefixed attribute dictionary of the form
    /// <c>attributes { name = value, ... }</c>. Returns an empty list when the
    /// <c>attributes</c> keyword is absent.
    /// </summary>
    internal ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictWithKeywordInternal()
    {
        if (!IsKeyword("attributes"))
        {
            return ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>>.Success(EmptyDelimitedSyntaxList<NamedAttributeSyntax>());
        }

        ConsumeToken();
        return TryParseAttrDict();
    }

    /// <summary>
    /// Expects an identifier token whose text exactly matches <paramref name="spelling"/>.
    /// Returns a failure diagnostic with <paramref name="message"/> when the current token
    /// does not match.
    /// </summary>
    internal ParseResult<Token> ExpectKeyword(string spelling, string message)
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, spelling, System.StringComparison.Ordinal))
        {
            return ParseResult<Token>.Failure(CreateDiagnostic(message));
        }

        return ParseResult<Token>.Success(ConsumeToken());
    }

    /// <summary>
    /// Returns <see langword="true"/> when the current token is an identifier whose text
    /// exactly matches <paramref name="spelling"/>.
    /// </summary>
    internal bool IsKeyword(string spelling)
    {
        return Is(TokenKind.Identifier) && string.Equals(Current.Text, spelling, StringComparison.Ordinal);
    }

    /// <summary>
    /// Peeks ahead to determine whether the current position looks like an attribute definition
    /// of the form <c>#name</c> and returns the name if so, or <see langword="null"/> otherwise.
    /// This is used to look up self-identifying attribute formats in the dialect registry without
    /// committing any tokens.
    /// </summary>
    private string? TryPeekAttributeDefinitionName()
    {
        if (!Is(TokenKind.Hash))
        {
            return null;
        }

        var lookahead = position + 1;
        return lookahead < tokens.Count && tokens[lookahead].TokenKind == TokenKind.Identifier
            ? tokens[lookahead].Text
            : null;
    }

    /// <summary>
    /// Peeks ahead to determine the likely type definition name at the current position so the
    /// dialect registry can be queried before any tokens are consumed. Returns the identifier text
    /// for a bare identifier or <c>!identifier</c> form, or <see langword="null"/> when no name can
    /// be determined.
    /// </summary>
    private string? TryPeekTypeDefinitionName()
    {
        if (Is(TokenKind.Identifier))
        {
            return Current.Text;
        }

        if (Is(TokenKind.Bang))
        {
            var lookahead = position + 1;
            return lookahead < tokens.Count && tokens[lookahead].TokenKind == TokenKind.Identifier
                ? tokens[lookahead].Text
                : null;
        }

        return null;
    }

    /// <summary>
    /// Strips surrounding double-quotes from a quoted operation name such as <c>"arith.addi"</c>,
    /// returning the bare name <c>arith.addi</c>. Bare names are returned unchanged.
    /// </summary>
    private static string NormalizeOperationName(string name)
    {
        return name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"' ? name.Substring(1, name.Length - 2) : name;
    }

    /// <summary>Gets the token at the current read position, which is always valid (the last token is always EOF).</summary>
    private Token Current => tokens[position];
}
