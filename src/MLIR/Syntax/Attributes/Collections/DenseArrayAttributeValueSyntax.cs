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
    public override SourceLocation Location =>
        SourceLocation.Merge(KeywordToken.Location, GreaterThanToken.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
        writer.WriteToken(LessThanToken);
        ElementTypeSyntax.WriteTo(writer);
        writer.WriteToken(ColonToken);
        writer.WriteSeparatedList(Items, " ");
        writer.WriteToken(GreaterThanToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new DenseArrayAttributeValueSyntax(
            rewriter.VisitToken(KeywordToken),
            rewriter.VisitToken(LessThanToken),
            (TypeSyntax)rewriter.Visit(ElementTypeSyntax),
            rewriter.VisitToken(ColonToken),
            rewriter.VisitSeparatedList(Items),
            rewriter.VisitToken(GreaterThanToken));
    }
}
