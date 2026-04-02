namespace MLIR.Syntax.Attributes;

using MLIR.Syntax;

/// <summary>
/// Represents a unit attribute literal.
/// </summary>
public sealed class UnitAttributeValueSyntax(SyntaxToken keywordToken) : AttributeValueSyntax
{
    private readonly RawSyntaxText rawText = new RawSyntaxText([keywordToken]);

    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = this.rawText;
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
    }
}
