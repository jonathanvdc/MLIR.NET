namespace TableGenDebug;

using System;
using System.IO;
using TableGen;

/// <summary>
/// Resolves TableGen includes from the local file system.
/// </summary>
internal sealed class FileSystemIncludeResolver : IncludeResolver
{
    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        SourceFile? includingFile,
        out ResolvedInclude resolvedInclude)
    {
        foreach (var candidatePath in GetCandidatePaths(includePath, includingFile))
        {
            if (File.Exists(candidatePath))
            {
                resolvedInclude = new ResolvedInclude(candidatePath, File.ReadAllText(candidatePath));
                return true;
            }
        }

        resolvedInclude = null!;
        return false;
    }

    private static IEnumerable<string> GetCandidatePaths(string includePath, SourceFile? includingFile)
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
                var includingDirectory = Path.GetDirectoryName(includingFile.LogicalPath);
                if (includingDirectory != null)
                {
                    candidates.Add(Path.GetFullPath(Path.Combine(includingDirectory, includePath)));
                }
            }
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
