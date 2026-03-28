namespace TableGen.Syntax;

/// <summary>
/// Represents a field declaration in a TableGen body.
/// </summary>
public sealed class TableGenFieldSyntax(string typeName, string name, TableGenExpressionSyntax? initializer) : TableGenBodyItemSyntax
{
    /// <summary>
    /// Gets the declared type name.
    /// </summary>
    public string TypeName { get; } = typeName;

    /// <summary>
    /// Gets the field name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the optional field initializer.
    /// </summary>
    public TableGenExpressionSyntax? Initializer { get; } = initializer;
}
