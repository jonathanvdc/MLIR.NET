namespace TableGen.Syntax;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents a TableGen class instantiation used as an expression, e.g. ClassName&lt;args&gt;.fieldName.
/// This pattern is used to simulate functions in TableGen by instantiating a class and
/// reading back a computed field from the resulting instance.
/// </summary>
public sealed class ClassInstantiationSyntax(
    string className,
    IReadOnlyList<ExpressionSyntax> arguments,
    string fieldName,
    SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the class name to instantiate.
    /// </summary>
    public string ClassName { get; } = className;

    /// <summary>
    /// Gets the template arguments passed to the class.
    /// </summary>
    public IReadOnlyList<ExpressionSyntax> Arguments { get; } = arguments;

    /// <summary>
    /// Gets the name of the field to read from the instantiated class.
    /// </summary>
    public string FieldName { get; } = fieldName;
}
