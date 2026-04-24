using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

using MLIR.Text;

/// <summary>
/// Represents a builtin vector type.
/// </summary>
public sealed class VectorTypeSyntax(
    Token keywordToken,
    Token lessThanToken,
    IReadOnlyList<ShapedTypeDimensionSyntax> dimensions,
    IReadOnlyList<Token> xTokens,
    TypeSyntax elementType,
    Token greaterThanToken) : TypeSyntax
{
    /// <summary>Gets the keyword token.</summary>
    public Token KeywordToken { get; } = keywordToken;
    /// <summary>Gets the opening angle-bracket token.</summary>
    public Token LessThanToken { get; } = lessThanToken;
    /// <summary>Gets the ranked dimensions.</summary>
    public IReadOnlyList<ShapedTypeDimensionSyntax> Dimensions { get; } = dimensions;
    /// <summary>Gets the preserved <c>x</c> separators.</summary>
    public IReadOnlyList<Token> XTokens { get; } = xTokens;
    /// <summary>Gets the element type.</summary>
    public TypeSyntax ElementType { get; } = elementType;
    /// <summary>Gets the closing angle-bracket token.</summary>
    public Token GreaterThanToken { get; } = greaterThanToken;

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(KeywordToken.Location, GreaterThanToken.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
        writer.WriteToken(LessThanToken);
        for (var i = 0; i < Dimensions.Count; i++)
        {
            Dimensions[i].WriteTo(writer);
            writer.WriteToken(XTokens[i]);
        }

        ElementType.WriteTo(writer);
        writer.WriteToken(GreaterThanToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new VectorTypeSyntax(
            rewriter.VisitToken(KeywordToken),
            rewriter.VisitToken(LessThanToken),
            rewriter.VisitList(Dimensions),
            rewriter.VisitTokenList(XTokens),
            (TypeSyntax)rewriter.Visit(ElementType),
            rewriter.VisitToken(GreaterThanToken));
    }
}
