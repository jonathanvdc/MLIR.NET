namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen dag expression such as <c>(ins I32:$lhs)</c>.
/// </summary>
public sealed class DagSyntax(string operatorName, IReadOnlyList<DagArgumentSyntax> arguments) : ExpressionSyntax
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
