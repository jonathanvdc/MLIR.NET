namespace TableGen.Tests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using TableGen.Evaluation;

internal static class TestHelpers
{
    public static Record EvaluateSingleRecord(string source)
    {
        return Document.Parse(source).Evaluate().Records.Single();
    }

    public static Record EvaluateSingleRecordWithPrelude(string source)
    {
        return LoadWithPrelude(source).Evaluate().Records.Single();
    }

    public static Document LoadWithPrelude(string source)
    {
        return Document.Load(source, new TableGenDictionaryIncludeResolver(PreludeFiles));
    }

    private static readonly IReadOnlyDictionary<string, string> PreludeFiles = Directory
        .GetFiles(Path.Combine(GetRepositoryRoot(), "src", "MLIR.Generators", "Prelude", "mlir", "IR"), "*.td")
        .ToDictionary(
            static path => "mlir/IR/" + Path.GetFileName(path),
            static path => File.ReadAllText(path));

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MLIR.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            throw new DirectoryNotFoundException("Could not locate the MLIR.NET repository root.");
        }

        return directory.FullName;
    }
}
