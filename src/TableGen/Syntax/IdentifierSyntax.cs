namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents an identifier reference.
/// </summary>
public sealed class IdentifierSyntax(string name, SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the referenced identifier name.
    /// </summary>
    public string Name { get; } = name;
}
