namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen bang operator call expression, e.g. !if(cond, a, b).
/// </summary>
public sealed class BangCallSyntax(string operatorName, IReadOnlyList<ExpressionSyntax> arguments, string? typeArgument = null) : ExpressionSyntax
{
    /// <summary>
    /// Gets the bang operator name, e.g. "if", "gt", "size".
    /// </summary>
    public string OperatorName { get; } = operatorName;

    /// <summary>
    /// Gets the argument expressions.
    /// </summary>
    public IReadOnlyList<ExpressionSyntax> Arguments { get; } = arguments;

    /// <summary>
    /// Gets the optional type argument used by operators such as <c>!isa&lt;T&gt;</c>.
    /// </summary>
    public string? TypeArgument { get; } = typeArgument;
}
