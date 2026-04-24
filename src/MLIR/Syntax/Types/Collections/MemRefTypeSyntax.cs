using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

using MLIR.Text;

/// <summary>
/// Represents a builtin memref type.
/// </summary>
public sealed class MemRefTypeSyntax(
    Token keywordToken,
    Token lessThanToken,
    IReadOnlyList<ShapedTypeDimensionSyntax> dimensions,
    IReadOnlyList<Token> xTokens,
    Token? unrankedToken,
    TypeSyntax elementType,
    IReadOnlyList<Token> trailingCommaTokens,
    IReadOnlyList<RawSyntaxText> trailingParameters,
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
    /// <summary>Gets the unranked marker when present.</summary>
    public Token? UnrankedToken { get; } = unrankedToken;
    /// <summary>Gets the element type.</summary>
    public TypeSyntax ElementType { get; } = elementType;
    /// <summary>Gets comma tokens for trailing parameters.</summary>
    public IReadOnlyList<Token> TrailingCommaTokens { get; } = trailingCommaTokens;
    /// <summary>Gets trailing parameters such as layouts and memory spaces.</summary>
    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; } = trailingParameters;
    /// <summary>Gets the closing angle-bracket token.</summary>
    public Token GreaterThanToken { get; } = greaterThanToken;

    /// <summary>
    /// Gets a value indicating whether the memref is unranked.
    /// </summary>
    public bool IsUnranked => UnrankedToken.HasValue;

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(KeywordToken.Location, GreaterThanToken.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
        writer.WriteToken(LessThanToken);
        if (IsUnranked)
        {
            writer.WriteToken(UnrankedToken!.Value);
            writer.WriteToken(XTokens[0]);
        }
        else
        {
            for (var i = 0; i < Dimensions.Count; i++)
            {
                Dimensions[i].WriteTo(writer);
                writer.WriteToken(XTokens[i]);
            }
        }

        ElementType.WriteTo(writer);
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            writer.WriteToken(TrailingCommaTokens[i]);
            writer.WriteRaw(TrailingParameters[i], " ");
        }

        writer.WriteToken(GreaterThanToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new MemRefTypeSyntax(
            rewriter.VisitToken(KeywordToken),
            rewriter.VisitToken(LessThanToken),
            rewriter.VisitList(Dimensions),
            rewriter.VisitTokenList(XTokens),
            rewriter.VisitToken(UnrankedToken),
            (TypeSyntax)rewriter.Visit(ElementType),
            rewriter.VisitTokenList(TrailingCommaTokens),
            rewriter.VisitRawTextList(TrailingParameters),
            rewriter.VisitToken(GreaterThanToken));
    }
}
