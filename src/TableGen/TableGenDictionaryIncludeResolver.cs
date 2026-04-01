namespace TableGen;

using System.Collections.Generic;

/// <summary>
/// A simple include resolver that serves includes from an in-memory dictionary.
/// Suitable for unit tests and lightweight single-process scenarios.
/// </summary>
public sealed class TableGenDictionaryIncludeResolver : TableGenIncludeResolver
{
    private readonly IReadOnlyDictionary<string, string> files;

    /// <summary>
    /// Initializes a new instance with the given file map.
    /// </summary>
    /// <param name="files">
    /// A mapping from logical include paths to their source text.
    /// </param>
    public TableGenDictionaryIncludeResolver(IReadOnlyDictionary<string, string> files)
    {
        this.files = files;
    }

    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        TableGenSourceFile? includingFile,
        out TableGenResolvedInclude resolvedInclude)
    {
        if (files.TryGetValue(includePath, out var sourceText))
        {
            resolvedInclude = new TableGenResolvedInclude(includePath, sourceText);
            return true;
        }

        resolvedInclude = null!;
        return false;
    }
}
