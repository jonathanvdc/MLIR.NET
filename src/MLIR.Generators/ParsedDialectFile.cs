namespace MLIR.Generators;

using System.Collections.Generic;
using MLIR.ODS.Model;

internal readonly struct ParsedDialectFile
{
    public ParsedDialectFile(string path, IReadOnlyList<DialectModel> dialects, string? errorMessage)
    {
        Path = path;
        Dialects = dialects;
        ErrorMessage = errorMessage;
    }

    public string Path { get; }

    public IReadOnlyList<DialectModel> Dialects { get; }

    public string? ErrorMessage { get; }
}
