namespace MLIR.Generators;

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Minimal incremental generator scaffold for MLIR dialect generation from TableGen/ODS inputs.
/// </summary>
[Generator]
public sealed class OdsDialectGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tableGenFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".td", System.StringComparison.OrdinalIgnoreCase))
            .Collect();

        context.RegisterSourceOutput(tableGenFiles, static (productionContext, files) =>
        {
            var source = GeneratePlaceholder(files);
            productionContext.AddSource("OdsDialectGenerator.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static string GeneratePlaceholder(System.Collections.Immutable.ImmutableArray<AdditionalText> files)
    {
        return
            "namespace MLIR.Generated;\n" +
            "\n" +
            "/// <summary>\n" +
            "/// Tracks the additional TableGen inputs seen by the incremental generator.\n" +
            "/// </summary>\n" +
            "internal static class OdsGeneratorInputs\n" +
            "{\n" +
            "    /// <summary>\n" +
            "    /// Gets the number of `.td` files currently visible to the generator.\n" +
            "    /// </summary>\n" +
            "    public const int TableGenFileCount = " + files.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";\n" +
            "}\n";
    }
}
