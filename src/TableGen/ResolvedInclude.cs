namespace TableGen;

/// <summary>
/// Represents a successfully resolved TableGen include.
/// </summary>
public sealed class ResolvedInclude
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="logicalPath">
    /// The logical path of the included file, used for diagnostics and include deduplication.
    /// </param>
    /// <param name="sourceText">The source text of the included file.</param>
    public ResolvedInclude(string logicalPath, string sourceText)
    {
        LogicalPath = logicalPath;
        SourceText = sourceText;
    }

    /// <summary>
    /// Gets the logical path of the included file, used for diagnostics and deduplication.
    /// </summary>
    public string LogicalPath { get; }

    /// <summary>
    /// Gets the source text of the included file.
    /// </summary>
    public string SourceText { get; }
}
