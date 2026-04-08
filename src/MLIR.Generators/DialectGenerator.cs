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
        var compilationAndFiles = context.CompilationProvider.Combine(tableGenFiles);

        context.RegisterSourceOutput(compilationAndFiles, static (productionContext, pair) =>
        {
            var compilation = pair.Left;
            var files = pair.Right;
            var dialects = DialectGenerationPipeline.ParseAndMerge(files, productionContext.CancellationToken, productionContext);
            var resolver = DialectSymbolResolver.Create(dialects);
            foreach (var dialect in dialects)
            {
                if (IsAlreadyProvided(compilation, dialect))
                {
                    continue;
                }

                try
                {
                    productionContext.AddSource(
                        DialectGeneratorNaming.GetHintName(dialect),
                        SourceText.From(DialectSourceEmitter.GenerateDialectSource(dialect, resolver), Encoding.UTF8));
                }
                catch (Exception exception)
                {
                    productionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            DialectGeneratorDiagnostics.DialectEmissionFailed,
                            Location.None,
                            dialect.Name,
                            exception.ToString()));
                }
            }
        });
    }

    private static bool IsAlreadyProvided(Compilation compilation, DialectModel dialect)
    {
        var metadataName = DialectGeneratorNaming.GetGeneratedNamespace(dialect)
            + "."
            + DialectGeneratorNaming.GetDialectRegistrationClassName(dialect);
        return compilation.GetTypeByMetadataName(metadataName) != null;
    }
}
