namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a TableGen expression.
/// </summary>
public abstract class ExpressionSyntax(SourceLocation location = default)
{
    /// <summary>
    /// Gets the source location of the expression.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
