namespace MLIR.Generators;

using System.IO;
using System.Reflection;
using TableGen;

/// <summary>
/// Resolves TableGen include directives against prelude <c>.td</c> files embedded as resources
/// in the <c>MLIR.Generators</c> assembly.
/// </summary>
/// <remarks>
/// The following virtual include paths are supported:
/// <list type="bullet">
/// <item><description><c>mlir/OpBase.td</c> – core ODS base classes and common traits</description></item>
/// <item><description><c>mlir/CommonTypes.td</c> – common integer and floating-point type constraints</description></item>
/// <item><description><c>mlir/CommonAttrs.td</c> – common attribute constraints</description></item>
/// </list>
/// </remarks>
internal sealed class EmbeddedPreludeResolver : TableGenIncludeResolver
{
    private static readonly Assembly ThisAssembly = typeof(EmbeddedPreludeResolver).Assembly;

    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        TableGenSourceFile? includingFile,
        out TableGenResolvedInclude resolvedInclude)
    {
        // Resource names are stored with the LogicalName set in the .csproj, which uses
        // forward slashes (e.g. "mlir/OpBase.td").
        var resourceStream = ThisAssembly.GetManifestResourceStream(includePath);
        if (resourceStream == null)
        {
            resolvedInclude = null!;
            return false;
        }

        string text;
        using (var reader = new StreamReader(resourceStream))
        {
            text = reader.ReadToEnd();
        }

        resolvedInclude = new TableGenResolvedInclude(includePath, text);
        return true;
    }
}
