namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a top-level TableGen defvar declaration, e.g. defvar Name = expr;
/// </summary>
public sealed class DefVarSyntax(string name, ExpressionSyntax value, SourceLocation location = default) : TopLevelSyntax(location)
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
