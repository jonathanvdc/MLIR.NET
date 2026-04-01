namespace TableGen;

/// <summary>
/// Identifies a TableGen source file within the include graph.
/// Used for diagnostics and include-path resolution.
/// </summary>
public sealed class TableGenSourceFile
{
    /// <summary>
    /// Initializes a new instance with the given logical path.
    /// </summary>
    /// <param name="logicalPath">The logical path that identifies this file.</param>
    public TableGenSourceFile(string logicalPath)
    {
        LogicalPath = logicalPath;
    }

    /// <summary>
    /// Gets the logical path used to identify this file in diagnostics and include tracking.
    /// </summary>
    public string LogicalPath { get; }
}
