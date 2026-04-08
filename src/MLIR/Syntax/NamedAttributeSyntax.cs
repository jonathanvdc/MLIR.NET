namespace MLIR.Syntax;

using MLIR.Semantics;
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
        : this(SyntaxTokenFactory.Identifier(name), SyntaxTokenFactory.Equal(), new RawAttributeValueSyntax(value))
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
    /// Gets the merged source location spanning from the attribute name to the end of the value.
    /// Returns an unknown location when neither the name token nor the value has source information.
    /// </summary>
    public override SourceLocation Location =>
        SourceLocation.Merge(NameToken.Location, ValueSyntax.Location);

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(NameToken);
        writer.WriteToken(EqualsToken, " ");
        writer.SuggestTrivia(" ");
        ValueSyntax.WriteTo(writer);
    }
}
