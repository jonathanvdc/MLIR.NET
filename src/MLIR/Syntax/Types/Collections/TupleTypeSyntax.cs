namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a tuple type such as <c>tuple&lt;i32, f32&gt;</c>.
/// </summary>
public sealed class TupleTypeSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    IReadOnlyList<TypeSyntax> elements,
    IReadOnlyList<SyntaxToken> commaTokens,
    SyntaxToken greaterThanToken) : TypeSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

    /// <summary>
    /// Gets the opening angle-bracket token.
    /// </summary>
    public SyntaxToken LessThanToken { get; } = lessThanToken;

    /// <summary>
    /// Gets the tuple element types.
    /// </summary>
    public IReadOnlyList<TypeSyntax> Elements { get; } = elements;

    /// <summary>
    /// Gets the separator tokens between tuple elements.
    /// </summary>
    public IReadOnlyList<SyntaxToken> CommaTokens { get; } = commaTokens;

    /// <summary>
    /// Gets the closing angle-bracket token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = SyntaxTextComposer.Compose(KeywordToken, LessThanToken, Interleave(Elements, CommaTokens), GreaterThanToken);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        WriteSeparatedTypes(writer, Elements, CommaTokens);
        writer.WriteToken(GreaterThanToken, string.Empty);
    }

    private static IEnumerable<object> Interleave(IReadOnlyList<TypeSyntax> items, IReadOnlyList<SyntaxToken> separators)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                yield return separators[i - 1];
            }

            yield return items[i];
        }
    }

    private static void WriteSeparatedTypes(Text.SyntaxWriter writer, IReadOnlyList<TypeSyntax> items, IReadOnlyList<SyntaxToken> separators)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(separators[i - 1], string.Empty);
            }

            items[i].WriteTo(writer, i > 0 ? " " : string.Empty);
        }
    }
}
