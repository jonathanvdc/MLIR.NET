namespace TableGen;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Combines multiple <see cref="IncludeResolver"/> instances, trying each in order.
/// The first resolver that successfully resolves an include wins.
/// </summary>
public sealed class CompositeIncludeResolver : IncludeResolver
{
    private readonly IReadOnlyList<IncludeResolver> resolvers;

    /// <summary>
    /// Initializes a new composite resolver with the given sub-resolvers.
    /// </summary>
    /// <param name="resolvers">The resolvers to try in priority order (first match wins).</param>
    public CompositeIncludeResolver(params IncludeResolver[] resolvers)
    {
        this.resolvers = resolvers;
    }

    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        SourceDocument? includingFile,
        out SourceDocument resolvedDocument)
    {
        foreach (var resolver in resolvers)
        {
            if (resolver.TryResolveInclude(includePath, includingFile, out resolvedDocument))
            {
                return true;
            }
        }

        resolvedDocument = null!;
        return false;
    }
}
