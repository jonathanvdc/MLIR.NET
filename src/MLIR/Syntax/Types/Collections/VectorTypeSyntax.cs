namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a builtin vector type.
/// </summary>
public sealed class VectorTypeSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    IReadOnlyList<ShapedTypeDimensionSyntax> dimensions,
    IReadOnlyList<SyntaxToken> xTokens,
    TypeSyntax elementType,
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
    /// <summary>Gets the element type.</summary>
    public TypeSyntax ElementType { get; } = elementType;
    /// <summary>Gets the closing angle-bracket token.</summary>
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        var parts = new List<object?> { KeywordToken, LessThanToken };
        for (var i = 0; i < Dimensions.Count; i++)
        {
            parts.Add(Dimensions[i]);
            parts.Add(XTokens[i]);
        }

        parts.Add(ElementType);
        parts.Add(GreaterThanToken);
        rawText = SyntaxTextComposer.Compose(parts.ToArray());
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        for (var i = 0; i < Dimensions.Count; i++)
        {
            Dimensions[i].WriteTo(writer, string.Empty);
            writer.WriteToken(XTokens[i], string.Empty);
        }

        ElementType.WriteTo(writer, string.Empty);
        writer.WriteToken(GreaterThanToken, string.Empty);
    }
}
