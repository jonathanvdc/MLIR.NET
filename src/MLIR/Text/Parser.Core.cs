namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Syntax;
using MLIR.Dialects;

public sealed partial class Parser
{
    /// <summary>
    /// Scans raw tokens until the end of the current operation, stopping at a newline
    /// boundary or the end of file. The scan is bracket-aware and never returns an empty result.
    /// Used to capture the type signature of an operation in one raw chunk.
    /// </summary>
    private ParseResult<RawSyntaxText> TryParseRawUntilOperationBoundaryResult()
    {
        return TryScanRawFragment([], [], stopAtOperationBoundary: true, allowEmpty: false, eofMessage: null);
    }

    /// <summary>
    /// Scans raw tokens until any of the supplied delimiters is reached at depth zero,
    /// or an operation boundary is encountered, whichever comes first.
    /// An empty scan is allowed (returns an empty <see cref="RawSyntaxText"/>).
    /// </summary>
    private ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrBoundaryResult(params TokenKind[] delimiters)
    {
        return TryScanRawFragment(delimiters, [], stopAtOperationBoundary: true, allowEmpty: true, eofMessage: null);
    }

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
        if (tokens[lookahead].Kind == TokenKind.RBrace)
        {
            return false;
        }

        if (tokens[lookahead].Kind == TokenKind.BlockLabel || tokens[lookahead].Kind == TokenKind.StringLiteral || tokens[lookahead].Kind == TokenKind.SsaName)
        {
            return true;
        }

        if (tokens[lookahead].Kind != TokenKind.Identifier)
        {
            return false;
        }

