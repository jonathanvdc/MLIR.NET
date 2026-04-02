namespace MLIR.Generators;

using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MLIR.ODS.Model;

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
            var dialects = DialectGenerationPipeline.ParseAndMerge(files, productionContext.CancellationToken, productionContext);
            var resolver = DialectSymbolResolver.Create(dialects);
            foreach (var dialect in dialects)
            {
                productionContext.AddSource(
                    DialectGeneratorNaming.GetHintName(dialect),
                    SourceText.From(DialectSourceEmitter.GenerateDialectSource(dialect, resolver), Encoding.UTF8));
            }
        });
    }
}
