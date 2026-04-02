namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MLIR.ODS.Model;
using TableGen;

/// <summary>
/// Incremental generator for convention-based MLIR dialect generation from TableGen/ODS inputs.
/// </summary>
[Generator]
public sealed class DialectGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tableGenFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".td", StringComparison.OrdinalIgnoreCase))
            .Collect();

        context.RegisterSourceOutput(tableGenFiles, static (productionContext, files) =>
        {
            var resolver = BuildIncludeResolver(files, productionContext.CancellationToken);
            var results = ImmutableArray.CreateRange(
                files,
                static (file, state) => DialectGeneratorInput.ParseFile(file, state.Resolver, state.Token),
                (Resolver: resolver, Token: productionContext.CancellationToken));

            var dialects = GetMergedDialects(results, productionContext).ToArray();
            var resolver = DialectSymbolResolver.Create(dialects);
            foreach (var dialect in dialects)
            {
                productionContext.AddSource(
                    DialectGeneratorNaming.GetHintName(dialect),
                    SourceText.From(DialectSourceEmitter.GenerateDialectSource(dialect, resolver), Encoding.UTF8));
            }
        });
    }

    private static TableGenIncludeResolver BuildIncludeResolver(
        ImmutableArray<AdditionalText> additionalTexts,
        System.Threading.CancellationToken cancellationToken)
    {
        return new TableGenCompositeIncludeResolver(
            new ConsumerFileResolver(additionalTexts, cancellationToken),
            new EmbeddedPreludeResolver());
    }

    private static IEnumerable<DialectModel> GetMergedDialects(
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
            .Select(DialectGeneratorModel.MergeDialectGroup)
            .OrderBy(static dialect => dialect.Name, StringComparer.Ordinal);
    }
}
