namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a TableGen template parameter.
/// </summary>
public sealed class TemplateParameterSyntax(string typeName, string name, ExpressionSyntax? defaultValue, SourceLocation location = default)
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
    public ExpressionSyntax? DefaultValue { get; } = defaultValue;

    /// <summary>
    /// Gets the source location of the template parameter.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
