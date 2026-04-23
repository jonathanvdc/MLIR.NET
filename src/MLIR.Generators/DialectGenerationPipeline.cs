namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using MLIR.ODS;
using MLIR.ODS.Model;
using TableGen;

internal static class DialectGenerationPipeline
{
    public static IReadOnlyList<DialectModel> ParseAndMerge(
        ImmutableArray<AdditionalText> additionalTexts,
        System.Threading.CancellationToken cancellationToken,
        SourceProductionContext productionContext)
    {
        var includeResolver = BuildIncludeResolver(additionalTexts, cancellationToken);
        var results = ImmutableArray.CreateRange(
            additionalTexts,
            static (file, state) => DialectGeneratorInput.ParseFile(file, state.Resolver, state.Token),
            (Resolver: includeResolver, Token: cancellationToken));

        return MergeDialects(
            results,
            result => productionContext.ReportDiagnostic(
                Diagnostic.Create(
                    DialectGeneratorDiagnostics.InvalidTableGenInput,
                    Location.None,
                    result.Path,
                    result.ErrorMessage))).ToArray();
    }

    internal static IReadOnlyList<DialectModel> ParseAndMerge(
        IReadOnlyList<TableGenInput> inputs,
        IncludeResolver includeResolver)
    {
        var results = inputs
            .Select(input => DialectGeneratorInput.ParseFile(input, includeResolver))
            .ToArray();
        return MergeDialects(results).ToArray();
    }

    private static IncludeResolver BuildIncludeResolver(
        ImmutableArray<AdditionalText> additionalTexts,
        System.Threading.CancellationToken cancellationToken)
    {
        return new CompositeIncludeResolver(
            new ConsumerFileResolver(additionalTexts, cancellationToken),
            new EmbeddedPreludeResolver());
    }

    private static IEnumerable<DialectModel> MergeDialects(
        IReadOnlyList<ParsedDialectFile> results,
        Action<ParsedDialectFile>? reportError = null)
    {
        var dialects = new List<DialectModel>();
        foreach (var result in results)
        {
            if (result.ErrorMessage != null)
            {
                reportError?.Invoke(result);
                continue;
            }

            dialects.AddRange(result.Dialects);
        }

        return dialects
            .GroupBy(static dialect => dialect.Name, StringComparer.Ordinal)
            .Select(DialectModelMerger.MergeDialectGroup)
            .OrderBy(static dialect => dialect.Name, StringComparer.Ordinal);
    }
}
