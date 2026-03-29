namespace TableGen.Syntax;

/// <summary>
/// Represents a field declaration in a TableGen body.
/// </summary>
public sealed class FieldSyntax(string typeName, string name, ExpressionSyntax? initializer) : BodyItemSyntax
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
    public ExpressionSyntax? Initializer { get; } = initializer;
}
