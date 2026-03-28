namespace MLIR.Text;

using System.Collections.Generic;
using System.Linq;
using MLIR.Syntax;

/// <summary>
/// Parses generic MLIR syntax into a concrete syntax tree.
/// </summary>
public sealed class MlirParser
{
    private readonly string source;
    private readonly IReadOnlyList<MlirToken> tokens;
    private int position;

    private MlirParser(string source)
    {
        this.source = source;
        tokens = MlirLexer.Lex(source);
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source)
    {
        return new MlirParser(source).ParseModuleCore();
    }

    private ModuleSyntax ParseModuleCore()
    {
        var operations = new List<OperationSyntax>();
        while (!Is(MlirTokenKind.EndOfFile))
        {
            operations.Add(ParseOperation());
            EnsureOperationBoundary(false);
        }

        return new ModuleSyntax(operations, ToSyntaxToken(ConsumeToken()));
    }

    private OperationSyntax ParseOperation()
    {
        var resultTokens = new List<SyntaxToken>();
        var resultCommaTokens = new List<SyntaxToken>();
        SyntaxToken? equalsToken = null;

        if (Is(MlirTokenKind.SsaName))
        {
            resultTokens.Add(ParseSsaToken());
            while (TryMatch(MlirTokenKind.Comma, out var resultCommaToken))
            {
                resultCommaTokens.Add(ToSyntaxToken(resultCommaToken));
                resultTokens.Add(ParseSsaToken());
            }

            equalsToken = ExpectToken(MlirTokenKind.Equal, "Expected '=' after operation result list.");
        }

        var nameToken = ParseOperationNameToken();
        var openParenthesisToken = ExpectToken(MlirTokenKind.LParen, "Expected '(' to start the operand list.");
        var operandTokens = new List<SyntaxToken>();
        var operandCommaTokens = new List<SyntaxToken>();

        if (!TryMatch(MlirTokenKind.RParen, out var closeParenthesisTokenValue))
        {
            operandTokens.Add(ParseSsaToken());
            while (TryMatch(MlirTokenKind.Comma, out var operandCommaToken))
            {
                operandCommaTokens.Add(ToSyntaxToken(operandCommaToken));
                operandTokens.Add(ParseSsaToken());
            }

            closeParenthesisTokenValue = ExpectRawToken(MlirTokenKind.RParen, "Expected ')' to close the operand list.");
        }

        var openSuccessorBracketToken = default(SyntaxToken);
        var closeSuccessorBracketToken = default(SyntaxToken);
        var hasSuccessors = false;
        var successorTokens = new List<SyntaxToken>();
        var successorCommaTokens = new List<SyntaxToken>();

        if (TryMatch(MlirTokenKind.LBracket, out var openSuccessorBracketValue))
        {
            hasSuccessors = true;
            openSuccessorBracketToken = ToSyntaxToken(openSuccessorBracketValue);

            if (!TryMatch(MlirTokenKind.RBracket, out var closeSuccessorBracketValue))
            {
                successorTokens.Add(ParseBlockLabelToken());
                while (TryMatch(MlirTokenKind.Comma, out var successorCommaToken))
                {
                    successorCommaTokens.Add(ToSyntaxToken(successorCommaToken));
                    successorTokens.Add(ParseBlockLabelToken());
                }

                closeSuccessorBracketValue = ExpectRawToken(MlirTokenKind.RBracket, "Expected ']' to close the successor list.");
            }

            closeSuccessorBracketToken = ToSyntaxToken(closeSuccessorBracketValue);
        }

        var regions = new List<RegionSyntax>();
        while (Is(MlirTokenKind.LBrace) && IsRegionStart())
        {
            regions.Add(ParseRegion());
        }

        var openAttributeBraceToken = default(SyntaxToken);
        var closeAttributeBraceToken = default(SyntaxToken);
        var hasAttributes = false;
        var attributes = new List<NamedAttributeSyntax>();
        var attributeCommaTokens = new List<SyntaxToken>();
        if (Is(MlirTokenKind.LBrace))
        {
            hasAttributes = true;
            openAttributeBraceToken = ExpectToken(MlirTokenKind.LBrace, "Expected '{' to start an attribute dictionary.");
            if (!TryMatch(MlirTokenKind.RBrace, out var closeAttributeBraceValue))
            {
                attributes.Add(ParseAttribute());
                while (TryMatch(MlirTokenKind.Comma, out var attributeCommaToken))
                {
                    attributeCommaTokens.Add(ToSyntaxToken(attributeCommaToken));
                    attributes.Add(ParseAttribute());
                }

                closeAttributeBraceValue = ExpectRawToken(MlirTokenKind.RBrace, "Expected '}' to close the attribute dictionary.");
            }

            closeAttributeBraceToken = ToSyntaxToken(closeAttributeBraceValue);
        }

        SyntaxToken? typeSignatureColonToken = null;
        RawSyntaxText? typeSignature = null;
        if (Is(MlirTokenKind.Colon))
        {
            typeSignatureColonToken = ExpectToken(MlirTokenKind.Colon, "Expected ':' before the type signature.");
            typeSignature = ParseRawUntilOperationBoundary();
        }

        return new OperationSyntax(
            resultTokens,
            resultCommaTokens,
            equalsToken,
            nameToken,
            new DelimitedSyntaxList<SyntaxToken>(
                openParenthesisToken,
                operandTokens,
                operandCommaTokens,
                ToSyntaxToken(closeParenthesisTokenValue)),
            new DelimitedSyntaxList<SyntaxToken>(
                hasSuccessors ? openSuccessorBracketToken : null,
                successorTokens,
                successorCommaTokens,
                hasSuccessors ? closeSuccessorBracketToken : null),
            regions,
            new DelimitedSyntaxList<NamedAttributeSyntax>(
                hasAttributes ? openAttributeBraceToken : null,
                attributes,
                attributeCommaTokens,
                hasAttributes ? closeAttributeBraceToken : null),
            typeSignatureColonToken,
            typeSignature);
    }

