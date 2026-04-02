namespace TableGen.Syntax;

/// <summary>
/// Represents a TableGen field access expression, e.g. expr.fieldName.
/// </summary>
public sealed class FieldAccessSyntax(ExpressionSyntax @object, string fieldName) : ExpressionSyntax
{
    /// <summary>
    /// Gets the object expression whose field is being accessed.
    /// </summary>
    public ExpressionSyntax Object { get; } = @object;

    /// <summary>
    /// Gets the name of the field to access.
    /// </summary>
    public string FieldName { get; } = fieldName;
}
