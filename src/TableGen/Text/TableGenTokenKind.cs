namespace TableGen.Text;

internal enum TableGenTokenKind
{
    EndOfFile,
    Identifier,
    Integer,
    String,
    CodeBlock,
    ClassKeyword,
    DefKeyword,
    LetKeyword,
    InKeyword,
    Colon,
    Semicolon,
    Comma,
    Equal,
    LessThan,
    GreaterThan,
    LBrace,
    RBrace,
    LParen,
    RParen,
    LBracket,
    RBracket,
    Dollar,
}
