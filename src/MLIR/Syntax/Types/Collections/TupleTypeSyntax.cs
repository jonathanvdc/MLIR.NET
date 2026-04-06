using MLIR.Semantics;

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
    public override SourceLocation Location => KeywordToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
        writer.WriteToken(LessThanToken);
        WriteSeparatedTypes(writer, Elements, CommaTokens);
        writer.WriteToken(GreaterThanToken);
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
                writer.WriteToken(separators[i - 1]);
                writer.SuggestTrivia(" ");
            }

            items[i].WriteTo(writer);
        }
    }
}
