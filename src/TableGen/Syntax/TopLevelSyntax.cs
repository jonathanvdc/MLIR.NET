namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a top-level TableGen declaration.
/// </summary>
public abstract class TopLevelSyntax(SourceLocation location = default)
{
    /// <summary>
    /// Gets the source location of the declaration.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
