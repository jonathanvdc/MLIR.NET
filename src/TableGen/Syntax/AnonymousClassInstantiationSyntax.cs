namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen anonymous class instantiation expression, e.g. ClassName&lt;args&gt;.
/// Field access on the result is represented separately by <see cref="FieldAccessSyntax"/>.
/// </summary>
public sealed class AnonymousClassInstantiationSyntax(
    string className,
    IReadOnlyList<ExpressionSyntax> arguments) : ExpressionSyntax
{
    /// <summary>
    /// Gets the class name to instantiate.
    /// </summary>
    public string ClassName { get; } = className;

    /// <summary>
    /// Gets the template arguments passed to the class.
    /// </summary>
    public IReadOnlyList<ExpressionSyntax> Arguments { get; } = arguments;
}
