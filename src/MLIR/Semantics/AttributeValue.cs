namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic attribute value bound from raw syntax text.
/// </summary>
public abstract class AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeValue"/> class.
    /// </summary>
    protected AttributeValue(RawSyntaxText? syntax, string? name, AttributeDefinition? definition, SourceLocation location)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Location = location;
    }

    /// <summary>
    /// Gets the raw syntax text for the attribute value, or null if this is a synthetic attribute value with no corresponding source text.
    /// </summary>
    public RawSyntaxText? Syntax { get; }

    /// <summary>
    /// Gets the canonical attribute name, if one was recognized.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the registered definition, if one exists.
    /// </summary>
    public AttributeDefinition? Definition { get; }

    /// <summary>
    /// Gets a value indicating whether the attribute value was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

    /// <summary>
    /// Gets the source location of the attribute value, if known.
    /// </summary>
    public SourceLocation Location { get; }
}
