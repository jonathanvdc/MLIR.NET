namespace MLIR.Generators;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

/// <summary>
/// Represents the result of generating one dialect source file, including any diagnostics emitted
/// while building its contents.
/// </summary>
internal sealed class GeneratedDialectSourceResult
{
    public GeneratedDialectSourceResult(string source, IReadOnlyList<Diagnostic> diagnostics)
    {
        Source = source;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets the generated source text. This may be incomplete when diagnostics are present.</summary>
    public string Source { get; }

    /// <summary>Gets the diagnostics produced while attempting to emit the dialect.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets whether generation completed without any diagnostics.</summary>
    public bool IsSuccess => Diagnostics.Count == 0;
}
