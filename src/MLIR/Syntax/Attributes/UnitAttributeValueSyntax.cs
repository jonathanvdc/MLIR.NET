namespace MLIR.Syntax.Attributes;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a unit attribute literal.
/// </summary>
public sealed class UnitAttributeValueSyntax(SyntaxToken keywordToken) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

    /// <inheritdoc/>
    public override SourceLocation Location => KeywordToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
    }
}
