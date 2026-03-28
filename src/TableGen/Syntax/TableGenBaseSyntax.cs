namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a base-class application in a TableGen class or def declaration.
/// </summary>
public sealed class TableGenBaseSyntax(string name, IReadOnlyList<TableGenExpressionSyntax> arguments)
{
    /// <summary>
    /// Gets the base-class name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the base-class arguments.
    /// </summary>
    public IReadOnlyList<TableGenExpressionSyntax> Arguments { get; } = arguments;
}
