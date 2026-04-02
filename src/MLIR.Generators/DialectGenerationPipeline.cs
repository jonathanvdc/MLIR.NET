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

        return MergeDialects(results, productionContext).ToArray();
    }

    private static TableGenIncludeResolver BuildIncludeResolver(
        ImmutableArray<AdditionalText> additionalTexts,
        System.Threading.CancellationToken cancellationToken)
    {
        return new TableGenCompositeIncludeResolver(
            new ConsumerFileResolver(additionalTexts, cancellationToken),
            new EmbeddedPreludeResolver());
    }

    private static IEnumerable<DialectModel> MergeDialects(
        IReadOnlyList<ParsedDialectFile> results,
        SourceProductionContext productionContext)
    {
        var dialects = new List<DialectModel>();
        foreach (var result in results)
        {
            if (result.ErrorMessage != null)
            {
                productionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        DialectGeneratorDiagnostics.InvalidTableGenInput,
                        Location.None,
                        result.Path,
                        result.ErrorMessage));
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
