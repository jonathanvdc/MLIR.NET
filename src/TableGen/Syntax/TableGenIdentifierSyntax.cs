namespace TableGen.Syntax;

/// <summary>
/// Represents an identifier reference.
/// </summary>
public sealed class TableGenIdentifierSyntax(string name) : TableGenExpressionSyntax
{
    /// <summary>
    /// Gets the referenced identifier name.
    /// </summary>
    public string Name { get; } = name;
}
