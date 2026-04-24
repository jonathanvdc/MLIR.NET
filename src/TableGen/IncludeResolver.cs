namespace TableGen;

using MLIR.Text;

/// <summary>
/// Abstract base class for resolving TableGen include directives.
/// </summary>
public abstract class IncludeResolver
{
    /// <summary>
    /// Attempts to resolve a TableGen include directive.
    /// </summary>
    /// <param name="includePath">The include path as written in the source file.</param>
    /// <param name="includingFile">
    /// The source document that contains the include directive, or <see langword="null"/> if
    /// resolving from the root document.
    /// </param>
    /// <param name="resolvedDocument">
    /// When this method returns <see langword="true"/>, contains the resolved source document.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the include was successfully resolved; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public abstract bool TryResolveInclude(
        string includePath,
        SourceDocument? includingFile,
        out SourceDocument resolvedDocument);
}
