namespace MLIR.Text;

internal readonly struct MlirToken(
    MlirTokenKind kind,
    string leadingTrivia,
    string text,
    int fullStart,
    int tokenStart,
    int end,
    int line,
    int column)
{
    public MlirTokenKind Kind { get; } = kind;

    public string LeadingTrivia { get; } = leadingTrivia;

    public string Text { get; } = text;

    public int FullStart { get; } = fullStart;

    public int TokenStart { get; } = tokenStart;

    public int End { get; } = end;

    public int Line { get; } = line;

    public int Column { get; } = column;
}
