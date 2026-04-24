namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents an integer literal.
/// </summary>
public sealed class IntegerSyntax(int value, SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public int Value { get; } = value;
}
