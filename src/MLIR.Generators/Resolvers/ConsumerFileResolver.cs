namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using TableGen;

/// <summary>
/// Resolves TableGen include directives from consumer-provided <see cref="AdditionalText"/>
/// files that were passed to the source generator.
/// </summary>
/// <remarks>
/// Resolution strategy (in order):
/// <list type="number">
/// <item><description>Exact match of the include path against any known file path.</description></item>
/// <item><description>Path relative to the directory of the including file, when the including file is known.</description></item>
/// </list>
/// Path comparisons are case-insensitive on all platforms to match common file-system behavior.
/// </remarks>
internal sealed class ConsumerFileResolver : TableGenIncludeResolver
{
    private readonly IReadOnlyDictionary<string, string> filesByPath;

    /// <summary>
    /// Initializes a new instance from the given collection of additional texts.
    /// </summary>
    public ConsumerFileResolver(
        IEnumerable<AdditionalText> additionalTexts,
        System.Threading.CancellationToken cancellationToken)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var text in additionalTexts)
        {
            var content = text.GetText(cancellationToken)?.ToString();
            if (content != null)
            {
                dict[text.Path] = content;
            }
        }

        filesByPath = dict;
    }

    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        TableGenSourceFile? includingFile,
        out TableGenResolvedInclude resolvedInclude)
    {
        // 1. Exact path match.
        if (filesByPath.TryGetValue(includePath, out var text))
        {
            resolvedInclude = new TableGenResolvedInclude(includePath, text);
            return true;
        }

        // 2. Relative-to-including-file resolution.
        if (includingFile != null)
        {
            var dir = Path.GetDirectoryName(includingFile.LogicalPath);
            if (dir != null)
            {
                var combined = Path.Combine(dir, includePath);
                // Normalize the separator so the key lookup works on all platforms.
                var normalized = combined.Replace('\\', '/');
                if (filesByPath.TryGetValue(normalized, out var relativeText))
                {
                    resolvedInclude = new TableGenResolvedInclude(normalized, relativeText);
                    return true;
                }

                // Also try with OS-native separators.
                if (filesByPath.TryGetValue(combined, out relativeText))
                {
                    resolvedInclude = new TableGenResolvedInclude(combined, relativeText);
                    return true;
                }
            }
        }

        resolvedInclude = null!;
        return false;
    }
}
