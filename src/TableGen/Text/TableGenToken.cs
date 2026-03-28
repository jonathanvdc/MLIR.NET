namespace TableGen.Text;

internal readonly struct TableGenToken(TableGenTokenKind kind, string text, int position, int line, int column)
{
    public TableGenTokenKind Kind { get; } = kind;

    public string Text { get; } = text;

    public int Position { get; } = position;

    public int Line { get; } = line;

    public int Column { get; } = column;
}
