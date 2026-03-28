namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen dag expression such as <c>(ins I32:$lhs)</c>.
/// </summary>
public sealed class TableGenDagSyntax(string operatorName, IReadOnlyList<TableGenDagArgumentSyntax> arguments) : TableGenExpressionSyntax
{
    /// <summary>
    /// Gets the dag operator name.
    /// </summary>
    public string OperatorName { get; } = operatorName;

    /// <summary>
    /// Gets the dag arguments.
    /// </summary>
    public IReadOnlyList<TableGenDagArgumentSyntax> Arguments { get; } = arguments;
}