    private RegionSyntax ParseRegion()
    {
        var openBraceToken = ExpectToken(MlirTokenKind.LBrace, "Expected '{' to start a region.");
        var blocks = new List<BlockSyntax>();
        var pendingEntryOperations = new List<OperationSyntax>();

        while (!Is(MlirTokenKind.RBrace))
        {
            if (Is(MlirTokenKind.BlockLabel))
            {
                if (pendingEntryOperations.Count > 0)
                {
                    // MLIR allows unlabeled operations at the start of a region. Model them as
                    // a synthetic entry block so the CST always has a block-based shape.
                    blocks.Add(new BlockSyntax(
                        new SyntaxToken("^entry"),
                        new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<SyntaxToken>(), null),
                        new SyntaxToken(":"),
                        pendingEntryOperations.ToList()));
                    pendingEntryOperations.Clear();
                }

                blocks.Add(ParseBlock());
            }
            else
            {
                pendingEntryOperations.Add(ParseOperation());
                EnsureOperationBoundary(true);
            }
        }

        if (pendingEntryOperations.Count > 0 || blocks.Count == 0)
        {
            // Keep region bodies uniform even for empty regions and unlabeled entry operations.
            blocks.Insert(0, new BlockSyntax(
                new SyntaxToken("^entry"),
                new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<SyntaxToken>(), null),
                new SyntaxToken(":"),
                pendingEntryOperations.ToList()));
        }

        var closeBraceToken = ExpectToken(MlirTokenKind.RBrace, "Expected '}' to close a region.");
        return new RegionSyntax(openBraceToken, blocks, closeBraceToken);
    }

    private BlockSyntax ParseBlock()
    {
        var labelToken = ParseBlockLabelToken();
        SyntaxToken? openParenthesisToken = null;
        SyntaxToken? closeParenthesisToken = null;
        var arguments = new List<BlockArgumentSyntax>();
        var argumentCommaTokens = new List<SyntaxToken>();

        if (TryMatch(MlirTokenKind.LParen, out var openParenthesisTokenValue))
        {
            openParenthesisToken = ToSyntaxToken(openParenthesisTokenValue);
            if (!TryMatch(MlirTokenKind.RParen, out var closeParenthesisTokenValue))
            {
                arguments.Add(ParseBlockArgument());
                while (TryMatch(MlirTokenKind.Comma, out var argumentCommaToken))
                {
                    argumentCommaTokens.Add(ToSyntaxToken(argumentCommaToken));
                    arguments.Add(ParseBlockArgument());
                }

                closeParenthesisTokenValue = ExpectRawToken(MlirTokenKind.RParen, "Expected ')' after block argument list.");
            }

            closeParenthesisToken = ToSyntaxToken(closeParenthesisTokenValue);
        }

        var colonToken = ExpectToken(MlirTokenKind.Colon, "Expected ':' after block label.");
        var operations = new List<OperationSyntax>();
        while (!Is(MlirTokenKind.RBrace) && !Is(MlirTokenKind.BlockLabel))
        {
            operations.Add(ParseOperation());
            EnsureOperationBoundary(true);
        }

        return new BlockSyntax(
            labelToken,
            new DelimitedSyntaxList<BlockArgumentSyntax>(openParenthesisToken, arguments, argumentCommaTokens, closeParenthesisToken),
            colonToken,
            operations);
    }

    private BlockArgumentSyntax ParseBlockArgument()
    {
        var nameToken = ParseSsaToken();
        var colonToken = ExpectToken(MlirTokenKind.Colon, "Expected ':' after block argument name.");
        var type = ParseRawUntilDelimiter(MlirTokenKind.Comma, MlirTokenKind.RParen);
        return new BlockArgumentSyntax(nameToken, colonToken, type);
    }

    private NamedAttributeSyntax ParseAttribute()
    {
        SyntaxToken nameToken;
        if (Is(MlirTokenKind.Identifier) || Is(MlirTokenKind.StringLiteral))
        {
            nameToken = ToSyntaxToken(ConsumeToken());
        }
        else
        {
            throw Error("Expected an attribute name.");
        }

        var equalsToken = ExpectToken(MlirTokenKind.Equal, "Expected '=' after attribute name.");
        var value = ParseRawUntilDelimiter(MlirTokenKind.Comma, MlirTokenKind.RBrace);
        return new NamedAttributeSyntax(nameToken, equalsToken, value);
    }

    private SyntaxToken ParseOperationNameToken()
    {
        if (!Is(MlirTokenKind.Identifier) && !Is(MlirTokenKind.StringLiteral))
        {
            throw Error("Expected an operation name.");
        }

        return ToSyntaxToken(ConsumeToken());
    }

    private SyntaxToken ParseSsaToken()
    {
        return ExpectToken(MlirTokenKind.SsaName, "Expected an SSA value name.");
    }

    private SyntaxToken ParseBlockLabelToken()
    {
        return ExpectToken(MlirTokenKind.BlockLabel, "Expected a block label name.");
    }

    private RawSyntaxText ParseRawUntilDelimiter(params MlirTokenKind[] delimiters)
    {
        var start = Current.FullStart;
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        // Raw syntax fragments may themselves contain nested delimiters, so only stop when
        // we reach one of the requested delimiters at the outermost nesting level.
        while (true)
        {
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0 && delimiters.Contains(Current.Kind))
            {
                break;
            }

            if (Is(MlirTokenKind.EndOfFile))
            {
                throw Error("Unexpected end of file while parsing raw syntax.");
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;

        return new RawSyntaxText(
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart),
            source.Substring(start, firstToken.TokenStart - start));
    }

    private RawSyntaxText ParseRawUntilOperationBoundary()
    {
        var start = Current.FullStart;
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        while (!Is(MlirTokenKind.EndOfFile))
        {
            if (depthParen == 0 &&
                depthBrace == 0 &&
                depthBracket == 0 &&
                depthAngle == 0 &&
                IsOperationBoundary(Current, false))
            {
                break;
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;

        return new RawSyntaxText(
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart),
            source.Substring(start, firstToken.TokenStart - start));
    }

    private bool IsRegionStart()
    {
        if (!Is(MlirTokenKind.LBrace))
        {
            return false;
        }

        // A '{' can start either a region or an attribute dictionary. Peek ahead to decide
        // which production we are looking at without consuming any tokens.
        var lookahead = position + 1;
        if (tokens[lookahead].Kind == MlirTokenKind.RBrace)
        {
            return false;
        }

        if (tokens[lookahead].Kind == MlirTokenKind.BlockLabel || tokens[lookahead].Kind == MlirTokenKind.StringLiteral || tokens[lookahead].Kind == MlirTokenKind.SsaName)
        {
            return true;
        }

        if (tokens[lookahead].Kind != MlirTokenKind.Identifier)
        {
            return false;
        }

        var secondLookahead = tokens[lookahead + 1];
        return secondLookahead.Kind != MlirTokenKind.Equal && secondLookahead.Kind != MlirTokenKind.Comma;
    }

    private void EnsureOperationBoundary(bool allowBlockStart)
    {
        if (!IsOperationBoundary(Current, allowBlockStart))
        {
            throw Error("Expected the end of the operation.");
        }
    }

    private bool IsOperationBoundary(MlirToken token, bool allowBlockStart)
    {
        if (token.Kind == MlirTokenKind.EndOfFile || token.Kind == MlirTokenKind.RBrace)
        {
            return true;
        }

        if (allowBlockStart && token.Kind == MlirTokenKind.BlockLabel && token.LeadingTrivia.Contains('\n'))
        {
            return true;
        }

        return token.LeadingTrivia.Contains('\n');
    }

    private static void UpdateDepth(MlirTokenKind kind, ref int depthParen, ref int depthBrace, ref int depthBracket, ref int depthAngle)
    {
        switch (kind)
        {
            case MlirTokenKind.LParen:
                depthParen++;
                break;
            case MlirTokenKind.RParen:
                depthParen--;
                break;
            case MlirTokenKind.LBrace:
                depthBrace++;
                break;
            case MlirTokenKind.RBrace:
                depthBrace--;
                break;
            case MlirTokenKind.LBracket:
                depthBracket++;
                break;
            case MlirTokenKind.RBracket:
                depthBracket--;
                break;
            case MlirTokenKind.LessThan:
                depthAngle++;
                break;
            case MlirTokenKind.GreaterThan:
                depthAngle--;
                break;
        }
    }

    private bool TryMatch(MlirTokenKind kind, out MlirToken token)
    {
        if (Current.Kind != kind)
        {
            token = default;
            return false;
        }

        token = ConsumeToken();
        return true;
    }

    private SyntaxToken ExpectToken(MlirTokenKind kind, string message)
    {
        return ToSyntaxToken(ExpectRawToken(kind, message));
    }

    private MlirToken ExpectRawToken(MlirTokenKind kind, string message)
    {
        if (!TryMatch(kind, out var token))
        {
            throw Error(message);
        }

        return token;
    }

    private bool Is(MlirTokenKind kind)
    {
        return Current.Kind == kind;
    }

    private MlirToken ConsumeToken()
    {
        var token = Current;
        position++;
        return token;
    }

    private MlirParseException Error(string message)
    {
        return new MlirParseException(new MlirDiagnostic(message, Current.Line, Current.Column));
    }

    private static SyntaxToken ToSyntaxToken(MlirToken token)
    {
        return new SyntaxToken(token.Text, token.LeadingTrivia);
    }

    private MlirToken Current => tokens[position];
}
