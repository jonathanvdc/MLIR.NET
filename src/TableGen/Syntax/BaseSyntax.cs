namespace TableGen.Syntax;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents a base-class application in a TableGen class or def declaration.
/// </summary>
public sealed class BaseSyntax(string name, IReadOnlyList<ExpressionSyntax> arguments, SourceLocation location = default)
{
    /// <summary>
    /// Gets the base-class name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the base-class arguments.
    /// </summary>
    public IReadOnlyList<ExpressionSyntax> Arguments { get; } = arguments;

    /// <summary>
    /// Gets the source location of the base-class application.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
