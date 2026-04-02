namespace TableGen.Syntax;

/// <summary>
/// Represents a top-level TableGen defvar declaration, e.g. defvar Name = expr;
/// </summary>
public sealed class DefVarSyntax(string name, ExpressionSyntax value) : TopLevelSyntax
{
    /// <summary>
    /// Gets the name of the constant.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the initializer expression.
    /// </summary>
    public ExpressionSyntax Value { get; } = value;
}
