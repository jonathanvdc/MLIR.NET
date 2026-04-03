namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Syntax;
using MLIR.Dialects;

public sealed partial class Parser
{
    private ParseResult<RawSyntaxText> TryParseRawUntilOperationBoundaryResult()
    {
        return TryScanRawFragment([], [], stopAtOperationBoundary: true, allowEmpty: false, eofMessage: null);
    }

    private ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrBoundaryResult(params TokenKind[] delimiters)
    {
        return TryScanRawFragment(delimiters, [], stopAtOperationBoundary: true, allowEmpty: true, eofMessage: null);
    }

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

    private ParseResult<bool> EnsureOperationBoundaryResult(bool allowBlockStart)
    {
        return IsOperationBoundary(Current, allowBlockStart)
            ? ParseResult<bool>.Success(true)
            : ParseResult<bool>.Failure(CreateDiagnostic("Expected the end of the operation."));
    }

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

    private static List<SyntaxToken> CreateSyntaxTokenList(IReadOnlyList<Token> tokens, int start, int end)
    {
        var result = new List<SyntaxToken>(end - start);
        for (var i = start; i < end; i++)
        {
            result.Add(ToSyntaxToken(tokens[i]));
        }

        return result;
    }

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

    private ParseResult<SyntaxToken> ExpectTokenResult(TokenKind kind, string message)
    {
        var rawTokenResult = ExpectRawTokenResult(kind, message);
        return rawTokenResult.IsSuccess
            ? ParseResult<SyntaxToken>.Success(ToSyntaxToken(rawTokenResult.Value))
            : ParseResult<SyntaxToken>.Failure(rawTokenResult.Diagnostic!);
    }

    private ParseResult<Token> ExpectRawTokenResult(TokenKind kind, string message)
    {
        if (!TryMatch(kind, out var token))
        {
            return ParseResult<Token>.Failure(CreateDiagnostic(message));
        }

        return ParseResult<Token>.Success(token);
    }

    private bool Is(TokenKind kind)
    {
        return Current.Kind == kind;
    }

    private ParseMark Mark()
    {
        return new ParseMark(position);
    }

    private void Reset(ParseMark mark)
    {
        position = mark.Position;
    }

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

    private static DelimitedSyntaxList<T> EmptyDelimitedSyntaxList<T>()
    {
        return new DelimitedSyntaxList<T>(null, new List<T>(), new List<SyntaxToken>(), null);
    }

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

    private Token ConsumeToken()
    {
        var token = Current;
        position++;
        return token;
    }

    private static ParseResult<Parser> TryCreateParser(string source, DialectRegistry? dialectRegistry)
    {
        var lexResult = Lexer.TryLexCore(source);
        if (!lexResult.IsSuccess)
        {
            return ParseResult<Parser>.Failure(lexResult.Diagnostic!);
        }

        return ParseResult<Parser>.Success(new Parser(source, lexResult.Value, dialectRegistry));
    }

    private Diagnostic CreateDiagnostic(string message)
    {
        return new Diagnostic(message, Current.Line, Current.Column);
    }

    internal static SyntaxToken ToSyntaxToken(Token token)
    {
        return new SyntaxToken(token.Text, token.LeadingTrivia, token.Line, token.Column);
    }

    internal bool IsToken(TokenKind kind)
    {
        return Is(kind);
    }

    internal bool TryMatchToken(TokenKind kind, out Token token)
    {
        return TryMatch(kind, out token);
    }

    internal ParseResult<SyntaxToken> ExpectTokenInternal(TokenKind kind, string message)
    {
        return ExpectTokenResult(kind, message);
    }

    internal ParseResult<SyntaxToken> TryParseSsaTokenInternal()
    {
        return TryParseSsaTokenResult();
    }

    internal ParseResult<SyntaxToken> TryParseBlockLabelTokenInternal()
    {
        return TryParseBlockLabelTokenResult();
    }

    internal ParseResult<RegionSyntax> TryParseRegionInternal()
    {
        return TryParseRegionResult();
    }

    internal ParseResult<NamedAttributeSyntax> TryParseAttributeInternal()
    {
        return TryParseAttributeResult();
    }

    internal ParseResult<RawSyntaxText> TryParseRawUntilDelimiterInternal(params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterResult(delimiters);
    }

    internal ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrKeywordInternal(string[] keywords, params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterOrKeywordResult(delimiters, keywords);
    }

    internal ParseResult<RawSyntaxText> TryParseRawUntilOperationBoundaryInternal()
    {
        return TryParseRawUntilOperationBoundaryResult();
    }

    internal ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterOrBoundaryResult(delimiters);
    }

    internal ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictInternal()
    {
        return TryParseAttrDictResult();
    }

    internal ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictWithKeywordInternal()
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, "attributes", System.StringComparison.Ordinal))
        {
            return ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>>.Success(EmptyDelimitedSyntaxList<NamedAttributeSyntax>());
        }

        ConsumeToken();
        return TryParseAttrDictResult();
    }

    internal ParseResult<SyntaxToken> ExpectKeywordInternal(string spelling, string message)
    {
        return ExpectKeywordResult(spelling, message);
    }

    private ParseResult<SyntaxToken> ExpectKeywordResult(string spelling, string message)
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, spelling, System.StringComparison.Ordinal))
        {
            return ParseResult<SyntaxToken>.Failure(CreateDiagnostic(message));
        }

        return ParseResult<SyntaxToken>.Success(ToSyntaxToken(ConsumeToken()));
    }

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

    internal ParseResult<DelimitedSyntaxList<SyntaxToken>> TryParseSuccessorsInternal() => TryParseSuccessorsResult();

    internal ParseResult<DelimitedSyntaxList<SyntaxToken>> TryParseOperandsInternal() => TryParseOperandsResult();

    internal bool IsKeywordInternal(string spelling)
    {
        return Is(TokenKind.Identifier) && string.Equals(Current.Text, spelling, System.StringComparison.Ordinal);
    }

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

    private static string NormalizeOperationName(string name)
    {
        return name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"' ? name.Substring(1, name.Length - 2) : name;
    }

    private Token Current => tokens[position];
}
