namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Provides static factory methods and properties for creating synthetic <see cref="Token"/>
/// instances with the correct <see cref="TokenKind"/> pre-set for each token category.
/// </summary>
/// <remarks>
/// <para>
/// Use this factory instead of calling <c>new Token(...)</c> directly when constructing
/// synthetic tokens (tokens that are not backed by a source document). The factory encodes the
/// correct <see cref="TokenKind"/> for each token kind so callers do not need to repeat that
/// knowledge at every call site.
/// </para>
/// <para>
/// For fixed-text punctuation tokens (e.g. <c>(</c>, <c>)</c>, <c>{</c>) there is one static
/// method per token kind that accepts an optional leading-trivia string.
/// For variable-text tokens (identifiers, SSA names, literals) there is one static method per
/// token kind that requires the text as its first argument.
/// </para>
/// </remarks>
public static class TokenFactory
{
    // -----------------------------------------------------------------------
    // Fixed-text punctuation tokens
    // -----------------------------------------------------------------------

    /// <summary>Creates a synthetic <c>(</c> token.</summary>
    public static Token LParen(string? leadingTrivia = null) =>
        new Token(TokenKind.LParen, "(", leadingTrivia);

    /// <summary>Creates a synthetic <c>)</c> token.</summary>
    public static Token RParen(string? leadingTrivia = null) =>
        new Token(TokenKind.RParen, ")", leadingTrivia);

    /// <summary>Creates a synthetic <c>{</c> token.</summary>
    public static Token LBrace(string? leadingTrivia = null) =>
        new Token(TokenKind.LBrace, "{", leadingTrivia);

    /// <summary>Creates a synthetic <c>}</c> token.</summary>
    public static Token RBrace(string? leadingTrivia = null) =>
        new Token(TokenKind.RBrace, "}", leadingTrivia);

    /// <summary>Creates a synthetic <c>[</c> token.</summary>
    public static Token LBracket(string? leadingTrivia = null) =>
        new Token(TokenKind.LBracket, "[", leadingTrivia);

    /// <summary>Creates a synthetic <c>]</c> token.</summary>
    public static Token RBracket(string? leadingTrivia = null) =>
        new Token(TokenKind.RBracket, "]", leadingTrivia);

    /// <summary>Creates a synthetic <c>&lt;</c> token.</summary>
    public static Token LessThan(string? leadingTrivia = null) =>
        new Token(TokenKind.LessThan, "<", leadingTrivia);

    /// <summary>Creates a synthetic <c>&gt;</c> token.</summary>
    public static Token GreaterThan(string? leadingTrivia = null) =>
        new Token(TokenKind.GreaterThan, ">", leadingTrivia);

    /// <summary>Creates a synthetic <c>:</c> token.</summary>
    public static Token Colon(string? leadingTrivia = null) =>
        new Token(TokenKind.Colon, ":", leadingTrivia);

    /// <summary>Creates a synthetic <c>,</c> token.</summary>
    public static Token Comma(string? leadingTrivia = null) =>
        new Token(TokenKind.Comma, ",", leadingTrivia);

    /// <summary>Creates a synthetic <c>=</c> token.</summary>
    public static Token Equal(string? leadingTrivia = null) =>
        new Token(TokenKind.Equal, "=", leadingTrivia);

    /// <summary>Creates a synthetic <c>-&gt;</c> token.</summary>
    public static Token Arrow(string? leadingTrivia = null) =>
        new Token(TokenKind.Arrow, "->", leadingTrivia);

    /// <summary>Creates a synthetic <c>@</c> token.</summary>
    public static Token At(string? leadingTrivia = null) =>
        new Token(TokenKind.At, "@", leadingTrivia);

    /// <summary>Creates a synthetic <c>#</c> token.</summary>
    public static Token Hash(string? leadingTrivia = null) =>
        new Token(TokenKind.Hash, "#", leadingTrivia);

    /// <summary>Creates a synthetic <c>!</c> token.</summary>
    public static Token Bang(string? leadingTrivia = null) =>
        new Token(TokenKind.Bang, "!", leadingTrivia);

    /// <summary>Creates a synthetic <c>?</c> token.</summary>
    public static Token Question(string? leadingTrivia = null) =>
        new Token(TokenKind.Question, "?", leadingTrivia);

    /// <summary>Creates a synthetic <c>*</c> token.</summary>
    public static Token Star(string? leadingTrivia = null) =>
        new Token(TokenKind.Star, "*", leadingTrivia);

    /// <summary>Creates a synthetic <c>+</c> token.</summary>
    public static Token Plus(string? leadingTrivia = null) =>
        new Token(TokenKind.Plus, "+", leadingTrivia);

    /// <summary>Creates a synthetic <c>-</c> token.</summary>
    public static Token Minus(string? leadingTrivia = null) =>
        new Token(TokenKind.Minus, "-", leadingTrivia);

    /// <summary>Creates a synthetic <c>.</c> token.</summary>
    public static Token Dot(string? leadingTrivia = null) =>
        new Token(TokenKind.Dot, ".", leadingTrivia);

    /// <summary>Creates a synthetic <c>|</c> token.</summary>
    public static Token Pipe(string? leadingTrivia = null) =>
        new Token(TokenKind.Pipe, "|", leadingTrivia);

    // -----------------------------------------------------------------------
    // End-of-file sentinel
    // -----------------------------------------------------------------------

    /// <summary>Creates a synthetic end-of-file token.</summary>
    public static Token EndOfFile(string? leadingTrivia = null) =>
        new Token(TokenKind.EndOfFile, string.Empty, leadingTrivia);

    // -----------------------------------------------------------------------
    // Variable-text tokens
    // -----------------------------------------------------------------------

    /// <summary>Creates a synthetic identifier token with the supplied <paramref name="text"/>.</summary>
    public static Token Identifier(string text, string? leadingTrivia = null) =>
        new Token(TokenKind.Identifier, text, leadingTrivia);

    /// <summary>Creates a synthetic integer literal token with the supplied <paramref name="text"/>.</summary>
    public static Token Integer(string text, string? leadingTrivia = null) =>
        new Token(TokenKind.Integer, text, leadingTrivia);

    /// <summary>
    /// Creates a synthetic string literal token with the supplied <paramref name="text"/>.
    /// The <paramref name="text"/> should include the surrounding double-quote characters,
    /// as it is stored verbatim.
    /// </summary>
    public static Token StringLiteral(string text, string? leadingTrivia = null) =>
        new Token(TokenKind.StringLiteral, text, leadingTrivia);

    /// <summary>
    /// Creates a synthetic SSA name token with the supplied <paramref name="text"/>
    /// (e.g. <c>%0</c> or <c>%arg0</c>).
    /// </summary>
    public static Token SsaName(string text, string? leadingTrivia = null) =>
        new Token(TokenKind.SsaName, text, leadingTrivia);

    /// <summary>
    /// Creates a synthetic block label token with the supplied <paramref name="text"/>
    /// (e.g. <c>^bb0</c> or <c>^entry</c>).
    /// </summary>
    public static Token BlockLabel(string text, string? leadingTrivia = null) =>
        new Token(TokenKind.BlockLabel, text, leadingTrivia);
}
