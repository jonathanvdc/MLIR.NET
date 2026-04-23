namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.ODS;
using MLIR.ODS.Model;
using TableGen;

/// <summary>
/// Compiles standalone TableGen dialect files into the generated C# source that the MLIR
/// dialect source generator would normally emit.
/// </summary>
/// <remarks>
/// This API is intended for tooling and inspection workflows. It reuses the same ODS import,
/// dialect merge, symbol-resolution, and emission layers as the source generator, but exposes
/// them without requiring a Roslyn generator driver.
/// </remarks>
public static class TableGenDialectCompiler
{
    /// <summary>
    /// Compiles the given in-memory <c>.td</c> inputs into generated dialect source files.
    /// </summary>
    /// <param name="inputs">The TableGen files to compile.</param>
    /// <param name="includeResolver">
    /// The include resolver used to expand nested <c>include</c> directives.
    /// </param>
    /// <param name="includePrelude">
    /// <see langword="true"/> to include the shared prelude output; otherwise the returned
    /// sources omit the prelude dialect.
    /// </param>
    /// <param name="dialectNames">
    /// Optional dialect-name filter applied after merge. When supplied, only dialects whose
    /// logical names appear in the set are emitted.
    /// </param>
    /// <returns>The generated C# source files, ordered by dialect name.</returns>
    public static IReadOnlyList<GeneratedDialectSource> CompileSources(
        IEnumerable<TableGenInput> inputs,
        IncludeResolver includeResolver,
        bool includePrelude = false,
        IEnumerable<string>? dialectNames = null)
    {
        if (inputs == null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        if (includeResolver == null)
        {
            throw new ArgumentNullException(nameof(includeResolver));
        }

        var inputArray = inputs.ToArray();
        if (inputArray.Length == 0)
        {
            throw new ArgumentException("At least one input is required.", nameof(inputs));
        }

        var dialects = new List<DialectModel>();
        foreach (var input in inputArray)
        {
            if (input == null)
            {
                throw new ArgumentException("Inputs may not contain null items.", nameof(inputs));
            }

            if (string.IsNullOrWhiteSpace(input.Path))
            {
                throw new ArgumentException("Input paths may not be null, empty, or whitespace.", nameof(inputs));
            }

            var sourceFile = new SourceFile(input.Path);
            var document = Document.Load(input.SourceText, includeResolver, sourceFile);
            dialects.AddRange(DialectImporter.Import(document.Evaluate()));
        }

        var mergedDialects = dialects
            .GroupBy(static dialect => dialect.Name, StringComparer.Ordinal)
            .Select(DialectModelMerger.MergeDialectGroup)
            .OrderBy(static dialect => dialect.Name, StringComparer.Ordinal)
            .ToArray();

        var requestedNames = dialectNames == null
            ? null
            : new HashSet<string>(dialectNames.Where(static name => !string.IsNullOrWhiteSpace(name)), StringComparer.Ordinal);
        if (requestedNames != null && requestedNames.Count == 0)
        {
            requestedNames = null;
        }

        var selectedDialects = mergedDialects
            .Where(dialect => (includePrelude || !dialect.IsPrelude)
                && (requestedNames == null || requestedNames.Contains(dialect.Name)))
            .ToArray();

        var resolver = DialectSymbolResolver.Create(mergedDialects);
        return selectedDialects
            .Select(dialect => new GeneratedDialectSource(
                dialect.Name,
                DialectGeneratorNaming.GetHintName(dialect),
                DialectSourceEmitter.GenerateDialectSource(dialect, resolver),
                dialect.IsPrelude))
            .ToArray();
    }
}
