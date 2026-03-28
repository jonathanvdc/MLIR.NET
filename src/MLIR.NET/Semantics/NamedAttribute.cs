namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a semantic attribute attached to an operation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NamedAttribute"/> class.
/// </remarks>
/// <param name="syntax">The concrete syntax node for the attribute.</param>
public sealed class NamedAttribute(NamedAttributeSyntax syntax)
{
    /// <summary>
    /// Gets the concrete syntax node for the attribute.
    /// </summary>
    public NamedAttributeSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string Name => Syntax.Name;

    /// <summary>
    /// Gets the raw attribute value text.
    /// </summary>
    public RawSyntaxText Value => Syntax.Value;

    /// <summary>
    /// Gets the source location of the attribute name, if known.
    /// </summary>
    public SourceLocation Location => SourceLocation.FromToken(Syntax.NameToken);
}
