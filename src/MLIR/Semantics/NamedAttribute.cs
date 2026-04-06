namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a semantic attribute attached to an operation.
/// </summary>
public sealed class NamedAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedAttribute"/> class.
    /// </summary>
    /// <param name="syntax">The concrete syntax node for the attribute.</param>
    /// <param name="value">The semantic attribute value.</param>
    public NamedAttribute(NamedAttributeSyntax syntax, AttributeValue value)
    {
        Syntax = syntax;
        Name = syntax.NameToken.Text;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedAttribute"/> class with no syntax information.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The semantic attribute value.</param>
    public NamedAttribute(string name, AttributeValue value)
    {
        Syntax = null;
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the concrete syntax node for the attribute, if it exists; otherwise, null if the attribute was synthesized without syntax information.
    /// </summary>
    /// <remarks>
    /// The syntax node provides access to the original source text and location of the attribute, which can be useful for diagnostics and
    /// transformations that need to preserve or analyze the original syntax.
    /// Synthetic attributes created without syntax information have a null syntax node, and their source location is considered unknown.
    /// </remarks>
    public NamedAttributeSyntax? Syntax { get; }

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the semantic attribute value bound from the raw text.
    /// </summary>
    public AttributeValue Value { get; }

    /// <summary>
    /// Gets the source location of the attribute name, if known.
    /// </summary>
    public SourceLocation Location => Syntax != null ? Syntax.NameToken.Location : SourceLocation.Unknown;
}
