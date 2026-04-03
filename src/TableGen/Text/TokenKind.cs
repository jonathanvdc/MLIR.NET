namespace TableGen.Text;

/// <summary>
/// Classifies the tokens emitted by the TableGen lexer.
/// </summary>
internal enum TokenKind
{
    /// <summary>The synthetic end-of-file token.</summary>
    EndOfFile,
    /// <summary>An identifier or unreserved name.</summary>
    Identifier,
    /// <summary>An integer literal token.</summary>
    Integer,
    /// <summary>A string literal token.</summary>
    String,
    /// <summary>A <c>[{ ... }]</c> code block literal token.</summary>
    CodeBlock,
    /// <summary>The <c>class</c> keyword.</summary>
    ClassKeyword,
    /// <summary>The <c>def</c> keyword.</summary>
    DefKeyword,
    /// <summary>The <c>let</c> keyword.</summary>
    LetKeyword,
    /// <summary>The <c>in</c> keyword.</summary>
    InKeyword,
    /// <summary>The <c>include</c> keyword.</summary>
    IncludeKeyword,
    /// <summary>The <c>assert</c> keyword.</summary>
    AssertKeyword,
    /// <summary>A colon punctuation token.</summary>
    Colon,
    /// <summary>A semicolon punctuation token.</summary>
    Semicolon,
    /// <summary>A comma punctuation token.</summary>
    Comma,
    /// <summary>An equals punctuation token.</summary>
    Equal,
    /// <summary>A less-than punctuation token.</summary>
    LessThan,
    /// <summary>A greater-than punctuation token.</summary>
    GreaterThan,
    /// <summary>A left-brace punctuation token.</summary>
    LBrace,
    /// <summary>A right-brace punctuation token.</summary>
    RBrace,
    /// <summary>A left-parenthesis punctuation token.</summary>
    LParen,
    /// <summary>A right-parenthesis punctuation token.</summary>
    RParen,
    /// <summary>A left-bracket punctuation token.</summary>
    LBracket,
    /// <summary>A right-bracket punctuation token.</summary>
    RBracket,
    /// <summary>A dollar-sign punctuation token.</summary>
    Dollar,
    /// <summary>A hash punctuation token.</summary>
    Hash,
    /// <summary>A dot punctuation token.</summary>
    Dot,
    /// <summary>A question-mark punctuation token.</summary>
    QuestionMark,
    /// <summary>A bang operator token whose text stores the operator name without the leading <c>!</c>.</summary>
    BangKeyword,
    /// <summary>The <c>defvar</c> keyword.</summary>
    DefVarKeyword,
}
