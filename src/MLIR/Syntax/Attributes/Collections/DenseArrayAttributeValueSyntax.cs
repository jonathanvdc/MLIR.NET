namespace MLIR.Syntax.Attributes.Collections;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a dense array attribute value such as <c>array&lt;i32: 1, 2&gt;</c>.
/// </summary>
public sealed class DenseArrayAttributeValueSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    TypeSyntax elementTypeSyntax,
    SyntaxToken colonToken,
    SeparatedSyntaxList<AttributeValueSyntax> items,
    SyntaxToken greaterThanToken) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the leading keyword token.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

    /// <summary>
    /// Gets the opening angle token.
    /// </summary>
    public SyntaxToken LessThanToken { get; } = lessThanToken;

    /// <summary>
    /// Gets the element type syntax.
    /// </summary>
    public TypeSyntax ElementTypeSyntax { get; } = elementTypeSyntax;

    /// <summary>
    /// Gets the colon token that separates the type from the values.
    /// </summary>
    public SyntaxToken ColonToken { get; } = colonToken;

    /// <summary>
    /// Gets the dense array items.
    /// </summary>
    public SeparatedSyntaxList<AttributeValueSyntax> Items { get; } = items;

    /// <summary>
    /// Gets the closing angle token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    /// <inheritdoc/>
    public override SourceLocation Location => KeywordToken.HasSourceLocation
        ? new SourceLocation(KeywordToken.Line, KeywordToken.Column)
        : SourceLocation.Unknown;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        var text = KeywordToken.Text + LessThanToken.Text + ElementTypeSyntax.ToString() + ColonToken.Text;
        for (var i = 0; i < Items.Count; i++)
        {
            text += i == 0 ? " " : string.Empty;
            text += Items[i].ToString();
            if (i < Items.SeparatorTokens.Count)
            {
                text += Items.SeparatorTokens[i].Text + " ";
            }
        }

        rawText = new RawSyntaxText(text + GreaterThanToken.Text);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        ElementTypeSyntax.WriteTo(writer, string.Empty);
        writer.WriteToken(ColonToken, string.Empty);
        writer.WriteSeparatedList(Items, " ");
        writer.WriteToken(GreaterThanToken, string.Empty);
    }
}
