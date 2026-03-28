namespace MLIR.Syntax;

/// <summary>
/// Represents a named attribute entry in an operation attribute dictionary.
/// </summary>
public sealed class NamedAttributeSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedAttributeSyntax"/> class.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The raw attribute value text.</param>
    public NamedAttributeSyntax(string name, RawSyntaxText value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the raw attribute value text.
    /// </summary>
    public RawSyntaxText Value { get; }
}
