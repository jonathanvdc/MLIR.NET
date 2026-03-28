namespace MLIR.Text;

internal readonly struct MlirToken
{
    public MlirToken(
        MlirTokenKind kind,
        string leadingTrivia,
        string text,
        int fullStart,
        int tokenStart,
        int end,
        int line,
        int column)
    {
        Kind = kind;
        LeadingTrivia = leadingTrivia;
        Text = text;
        FullStart = fullStart;
        TokenStart = tokenStart;
        End = end;
        Line = line;
        Column = column;
    }

    public MlirTokenKind Kind { get; }

    public string LeadingTrivia { get; }

    public string Text { get; }

    public int FullStart { get; }

    public int TokenStart { get; }

    public int End { get; }

    public int Line { get; }

    public int Column { get; }
}
