namespace TableGen.Syntax;

/// <summary>
/// Represents an identifier reference.
/// </summary>
public sealed class IdentifierSyntax(string name) : ExpressionSyntax
{
    /// <summary>
    /// Gets the referenced identifier name.
    /// </summary>
    public string Name { get; } = name;
}