        var secondLookahead = tokens[lookahead + 1];
        return secondLookahead.Kind != TokenKind.Equal
            && secondLookahead.Kind != TokenKind.Colon
            && secondLookahead.Kind != TokenKind.Comma;
    }

    /// <summary>
    /// Verifies that the current token represents an operation boundary, returning a failure diagnostic
    /// when it does not. Used after every top-level or block-level operation.
    /// </summary>
    /// <param name="allowBlockStart">
    /// When <see langword="true"/>, a block label token preceded by a newline is also accepted as a boundary.
    /// </param>
    private ParseResult<bool> EnsureOperationBoundaryResult(bool allowBlockStart)
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
        if (token.Kind == TokenKind.EndOfFile || token.Kind == TokenKind.RBrace)
        {
            return true;
        }

        if (allowBlockStart && token.Kind == TokenKind.BlockLabel && token.LeadingTrivia.Contains('\n'))
        {
            return true;
        }

        return token.LeadingTrivia.Contains('\n');
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
    /// Converts a contiguous sub-range of the raw token list into <see cref="SyntaxToken"/> instances.
    /// </summary>
    /// <param name="tokens">The full token list from the lexer.</param>
    /// <param name="start">Inclusive start index in <paramref name="tokens"/>.</param>
    /// <param name="end">Exclusive end index in <paramref name="tokens"/>.</param>
    private static List<SyntaxToken> CreateSyntaxTokenList(IReadOnlyList<Token> tokens, int start, int end)
    {
        var result = new List<SyntaxToken>(end - start);
        for (var i = start; i < end; i++)
        {
            result.Add(ToSyntaxToken(tokens[i]));
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
        if (Current.Kind != kind)
        {
            token = default;
            return false;
        }

        token = ConsumeToken();
        return true;
    }

    /// <summary>
    /// Expects the current token to have the supplied <paramref name="kind"/> and converts it to a
    /// <see cref="SyntaxToken"/> on success. Returns a failure diagnostic with <paramref name="message"/>
    /// when the token does not match.
    /// </summary>
    private ParseResult<SyntaxToken> ExpectTokenResult(TokenKind kind, string message)
    {
        var rawTokenResult = ExpectRawTokenResult(kind, message);
        return rawTokenResult.IsSuccess
            ? ParseResult<SyntaxToken>.Success(ToSyntaxToken(rawTokenResult.Value))
            : ParseResult<SyntaxToken>.Failure(rawTokenResult.Diagnostic!);
    }

    /// <summary>
    /// Expects the current token to have the supplied <paramref name="kind"/> and returns it as a raw
    /// <see cref="Token"/> on success. Returns a failure diagnostic with <paramref name="message"/>
    /// when the token does not match.
    /// </summary>
    private ParseResult<Token> ExpectRawTokenResult(TokenKind kind, string message)
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
        return Current.Kind == kind;
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
    {
        if (!TryMatch(openKind, out var openToken))
        {
            return ParseResult<DelimitedSyntaxList<T>>.Success(EmptyDelimitedSyntaxList<T>());
        }

        return TryParseCommaSeparatedDelimitedListCore(ToSyntaxToken(openToken), closeKind, parseElement, closeMessage);
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
    {
        return ExpectTokenResult(openKind, openMessage)
            .Bind(openToken => TryParseCommaSeparatedDelimitedListCore(openToken, closeKind, parseElement, closeMessage));
    }

    /// <summary>
    /// Core loop for parsing a comma-separated list after the opening token has already been consumed.
    /// Reads items separated by commas until the closing token <paramref name="closeKind"/> is found,
    /// then wraps the result in a <see cref="DelimitedSyntaxList{T}"/>.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<T>> TryParseCommaSeparatedDelimitedListCore<T>(
        SyntaxToken openToken,
        TokenKind closeKind,
        Func<ParseResult<T>> parseElement,
        string closeMessage)
    {
        var items = new List<T>();
        var separators = new List<SyntaxToken>();
        if (!TryMatch(closeKind, out var closeToken))
        {
            var itemsResult = TryParseCommaSeparatedItems(items, separators, parseElement);
            if (!itemsResult.IsSuccess)
            {
                return ParseResult<DelimitedSyntaxList<T>>.Failure(itemsResult.Diagnostic!);
            }

            var closeTokenResult = ExpectRawTokenResult(closeKind, closeMessage);
            if (!closeTokenResult.TryGetValue(out closeToken))
            {
                return ParseResult<DelimitedSyntaxList<T>>.Failure(closeTokenResult.Diagnostic!);
            }
        }

        return ParseResult<DelimitedSyntaxList<T>>.Success(new DelimitedSyntaxList<T>(openToken, items, separators, ToSyntaxToken(closeToken)));
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
                if (IsAnyDelimiter(delimiters, Current.Kind))
                {
                    break;
                }

                if (Current.Kind == TokenKind.Identifier && IsAnyKeyword(keywords, Current.Text))
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

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        if (position == firstTokenIndex)
        {
            return allowEmpty
                ? ParseResult<RawSyntaxText>.Success(new RawSyntaxText(new List<SyntaxToken>(), string.Empty))
                : ParseResult<RawSyntaxText>.Failure(CreateDiagnostic("Expected raw syntax."));
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;

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
    {
        return new DelimitedSyntaxList<T>(null, new List<T>(), new List<SyntaxToken>(), null);
    }

    /// <summary>
    /// Reads a comma-separated sequence of items, appending each to <paramref name="items"/>
    /// and each consumed comma separator to <paramref name="separators"/>.
    /// Fails immediately if <paramref name="parseElement"/> returns a failure for any item.
    /// </summary>
    private ParseResult<bool> TryParseCommaSeparatedItems<T>(
        List<T> items,
        List<SyntaxToken> separators,
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
            separators.Add(ToSyntaxToken(comma));
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
        var lexResult = Lexer.TryLexCore(source);
        if (!lexResult.IsSuccess)
        {
            return ParseResult<Parser>.Failure(lexResult.Diagnostic!);
        }

        return ParseResult<Parser>.Success(new Parser(source, lexResult.Value, dialectRegistry));
    }

    /// <summary>
    /// Creates a <see cref="Diagnostic"/> pointing at the current token position.
    /// </summary>
    private Diagnostic CreateDiagnostic(string message)
    {
        return new Diagnostic(message, Current.Line, Current.Column);
    }

    /// <summary>
    /// Converts an internal <see cref="Token"/> (produced by the lexer) to a public
    /// <see cref="SyntaxToken"/> (part of the CST) by copying its text, leading trivia, and source location.
    /// </summary>
    internal static SyntaxToken ToSyntaxToken(Token token)
    {
        return new SyntaxToken(token.Text, token.LeadingTrivia, token.Line, token.Column);
    }

    /// <summary>Bridges <see cref="Is"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal bool IsToken(TokenKind kind)
    {
        return Is(kind);
    }

    /// <summary>Bridges <see cref="TryMatch"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal bool TryMatchToken(TokenKind kind, out Token token)
    {
        return TryMatch(kind, out token);
    }

    /// <summary>Bridges <see cref="ExpectTokenResult"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<SyntaxToken> ExpectTokenInternal(TokenKind kind, string message)
    {
        return ExpectTokenResult(kind, message);
    }

    /// <summary>Bridges <see cref="TryParseSsaTokenResult"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal ParseResult<SyntaxToken> TryParseSsaTokenInternal()
    {
        return TryParseSsaTokenResult();
    }

    /// <summary>
    /// Parses a comma-separated list of SSA value tokens, consuming as many as are present.
    /// Returns a successful result with an empty list when the current token is not an SSA name.
    /// Stops as soon as a non-SSA, non-comma token is encountered.
    /// Returns a failed result with a diagnostic if an SSA token that was expected to parse fails.
    /// </summary>
    internal ParseResult<SeparatedSyntaxList<SyntaxToken>> TryParseSsaTokenListInternal()
    {
        var items = new List<SyntaxToken>();
        var separators = new List<SyntaxToken>();
        while (Is(TokenKind.SsaName))
        {
            var tokenResult = TryParseSsaTokenResult();
            if (!tokenResult.IsSuccess)
            {
                return ParseResult<SeparatedSyntaxList<SyntaxToken>>.Failure(tokenResult.Diagnostic!);
            }

            items.Add(tokenResult.Value);
            if (!TryMatch(TokenKind.Comma, out var comma))
            {
                break;
            }

            separators.Add(ToSyntaxToken(comma));
        }

        return ParseResult<SeparatedSyntaxList<SyntaxToken>>.Success(new SeparatedSyntaxList<SyntaxToken>(items, separators));
    }

    /// <summary>Bridges <see cref="TryParseBlockLabelTokenResult"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal ParseResult<SyntaxToken> TryParseBlockLabelTokenInternal()
    {
        return TryParseBlockLabelTokenResult();
    }

    /// <summary>Bridges <see cref="TryParseRegionResult"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal ParseResult<RegionSyntax> TryParseRegionInternal()
    {
        return TryParseRegionResult();
    }

    /// <summary>Bridges <see cref="TryParseAttributeResult"/> for use by dialect parsing contexts.</summary>
    internal ParseResult<NamedAttributeSyntax> TryParseAttributeInternal()
    {
        return TryParseAttributeResult();
    }

    /// <summary>Bridges <see cref="TryParseRawUntilDelimiterResult"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<RawSyntaxText> TryParseRawUntilDelimiterInternal(params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterResult(delimiters);
    }

    /// <summary>Bridges <see cref="TryParseRawUntilDelimiterOrKeywordResult"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrKeywordInternal(string[] keywords, params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterOrKeywordResult(delimiters, keywords);
    }

    /// <summary>Bridges <see cref="TryParseRawUntilOperationBoundaryResult"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<RawSyntaxText> TryParseRawUntilOperationBoundaryInternal()
    {
        return TryParseRawUntilOperationBoundaryResult();
    }

    /// <summary>Bridges <see cref="TryParseRawUntilDelimiterOrBoundaryResult"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterOrBoundaryResult(delimiters);
    }

    /// <summary>Bridges <see cref="TryParseAttrDictResult"/> for use by dialect parsing contexts.</summary>
    internal ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictInternal()
    {
        return TryParseAttrDictResult();
    }

    /// <summary>
    /// Parses an optional keyword-prefixed attribute dictionary of the form
    /// <c>attributes { name = value, ... }</c>. Returns an empty list when the
    /// <c>attributes</c> keyword is absent.
    /// </summary>
    internal ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictWithKeywordInternal()
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, "attributes", System.StringComparison.Ordinal))
        {
            return ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>>.Success(EmptyDelimitedSyntaxList<NamedAttributeSyntax>());
        }

        ConsumeToken();
        return TryParseAttrDictResult();
    }

    /// <summary>Bridges <see cref="ExpectKeywordResult"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal ParseResult<SyntaxToken> ExpectKeywordInternal(string spelling, string message)
    {
        return ExpectKeywordResult(spelling, message);
    }

    /// <summary>
    /// Expects an identifier token whose text exactly matches <paramref name="spelling"/>.
    /// Returns a failure diagnostic with <paramref name="message"/> when the current token
    /// does not match.
    /// </summary>
    private ParseResult<SyntaxToken> ExpectKeywordResult(string spelling, string message)
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, spelling, System.StringComparison.Ordinal))
        {
            return ParseResult<SyntaxToken>.Failure(CreateDiagnostic(message));
        }

        return ParseResult<SyntaxToken>.Success(ToSyntaxToken(ConsumeToken()));
    }

    /// <summary>
    /// Parses zero or more consecutive regions, each delimited by <c>{ ... }</c>.
    /// Stops as soon as the next token is not <c>{</c>.
    /// </summary>
    internal ParseResult<IReadOnlyList<RegionSyntax>> TryParseRegionsInternal()
    {
        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace))
        {
            var regionResult = TryParseRegionResult();
            if (!regionResult.IsSuccess)
            {
                return ParseResult<IReadOnlyList<RegionSyntax>>.Failure(regionResult.Diagnostic!);
            }

            regions.Add(regionResult.Value);
        }

        return ParseResult<IReadOnlyList<RegionSyntax>>.Success(regions);
    }

    /// <summary>Bridges <see cref="TryParseSuccessorsResult"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal ParseResult<DelimitedSyntaxList<SyntaxToken>> TryParseSuccessorsInternal() => TryParseSuccessorsResult();

    /// <summary>Bridges <see cref="TryParseOperandsResult"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal ParseResult<DelimitedSyntaxList<SyntaxToken>> TryParseOperandsInternal() => TryParseOperandsResult();

    /// <summary>
    /// Returns <see langword="true"/> when the current token is an identifier whose text
    /// exactly matches <paramref name="spelling"/>.
    /// </summary>
    internal bool IsKeywordInternal(string spelling)
    {
        return Is(TokenKind.Identifier) && string.Equals(Current.Text, spelling, System.StringComparison.Ordinal);
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
        return lookahead < tokens.Count && tokens[lookahead].Kind == TokenKind.Identifier
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
            return lookahead < tokens.Count && tokens[lookahead].Kind == TokenKind.Identifier
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
