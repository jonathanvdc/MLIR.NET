namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a TableGen <c>extends</c> overlay declaration.
/// </summary>
public sealed class ExtendsSyntax(
    string targetName,
    IReadOnlyList<BaseSyntax> bases,
    IReadOnlyList<LetSyntax> topLevelLets,
    IReadOnlyList<LetSyntax> bodyLets) : TopLevelSyntax
{
    /// <summary>
    /// Gets the target record name that receives the overlay fields.
    /// </summary>
    public string TargetName { get; } = targetName;

    /// <summary>
    /// Gets the schema classes used to validate and materialize the overlay.
    /// </summary>
    public IReadOnlyList<BaseSyntax> Bases { get; } = bases;

    /// <summary>
    /// Gets top-level <c>let ... in</c> bindings lexically applied to this overlay.
    /// </summary>
    public IReadOnlyList<LetSyntax> TopLevelLets { get; } = topLevelLets;

    /// <summary>
    /// Gets the <c>let</c> assignments applied to the anonymous overlay instance.
    /// </summary>
    public IReadOnlyList<LetSyntax> BodyLets { get; } = bodyLets;
}
