namespace MLIR.Syntax.Attributes;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a unit attribute literal.
/// </summary>
public sealed class UnitAttributeValueSyntax(Token keywordToken) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public Token KeywordToken { get; } = keywordToken;

    /// <inheritdoc/>
    public override SourceLocation Location => KeywordToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new UnitAttributeValueSyntax(rewriter.VisitToken(KeywordToken));
    }
}
