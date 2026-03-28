namespace MLIR.Syntax;

/// <summary>
/// Represents a named attribute entry in an operation attribute dictionary.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NamedAttributeSyntax"/> class.
/// </remarks>
/// <param name="nameToken">The attribute name token.</param>
/// <param name="equalsToken">The equals token.</param>
/// <param name="value">The raw attribute value text.</param>
public sealed class NamedAttributeSyntax(SyntaxToken nameToken, SyntaxToken equalsToken, RawSyntaxText value)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedAttributeSyntax"/> class.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The raw attribute value text.</param>
    public NamedAttributeSyntax(string name, RawSyntaxText value)
        : this(new SyntaxToken(name), new SyntaxToken("="), value)
    {
    }

    /// <summary>
    /// Gets the attribute name token.
    /// </summary>
    public SyntaxToken NameToken { get; } = nameToken;

    /// <summary>
    /// Gets the equals token.
    /// </summary>
    public SyntaxToken EqualsToken { get; } = equalsToken;

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string Name => NameToken.Text;

    /// <summary>
    /// Gets the raw attribute value text.
    /// </summary>
    public RawSyntaxText Value { get; } = value;
}
