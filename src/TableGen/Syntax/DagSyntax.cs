namespace TableGen.Syntax;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents a TableGen dag expression such as <c>(ins I32:$lhs)</c>.
/// </summary>
public sealed class DagSyntax(string operatorName, IReadOnlyList<DagArgumentSyntax> arguments, SourceLocation location = default) : ExpressionSyntax(location)
{
    /// <summary>
    /// Gets the dag operator name.
    /// </summary>
    public string OperatorName { get; } = operatorName;

    /// <summary>
    /// Gets the dag arguments.
    /// </summary>
    public IReadOnlyList<DagArgumentSyntax> Arguments { get; } = arguments;
}
