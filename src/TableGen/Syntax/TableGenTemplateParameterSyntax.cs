namespace TableGen.Syntax;

/// <summary>
/// Represents a TableGen template parameter.
/// </summary>
public sealed class TableGenTemplateParameterSyntax(string typeName, string name, TableGenExpressionSyntax? defaultValue)
{
    /// <summary>
    /// Gets the declared type name.
    /// </summary>
    public string TypeName { get; } = typeName;

    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the optional default value.
    /// </summary>
    public TableGenExpressionSyntax? DefaultValue { get; } = defaultValue;
}
