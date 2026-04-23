namespace MLIR.Generators;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

/// <summary>
/// Represents the outcome of compiling one or more TableGen inputs into generated dialect sources.
/// </summary>
public sealed class GeneratedDialectCompilationResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="GeneratedDialectCompilationResult"/>.
    /// </summary>
    public GeneratedDialectCompilationResult(
        IReadOnlyList<GeneratedDialectSource> generatedSources,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        GeneratedSources = generatedSources;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets the successfully generated dialect source files.</summary>
    public IReadOnlyList<GeneratedDialectSource> GeneratedSources { get; }

    /// <summary>Gets the diagnostics emitted while parsing or generating the requested inputs.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets whether the compilation completed without diagnostics.</summary>
    public bool IsSuccess => Diagnostics.Count == 0;
}
