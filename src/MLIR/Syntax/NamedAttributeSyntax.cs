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
public sealed class NamedAttributeSyntax(SyntaxToken nameToken, SyntaxToken equalsToken, AttributeValueSyntax valueSyntax) : SyntaxNode
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

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(NameToken);
        writer.WriteToken(EqualsToken, " ");
        writer.SuggestTrivia(" ");
        ValueSyntax.WriteTo(writer);
    }
}
