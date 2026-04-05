namespace MLIR.Syntax.Attributes.Primitives;

using MLIR.Syntax;

/// <summary>
/// Represents a primitive string attribute literal.
/// </summary>
public sealed class StringAttributeValueSyntax : AttributeValueSyntax
{
    private readonly RawSyntaxText rawText;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringAttributeValueSyntax"/> class.
    /// </summary>
    public StringAttributeValueSyntax(SyntaxToken literalToken, string value)
    {
        LiteralToken = literalToken;
        Value = value;
        rawText = new RawSyntaxText([literalToken]);
    }

    /// <summary>
    /// Gets the literal token.
    /// </summary>
    public SyntaxToken LiteralToken { get; }

    /// <summary>
    /// Gets the unescaped string value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = this.rawText;
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(LiteralToken);
    }
}
