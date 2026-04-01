namespace TableGen;

using System.Collections.Generic;

/// <summary>
/// Combines multiple <see cref="TableGenIncludeResolver"/> instances, trying each in order.
/// The first resolver that successfully resolves an include wins.
/// </summary>
public sealed class TableGenCompositeIncludeResolver : TableGenIncludeResolver
{
    private readonly IReadOnlyList<TableGenIncludeResolver> resolvers;

    /// <summary>
    /// Initializes a new composite resolver with the given sub-resolvers.
    /// </summary>
    /// <param name="resolvers">The resolvers to try in priority order (first match wins).</param>
    public TableGenCompositeIncludeResolver(params TableGenIncludeResolver[] resolvers)
    {
        this.resolvers = resolvers;
    }

    /// <inheritdoc/>
    public override bool TryResolveInclude(
        string includePath,
        TableGenSourceFile? includingFile,
        out TableGenResolvedInclude resolvedInclude)
    {
        foreach (var resolver in resolvers)
        {
            if (resolver.TryResolveInclude(includePath, includingFile, out resolvedInclude))
            {
                return true;
            }
        }

        resolvedInclude = null!;
        return false;
    }
}
