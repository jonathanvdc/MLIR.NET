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
/// <item><description><c>mlir/IR/*</c> – upstream MLIR IR prelude definitions embedded as resources</description></item>
/// <item><description><c>mlir/Interfaces/*</c> – upstream MLIR interface definitions required by embedded dialect preludes</description></item>
/// <item><description><c>mlir/Dialect/Arith/IR/*</c> – upstream MLIR Arith ODS definitions</description></item>
/// </list>
/// </remarks>
internal sealed class EmbeddedPreludeResolver : IncludeResolver
{
    private static readonly Assembly ThisAssembly = typeof(EmbeddedPreludeResolver).Assembly;

    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        SourceFile? includingFile,
        out ResolvedInclude resolvedInclude)
    {
        // Resource names are stored with the LogicalName set in the .csproj, which uses
        // forward slashes (e.g. "mlir/IR/OpBase.td").
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

        resolvedInclude = new ResolvedInclude(includePath, text);
        return true;
    }
}
