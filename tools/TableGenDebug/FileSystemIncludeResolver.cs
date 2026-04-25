namespace TableGenDebug;

using System;
using System.IO;
using MLIR.Text;
using TableGen;

/// <summary>
/// Resolves TableGen includes from the local file system.
/// </summary>
internal sealed class FileSystemIncludeResolver : IncludeResolver
{
    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        SourceDocument? includingFile,
        out SourceDocument resolvedDocument)
    {
        foreach (var candidatePath in GetCandidatePaths(includePath, includingFile))
        {
            if (File.Exists(candidatePath))
            {
                resolvedDocument = new OriginalSourceDocument(File.ReadAllText(candidatePath), candidatePath);
                return true;
            }
        }

        resolvedDocument = null!;
        return false;
    }

    private static IEnumerable<string> GetCandidatePaths(string includePath, SourceDocument? includingFile)
    {
        var candidates = new List<string>();
        if (Path.IsPathRooted(includePath))
        {
            candidates.Add(Path.GetFullPath(includePath));
        }
        else
        {
            candidates.Add(Path.GetFullPath(includePath));

            if (includingFile != null)
            {
                var includingDirectory = Path.GetDirectoryName(includingFile.FileName);
                if (includingDirectory != null)
                {
                    candidates.Add(Path.GetFullPath(Path.Combine(includingDirectory, includePath)));
                }
            }
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
