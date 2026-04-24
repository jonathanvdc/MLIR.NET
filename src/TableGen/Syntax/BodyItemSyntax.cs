namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a body item in a TableGen class or def declaration.
/// </summary>
public abstract class BodyItemSyntax(SourceLocation location = default)
{
    /// <summary>
    /// Gets the source location of the body item.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
