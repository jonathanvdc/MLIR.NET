namespace MLIR.Syntax.Attributes.Collections;

using MLIR.Syntax;

/// <summary>
/// Represents a dense elements attribute value.
/// </summary>
public sealed class ElementsAttributeValueSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    AttributeValueSyntax payload,
    SyntaxToken greaterThanToken,
    SyntaxToken colonToken,
    TypeSyntax typeSyntax) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the leading keyword token, such as <c>dense</c>.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

    /// <summary>
    /// Gets the opening angle token.
    /// </summary>
    public SyntaxToken LessThanToken { get; } = lessThanToken;

    /// <summary>
    /// Gets the payload syntax.
    /// </summary>
    public AttributeValueSyntax Payload { get; } = payload;

    /// <summary>
    /// Gets the closing angle token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    /// <summary>
    /// Gets the colon token.
    /// </summary>
    public SyntaxToken ColonToken { get; } = colonToken;

    /// <summary>
    /// Gets the trailing type syntax.
    /// </summary>
    public TypeSyntax TypeSyntax { get; } = typeSyntax;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText(
            KeywordToken.Text + LessThanToken.Text + Payload.GetRawText().Text + GreaterThanToken.Text + " " + ColonToken.Text + " " + TypeSyntax.GetRawText().Text);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        Payload.WriteTo(writer, string.Empty);
        writer.WriteToken(GreaterThanToken, string.Empty);
        writer.WriteToken(ColonToken, " ");
        TypeSyntax.WriteTo(writer, " ");
    }
}
