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
        var resourceStream = OpenPreludeResource(includePath);
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

    private static Stream? OpenPreludeResource(string includePath)
    {
        foreach (var candidate in GetCandidateResourceNames(includePath))
        {
            var stream = ThisAssembly.GetManifestResourceStream(candidate);
            if (stream != null)
            {
                return stream;
            }
        }

        return null;
    }

    private static string[] GetCandidateResourceNames(string includePath)
    {
        if (includePath.StartsWith("mlir/Extensions/", System.StringComparison.Ordinal)
            || includePath.StartsWith("mlir/Upstream/", System.StringComparison.Ordinal))
        {
            return [includePath];
        }

        // Embedded resources expose Include files at their original logical path and
        // Upstream files under the mlir/Upstream/... namespace. Prefer Include first.
        return
        [
            includePath,
            "mlir/Upstream/" + includePath.Substring("mlir/".Length),
        ];
    }
}
