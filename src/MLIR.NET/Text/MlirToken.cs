namespace MLIR.Text;

internal readonly struct MlirToken
{
    public MlirToken(MlirTokenKind kind, int start, int end, int line, int column)
    {
        Kind = kind;
        Start = start;
        End = end;
        Line = line;
        Column = column;
    }

    public MlirTokenKind Kind { get; }

    public int Start { get; }

    public int End { get; }

    public int Line { get; }

    public int Column { get; }
}
