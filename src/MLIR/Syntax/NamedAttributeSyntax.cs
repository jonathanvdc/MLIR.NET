namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a named attribute entry in an operation attribute dictionary.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NamedAttributeSyntax"/> class.
/// </remarks>
/// <param name="nameToken">The attribute name token.</param>
/// <param name="equalsToken">The equals token.</param>
/// <param name="valueSyntax">The attribute value syntax.</param>
public sealed class NamedAttributeSyntax(SyntaxToken nameToken, SyntaxToken equalsToken, AttributeValueSyntax valueSyntax)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedAttributeSyntax"/> class.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The raw attribute value text.</param>
    public NamedAttributeSyntax(string name, RawSyntaxText value)
        : this(new SyntaxToken(name), new SyntaxToken("="), new RawAttributeValueSyntax(value))
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
    /// Gets the attribute value syntax.
    /// </summary>
    public AttributeValueSyntax ValueSyntax { get; } = valueSyntax;

    /// <summary>
    /// Attempts to get the attribute value as raw syntax text.
    /// </summary>
    public bool TryGetRawValue(out RawSyntaxText? rawValue)
    {
        return ValueSyntax.TryGetRawText(out rawValue);
    }

    /// <summary>
    /// Gets the attribute value as raw syntax text.
    /// </summary>
    public RawSyntaxText RawValue => ValueSyntax.GetRawText();

    /// <summary>
    /// Writes the named attribute to the supplied syntax writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia for the name token.</param>
    public void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(NameToken, defaultLeadingTrivia);
        writer.WriteToken(EqualsToken, " ");
        ValueSyntax.WriteTo(writer, " ");
    }
}
