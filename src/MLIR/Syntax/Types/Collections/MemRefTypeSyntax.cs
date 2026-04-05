namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a builtin memref type.
/// </summary>
public sealed class MemRefTypeSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    IReadOnlyList<ShapedTypeDimensionSyntax> dimensions,
    IReadOnlyList<SyntaxToken> xTokens,
    SyntaxToken? unrankedToken,
    TypeSyntax elementType,
    IReadOnlyList<SyntaxToken> trailingCommaTokens,
    IReadOnlyList<RawSyntaxText> trailingParameters,
    SyntaxToken greaterThanToken) : TypeSyntax
{
    /// <summary>Gets the keyword token.</summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;
    /// <summary>Gets the opening angle-bracket token.</summary>
    public SyntaxToken LessThanToken { get; } = lessThanToken;
    /// <summary>Gets the ranked dimensions.</summary>
    public IReadOnlyList<ShapedTypeDimensionSyntax> Dimensions { get; } = dimensions;
    /// <summary>Gets the preserved <c>x</c> separators.</summary>
    public IReadOnlyList<SyntaxToken> XTokens { get; } = xTokens;
    /// <summary>Gets the unranked marker when present.</summary>
    public SyntaxToken? UnrankedToken { get; } = unrankedToken;
    /// <summary>Gets the element type.</summary>
    public TypeSyntax ElementType { get; } = elementType;
    /// <summary>Gets comma tokens for trailing parameters.</summary>
    public IReadOnlyList<SyntaxToken> TrailingCommaTokens { get; } = trailingCommaTokens;
    /// <summary>Gets trailing parameters such as layouts and memory spaces.</summary>
    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; } = trailingParameters;
    /// <summary>Gets the closing angle-bracket token.</summary>
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    /// <summary>
    /// Gets a value indicating whether the memref is unranked.
    /// </summary>
    public bool IsUnranked => UnrankedToken.HasValue;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        var parts = new List<object?> { KeywordToken, LessThanToken };
        if (IsUnranked)
        {
            parts.Add(UnrankedToken);
            parts.Add(XTokens[0]);
        }
        else
        {
            for (var i = 0; i < Dimensions.Count; i++)
            {
                parts.Add(Dimensions[i]);
                parts.Add(XTokens[i]);
            }
        }

        parts.Add(ElementType);
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            parts.Add(TrailingCommaTokens[i]);
            parts.Add(TrailingParameters[i]);
        }

        parts.Add(GreaterThanToken);
        rawText = SyntaxTextComposer.Compose(parts.ToArray());
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
        writer.WriteToken(LessThanToken, string.Empty);
        if (IsUnranked)
        {
            writer.WriteToken(UnrankedToken!.Value, string.Empty);
            writer.WriteToken(XTokens[0], string.Empty);
        }
        else
        {
            for (var i = 0; i < Dimensions.Count; i++)
            {
                Dimensions[i].WriteTo(writer);
                writer.WriteToken(XTokens[i], string.Empty);
            }
        }

        ElementType.WriteTo(writer);
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            writer.WriteToken(TrailingCommaTokens[i], string.Empty);
            writer.WriteRaw(TrailingParameters[i], " ");
        }

        writer.WriteToken(GreaterThanToken, string.Empty);
    }
}
