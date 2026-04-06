namespace MLIR.Syntax.Attributes.Primitives;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a primitive boolean attribute literal.
/// </summary>
public sealed class BooleanAttributeValueSyntax : AttributeValueSyntax
{
    private readonly RawSyntaxText rawText;

    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanAttributeValueSyntax"/> class.
    /// </summary>
    public BooleanAttributeValueSyntax(SyntaxToken literalToken, bool value)
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
    /// Gets the parsed boolean value.
    /// </summary>
    public bool Value { get; }

    /// <inheritdoc/>
    public override SourceLocation Location => LiteralToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(LiteralToken);
    }
}
