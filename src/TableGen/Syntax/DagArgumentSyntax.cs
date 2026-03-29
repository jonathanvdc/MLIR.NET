namespace TableGen.Syntax;

/// <summary>
/// Represents a single argument in a TableGen dag expression.
/// </summary>
public sealed class DagArgumentSyntax(ExpressionSyntax value, string? name)
{
    /// <summary>
    /// Gets the argument value.
    /// </summary>
    public ExpressionSyntax Value { get; } = value;

    /// <summary>
    /// Gets the optional argument name.
    /// </summary>
    public string? Name { get; } = name;
}
