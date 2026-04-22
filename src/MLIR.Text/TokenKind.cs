namespace MLIR.Text;

/// <summary>
/// Identifies the lexical token kinds recognized by the generic MLIR lexer.
/// </summary>
public enum TokenKind
{
    /// <summary>
    /// Marks the logical end of the token stream.
    /// </summary>
    EndOfFile,

    /// <summary>
    /// Represents an unquoted identifier.
    /// </summary>
    Identifier,

    /// <summary>
    /// Represents an integer literal that appears in raw syntax.
    /// </summary>
    Integer,

    /// <summary>
    /// Represents a quoted string literal.
    /// </summary>
    StringLiteral,

    /// <summary>
    /// Represents an SSA value name such as <c>%0</c> or <c>%arg</c>.
    /// </summary>
    SsaName,

    /// <summary>
    /// Represents a block label such as <c>^bb0</c>.
    /// </summary>
    BlockLabel,

    /// <summary>
    /// Represents a symbol name such as <c>@foo</c> or <c>@"quoted-name"</c>.
    /// </summary>
    SymbolName,

    /// <summary>
    /// Represents the <c>@</c> token.
    /// </summary>
    At,

    /// <summary>
    /// Represents the <c>#</c> token.
    /// </summary>
    Hash,

    /// <summary>
    /// Represents the <c>!</c> token.
    /// </summary>
    Bang,

    /// <summary>
    /// Represents the <c>:</c> token.
    /// </summary>
    Colon,

    /// <summary>
    /// Represents the <c>,</c> token.
    /// </summary>
    Comma,

    /// <summary>
    /// Represents the <c>=</c> token.
    /// </summary>
    Equal,

    /// <summary>
    /// Represents the <c>-&gt;</c> token.
    /// </summary>
    Arrow,

    /// <summary>
    /// Represents the <c>(</c> token.
    /// </summary>
    LParen,

    /// <summary>
    /// Represents the <c>)</c> token.
    /// </summary>
    RParen,

    /// <summary>
    /// Represents the <c>{</c> token.
    /// </summary>
    LBrace,

    /// <summary>
    /// Represents the <c>}</c> token.
    /// </summary>
    RBrace,

    /// <summary>
    /// Represents the <c>[</c> token.
    /// </summary>
    LBracket,

    /// <summary>
    /// Represents the <c>]</c> token.
    /// </summary>
    RBracket,

    /// <summary>
    /// Represents the <c>&lt;</c> token.
    /// </summary>
    LessThan,

    /// <summary>
    /// Represents the <c>&gt;</c> token.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Represents the <c>?</c> token.
    /// </summary>
    Question,

    /// <summary>
    /// Represents the <c>*</c> token.
    /// </summary>
    Star,

    /// <summary>
    /// Represents the <c>+</c> token.
    /// </summary>
    Plus,

    /// <summary>
    /// Represents the <c>-</c> token when it is not part of <c>-&gt;</c>.
    /// </summary>
    Minus,

    /// <summary>
    /// Represents the <c>.</c> token.
    /// </summary>
    Dot,

    /// <summary>
    /// Represents the <c>|</c> token.
    /// </summary>
    Pipe,
}
