namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a string literal.
/// </summary>
public sealed class StringSyntax(string value, SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; } = value;
}
