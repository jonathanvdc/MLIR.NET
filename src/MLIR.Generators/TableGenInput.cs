namespace MLIR.Generators;

/// <summary>
/// Represents one in-memory TableGen input file to compile.
/// </summary>
public sealed class TableGenInput
{
    /// <summary>
    /// Initializes a new instance of <see cref="TableGenInput"/>.
    /// </summary>
    public TableGenInput(string path, string sourceText)
    {
        Path = path;
        SourceText = sourceText;
    }

    /// <summary>
    /// Gets the logical path for this input. It is used for diagnostics and relative include resolution.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the full TableGen source text for this input.
    /// </summary>
    public string SourceText { get; }
}
