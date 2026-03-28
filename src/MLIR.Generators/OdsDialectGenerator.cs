namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MLIR.ODS.Model;

/// <summary>
/// Incremental generator for convention-based MLIR dialect generation from TableGen/ODS inputs.
/// </summary>
[Generator]
public sealed class OdsDialectGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tableGenFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".td", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) => OdsDialectGeneratorInput.ParseFile(file, cancellationToken))
            .Collect();

        context.RegisterSourceOutput(tableGenFiles, static (productionContext, results) =>
        {
            foreach (var dialect in GetMergedDialects(results, productionContext))
            {
                productionContext.AddSource(
                    OdsDialectGeneratorNaming.GetHintName(dialect),
                    SourceText.From(OdsDialectSourceEmitter.GenerateDialectSource(dialect), Encoding.UTF8));
            }
        });
    }

    private static IEnumerable<OdsDialectModel> GetMergedDialects(
        IReadOnlyList<ParsedDialectFile> results,
        SourceProductionContext productionContext)
    {
        var dialects = new List<OdsDialectModel>();
        foreach (var result in results)
        {
            if (result.ErrorMessage != null)
            {
                productionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        OdsDialectGeneratorDiagnostics.InvalidTableGenInput,
                        Location.None,
                        result.Path,
                        result.ErrorMessage));
                continue;
            }

            dialects.AddRange(result.Dialects);
        }

        return dialects
            .GroupBy(static dialect => dialect.Name, StringComparer.Ordinal)
            .Select(OdsDialectGeneratorModel.MergeDialectGroup)
            .OrderBy(static dialect => dialect.Name, StringComparer.Ordinal);
    }
}
