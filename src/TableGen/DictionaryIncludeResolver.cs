namespace TableGen;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// A simple include resolver that serves includes from an in-memory dictionary.
/// Suitable for unit tests and lightweight single-process scenarios.
/// </summary>
public sealed class DictionaryIncludeResolver : IncludeResolver
{
    private readonly IReadOnlyDictionary<string, string> files;

    /// <summary>
    /// Initializes a new instance with the given file map.
    /// </summary>
    /// <param name="files">
    /// A mapping from logical include paths to their source text.
    /// </param>
    public DictionaryIncludeResolver(IReadOnlyDictionary<string, string> files)
    {
        this.files = files;
    }

    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        SourceDocument? includingFile,
        out SourceDocument resolvedDocument)
    {
        if (files.TryGetValue(includePath, out var sourceText))
        {
            resolvedDocument = new StringDocument(includePath, sourceText);
            return true;
        }

        resolvedDocument = null!;
        return false;
    }
}
