namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Provides static factory methods and properties for creating synthetic <see cref="SyntaxToken"/>
/// instances with the correct <see cref="TokenKind"/> pre-set for each token category.
/// </summary>
/// <remarks>
/// <para>
/// Use this factory instead of calling <c>new SyntaxToken(...)</c> directly when constructing
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
public static class SyntaxTokenFactory
{
    // -----------------------------------------------------------------------
    // Fixed-text punctuation tokens
    // -----------------------------------------------------------------------

    /// <summary>Creates a synthetic <c>(</c> token.</summary>
    public static SyntaxToken LParen(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.LParen, "(", leadingTrivia);

    /// <summary>Creates a synthetic <c>)</c> token.</summary>
    public static SyntaxToken RParen(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.RParen, ")", leadingTrivia);

    /// <summary>Creates a synthetic <c>{</c> token.</summary>
    public static SyntaxToken LBrace(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.LBrace, "{", leadingTrivia);

    /// <summary>Creates a synthetic <c>}</c> token.</summary>
    public static SyntaxToken RBrace(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.RBrace, "}", leadingTrivia);

    /// <summary>Creates a synthetic <c>[</c> token.</summary>
    public static SyntaxToken LBracket(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.LBracket, "[", leadingTrivia);

    /// <summary>Creates a synthetic <c>]</c> token.</summary>
    public static SyntaxToken RBracket(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.RBracket, "]", leadingTrivia);

    /// <summary>Creates a synthetic <c>&lt;</c> token.</summary>
    public static SyntaxToken LessThan(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.LessThan, "<", leadingTrivia);

    /// <summary>Creates a synthetic <c>&gt;</c> token.</summary>
    public static SyntaxToken GreaterThan(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.GreaterThan, ">", leadingTrivia);

    /// <summary>Creates a synthetic <c>:</c> token.</summary>
    public static SyntaxToken Colon(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Colon, ":", leadingTrivia);

    /// <summary>Creates a synthetic <c>,</c> token.</summary>
    public static SyntaxToken Comma(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Comma, ",", leadingTrivia);

    /// <summary>Creates a synthetic <c>=</c> token.</summary>
    public static SyntaxToken Equal(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Equal, "=", leadingTrivia);

    /// <summary>Creates a synthetic <c>-&gt;</c> token.</summary>
    public static SyntaxToken Arrow(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Arrow, "->", leadingTrivia);

    /// <summary>Creates a synthetic <c>@</c> token.</summary>
    public static SyntaxToken At(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.At, "@", leadingTrivia);

    /// <summary>Creates a synthetic <c>#</c> token.</summary>
    public static SyntaxToken Hash(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Hash, "#", leadingTrivia);

    /// <summary>Creates a synthetic <c>!</c> token.</summary>
    public static SyntaxToken Bang(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Bang, "!", leadingTrivia);

    /// <summary>Creates a synthetic <c>?</c> token.</summary>
    public static SyntaxToken Question(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Question, "?", leadingTrivia);

    /// <summary>Creates a synthetic <c>*</c> token.</summary>
    public static SyntaxToken Star(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Star, "*", leadingTrivia);

    /// <summary>Creates a synthetic <c>+</c> token.</summary>
    public static SyntaxToken Plus(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Plus, "+", leadingTrivia);

    /// <summary>Creates a synthetic <c>-</c> token.</summary>
    public static SyntaxToken Minus(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Minus, "-", leadingTrivia);

    /// <summary>Creates a synthetic <c>.</c> token.</summary>
    public static SyntaxToken Dot(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Dot, ".", leadingTrivia);

    /// <summary>Creates a synthetic <c>|</c> token.</summary>
    public static SyntaxToken Pipe(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Pipe, "|", leadingTrivia);

    // -----------------------------------------------------------------------
    // End-of-file sentinel
    // -----------------------------------------------------------------------

    /// <summary>Creates a synthetic end-of-file token.</summary>
    public static SyntaxToken EndOfFile(string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.EndOfFile, string.Empty, leadingTrivia);

    // -----------------------------------------------------------------------
    // Variable-text tokens
    // -----------------------------------------------------------------------

    /// <summary>Creates a synthetic identifier token with the supplied <paramref name="text"/>.</summary>
    public static SyntaxToken Identifier(string text, string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Identifier, text, leadingTrivia);

    /// <summary>Creates a synthetic integer literal token with the supplied <paramref name="text"/>.</summary>
    public static SyntaxToken Integer(string text, string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.Integer, text, leadingTrivia);

    /// <summary>
    /// Creates a synthetic string literal token with the supplied <paramref name="text"/>.
    /// The <paramref name="text"/> should include the surrounding double-quote characters,
    /// as it is stored verbatim.
    /// </summary>
    public static SyntaxToken StringLiteral(string text, string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.StringLiteral, text, leadingTrivia);

    /// <summary>
    /// Creates a synthetic SSA name token with the supplied <paramref name="text"/>
    /// (e.g. <c>%0</c> or <c>%arg0</c>).
    /// </summary>
    public static SyntaxToken SsaName(string text, string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.SsaName, text, leadingTrivia);

    /// <summary>
    /// Creates a synthetic block label token with the supplied <paramref name="text"/>
    /// (e.g. <c>^bb0</c> or <c>^entry</c>).
    /// </summary>
    public static SyntaxToken BlockLabel(string text, string? leadingTrivia = null) =>
        new SyntaxToken(TokenKind.BlockLabel, text, leadingTrivia);
}
