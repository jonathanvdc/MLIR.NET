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

/// <summary>
/// Represents the self-identifying builtin unit attribute syntax <c>#builtin.unit</c>.
/// </summary>
public sealed class PrefixedUnitAttributeValueSyntax(DialectAttributePrefix prefix)
    : DialectPrefixedAttributeValueSyntax(prefix)
{
    /// <inheritdoc/>
    public override SourceLocation Location => SourceLocation.Merge(Prefix.HashToken.Location, Prefix.NameToken.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        WritePrefix(writer);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new PrefixedUnitAttributeValueSyntax(
            new DialectAttributePrefix(
                rewriter.VisitToken(Prefix.HashToken),
                rewriter.VisitToken(Prefix.NameToken)));
    }
}
