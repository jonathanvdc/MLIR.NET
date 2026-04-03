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
        return Document.Load(source, new DictionaryIncludeResolver(PreludeFiles));
    }

    private static readonly IReadOnlyDictionary<string, string> PreludeFiles = BuildPreludeFiles();

    private static IReadOnlyDictionary<string, string> BuildPreludeFiles()
    {
        var preludeRoot = Path.Combine(GetRepositoryRoot(), "src", "MLIR.Generators", "Prelude");
        var files = new Dictionary<string, string>(System.StringComparer.Ordinal);

        AddPreludeFiles(files, Path.Combine(preludeRoot, "Include"), static relativePath => relativePath);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Upstream"), static relativePath => relativePath, addOnlyIfMissing: true);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Upstream"), static relativePath => "mlir/Upstream/" + relativePath.Substring("mlir/".Length), addOnlyIfMissing: true);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Extensions"), static relativePath => "mlir/Extensions/" + relativePath.Substring("mlir/".Length), addOnlyIfMissing: true);

        return files;
    }

    private static void AddPreludeFiles(
        Dictionary<string, string> files,
        string directory,
        System.Func<string, string> logicalNameSelector,
        bool addOnlyIfMissing = false)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(directory, "*.td", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, path).Replace('\\', '/');
            var logicalName = logicalNameSelector(relativePath);
            if (addOnlyIfMissing && files.ContainsKey(logicalName))
            {
                continue;
            }

            files[logicalName] = File.ReadAllText(path);
        }
    }

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
