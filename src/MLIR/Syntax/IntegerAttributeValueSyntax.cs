namespace MLIR.Syntax;

using System.Numerics;

/// <summary>
/// Represents a primitive integer attribute literal.
/// </summary>
public sealed class IntegerAttributeValueSyntax : AttributeValueSyntax
{
    private readonly RawSyntaxText rawText;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerAttributeValueSyntax"/> class.
    /// </summary>
    /// <param name="literalToken">The source token for the integer literal.</param>
    /// <param name="value">The parsed integer value.</param>
    public IntegerAttributeValueSyntax(SyntaxToken literalToken, BigInteger value)
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
    /// Gets the parsed integer value.
    /// </summary>
    public BigInteger Value { get; }

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = this.rawText;
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(LiteralToken, defaultLeadingTrivia);
    }
}
