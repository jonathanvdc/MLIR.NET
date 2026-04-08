namespace MLIR.Syntax.Attributes.Collections;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a dense elements attribute value.
/// </summary>
public sealed class ElementsAttributeValueSyntax(
    Token keywordToken,
    Token lessThanToken,
    AttributeValueSyntax payload,
    Token greaterThanToken,
    Token colonToken,
    TypeSyntax typeSyntax) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the leading keyword token, such as <c>dense</c>.
    /// </summary>
    public Token KeywordToken { get; } = keywordToken;

    /// <summary>
    /// Gets the opening angle token.
    /// </summary>
    public Token LessThanToken { get; } = lessThanToken;

    /// <summary>
    /// Gets the payload syntax.
    /// </summary>
    public AttributeValueSyntax Payload { get; } = payload;

    /// <summary>
    /// Gets the closing angle token.
    /// </summary>
    public Token GreaterThanToken { get; } = greaterThanToken;

    /// <summary>
    /// Gets the colon token.
    /// </summary>
    public Token ColonToken { get; } = colonToken;

    /// <summary>
    /// Gets the trailing type syntax.
    /// </summary>
    public TypeSyntax TypeSyntax { get; } = typeSyntax;

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(KeywordToken.Location, TypeSyntax.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
        writer.WriteToken(LessThanToken);
        Payload.WriteTo(writer);
        writer.WriteToken(GreaterThanToken);
        writer.WriteToken(ColonToken, " ");
        writer.SuggestTrivia(" ");
        TypeSyntax.WriteTo(writer);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new ElementsAttributeValueSyntax(
            rewriter.VisitToken(KeywordToken),
            rewriter.VisitToken(LessThanToken),
            (AttributeValueSyntax)rewriter.Visit(Payload),
            rewriter.VisitToken(GreaterThanToken),
            rewriter.VisitToken(ColonToken),
            (TypeSyntax)rewriter.Visit(TypeSyntax));
    }
}
