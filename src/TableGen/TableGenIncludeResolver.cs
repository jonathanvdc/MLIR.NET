namespace TableGen;

/// <summary>
/// Abstract base class for resolving TableGen include directives.
/// </summary>
public abstract class TableGenIncludeResolver
{
    /// <summary>
    /// Attempts to resolve a TableGen include directive.
    /// </summary>
    /// <param name="includePath">The include path as written in the source file.</param>
    /// <param name="includingFile">
    /// The source file that contains the include directive, or <see langword="null"/> if
    /// resolving from the root document.
    /// </param>
    /// <param name="resolvedInclude">
    /// When this method returns <see langword="true"/>, contains the resolved include.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the include was successfully resolved; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public abstract bool TryResolveInclude(
        string includePath,
        TableGenSourceFile? includingFile,
        out TableGenResolvedInclude resolvedInclude);
}
