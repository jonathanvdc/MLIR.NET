namespace MLIR.Generators;

using System.Collections.Generic;
using MLIR.Text;
using MLIR.ODS.Model;

internal readonly struct ParsedDialectFile
{
    public ParsedDialectFile(
        string path,
        IReadOnlyList<DialectModel> dialects,
        string? errorMessage,
        Diagnostic? tableGenDiagnostic = null)
    {
        Path = path;
        Dialects = dialects;
        ErrorMessage = errorMessage;
        TableGenDiagnostic = tableGenDiagnostic;
    }

    public string Path { get; }

    public IReadOnlyList<DialectModel> Dialects { get; }

    public string? ErrorMessage { get; }

    public Diagnostic? TableGenDiagnostic { get; }
}
