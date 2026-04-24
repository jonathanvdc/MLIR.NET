using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

using MLIR.Text;

/// <summary>
/// Represents a tuple type such as <c>tuple&lt;i32, f32&gt;</c>.
/// </summary>
public sealed class TupleTypeSyntax(
    Token keywordToken,
    Token lessThanToken,
    IReadOnlyList<TypeSyntax> elements,
    IReadOnlyList<Token> commaTokens,
    Token greaterThanToken) : TypeSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public Token KeywordToken { get; } = keywordToken;

    /// <summary>
    /// Gets the opening angle-bracket token.
    /// </summary>
    public Token LessThanToken { get; } = lessThanToken;

    /// <summary>
    /// Gets the tuple element types.
    /// </summary>
    public IReadOnlyList<TypeSyntax> Elements { get; } = elements;

    /// <summary>
    /// Gets the separator tokens between tuple elements.
    /// </summary>
    public IReadOnlyList<Token> CommaTokens { get; } = commaTokens;

    /// <summary>
    /// Gets the closing angle-bracket token.
    /// </summary>
    public Token GreaterThanToken { get; } = greaterThanToken;

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(KeywordToken.Location, GreaterThanToken.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
        writer.WriteToken(LessThanToken);
        WriteSeparatedTypes(writer, Elements, CommaTokens);
        writer.WriteToken(GreaterThanToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new TupleTypeSyntax(
            rewriter.VisitToken(KeywordToken),
            rewriter.VisitToken(LessThanToken),
            rewriter.VisitList(Elements),
            rewriter.VisitTokenList(CommaTokens),
            rewriter.VisitToken(GreaterThanToken));
    }

    private static IEnumerable<object> Interleave(IReadOnlyList<TypeSyntax> items, IReadOnlyList<Token> separators)
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

    private static void WriteSeparatedTypes(Text.SyntaxWriter writer, IReadOnlyList<TypeSyntax> items, IReadOnlyList<Token> separators)
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
