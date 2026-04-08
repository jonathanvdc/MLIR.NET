using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a builtin tensor type.
/// </summary>
public sealed class TensorTypeSyntax(
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
    /// <summary>Gets trailing parameters such as encoding attributes.</summary>
    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; } = trailingParameters;
    /// <summary>Gets the closing angle-bracket token.</summary>
    public Token GreaterThanToken { get; } = greaterThanToken;

    /// <summary>
    /// Gets a value indicating whether the tensor is unranked.
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
        WriteShapedPrefix(writer);
        ElementType.WriteTo(writer);
        WriteTrailing(writer);
        writer.WriteToken(GreaterThanToken);
    }

    private void AppendShapedPrefix(List<object?> parts)
    {
        if (IsUnranked)
        {
            parts.Add(UnrankedToken);
            parts.Add(XTokens[0]);
            return;
        }

        for (var i = 0; i < Dimensions.Count; i++)
        {
            parts.Add(Dimensions[i]);
            parts.Add(XTokens[i]);
        }
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new TensorTypeSyntax(
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

    private void WriteShapedPrefix(Text.SyntaxWriter writer)
    {
        if (IsUnranked)
        {
            writer.WriteToken(UnrankedToken!.Value);
            writer.WriteToken(XTokens[0]);
            return;
        }

        for (var i = 0; i < Dimensions.Count; i++)
        {
            Dimensions[i].WriteTo(writer);
            writer.WriteToken(XTokens[i]);
        }
    }

    private void AppendTrailing(List<object?> parts)
    {
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            parts.Add(TrailingCommaTokens[i]);
            parts.Add(TrailingParameters[i]);
        }
    }

    private void WriteTrailing(Text.SyntaxWriter writer)
    {
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            writer.WriteToken(TrailingCommaTokens[i]);
            writer.WriteRaw(TrailingParameters[i], " ");
        }
    }
}
