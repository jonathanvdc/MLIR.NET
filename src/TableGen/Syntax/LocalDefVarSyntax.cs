namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a body-local TableGen <c>defvar</c> declaration.
/// </summary>
public sealed class LocalDefVarSyntax(string name, ExpressionSyntax value, SourceLocation location = default) : BodyItemSyntax(location)
{
    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the assigned value.
    /// </summary>
    public ExpressionSyntax Value { get; } = value;
}
