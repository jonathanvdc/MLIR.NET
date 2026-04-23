namespace MLIR.Generators;

/// <summary>
/// Represents one generated C# source file produced from one merged dialect model.
/// </summary>
public sealed class GeneratedDialectSource
{
    /// <summary>
    /// Initializes a new instance of <see cref="GeneratedDialectSource"/>.
    /// </summary>
    public GeneratedDialectSource(
        string dialectName,
        string hintName,
        string sourceText,
        bool isPrelude)
    {
        DialectName = dialectName;
        HintName = hintName;
        SourceText = sourceText;
        IsPrelude = isPrelude;
    }

    /// <summary>
    /// Gets the logical dialect name that produced this source file.
    /// </summary>
    public string DialectName { get; }

    /// <summary>
    /// Gets the generated Roslyn hint name, which also makes a good output file name.
    /// </summary>
    public string HintName { get; }

    /// <summary>
    /// Gets the generated C# source text.
    /// </summary>
    public string SourceText { get; }

    /// <summary>
    /// Gets a value indicating whether this source belongs to the shared prelude dialect.
    /// </summary>
    public bool IsPrelude { get; }
}
