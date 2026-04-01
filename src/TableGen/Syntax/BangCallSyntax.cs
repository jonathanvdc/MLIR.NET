namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen bang operator call expression, e.g. !if(cond, a, b).
/// </summary>
public sealed class BangCallSyntax(string operatorName, IReadOnlyList<ExpressionSyntax> arguments) : ExpressionSyntax
{
    /// <summary>
    /// Gets the bang operator name, e.g. "if", "gt", "size".
    /// </summary>
    public string OperatorName { get; } = operatorName;

    /// <summary>
    /// Gets the argument expressions.
    /// </summary>
    public IReadOnlyList<ExpressionSyntax> Arguments { get; } = arguments;
}
