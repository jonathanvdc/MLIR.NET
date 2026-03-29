namespace TableGen.Text;

internal readonly struct Token(TokenKind kind, string text, int position, int line, int column)
{
    public TokenKind Kind { get; } = kind;

    public string Text { get; } = text;

    public int Position { get; } = position;

    public int Line { get; } = line;

    public int Column { get; } = column;
}
