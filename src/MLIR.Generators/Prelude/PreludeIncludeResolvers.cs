namespace MLIR.Generators;

using TableGen;

/// <summary>
/// Creates include resolvers for the TableGen prelude resources embedded in <c>MLIR.Generators</c>.
/// </summary>
public static class PreludeIncludeResolvers
{
    /// <summary>
    /// Creates an include resolver that serves the embedded MLIR prelude files.
    /// </summary>
    public static IncludeResolver CreateEmbeddedPreludeResolver()
    {
        return new EmbeddedPreludeResolver();
    }
}
