namespace MLIR.Syntax.Attributes.Primitives;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a primitive string attribute literal.
/// </summary>
public sealed class StringAttributeValueSyntax : AttributeValueSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringAttributeValueSyntax"/> class.
    /// </summary>
    public StringAttributeValueSyntax(SyntaxToken literalToken, string value)
    {
        LiteralToken = literalToken;
        Value = value;
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
    public override SourceLocation Location => LiteralToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(LiteralToken);
    }
}
