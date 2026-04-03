namespace TableGen;

using System.Collections.Generic;

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
        SourceFile? includingFile,
        out ResolvedInclude resolvedInclude)
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
