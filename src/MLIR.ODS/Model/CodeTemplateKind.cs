namespace MLIR.ODS.Model;

/// <summary>
/// Describes the structural role of a <see cref="CodeTemplate"/> snippet.
/// The kind is informational and is used to document intent; no C# syntax
/// validation is performed.
/// </summary>
public enum CodeTemplateKind
{
    /// <summary>
    /// A single C# expression (e.g., a method call, property access, or conditional expression).
    /// </summary>
    Expression,

    /// <summary>
    /// A single C# statement, including the trailing semicolon.
    /// </summary>
    Statement,

    /// <summary>
    /// A block of one or more C# statements, not wrapped in braces.
    /// </summary>
    StatementBlock,

    /// <summary>
    /// A C# type name or type expression (e.g., <c>string</c>, <c>global::MLIR.Foo.Bar</c>).
    /// </summary>
    TypeName,
}
