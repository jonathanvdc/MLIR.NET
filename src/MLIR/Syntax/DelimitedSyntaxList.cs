namespace MLIR.Syntax;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLIR.Semantics;

/// <summary>
/// Represents a delimited separated list of syntax items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="DelimitedSyntaxList{T}"/> class.
/// </remarks>
/// <param name="openToken">The opening delimiter token.</param>
/// <param name="items">The list items.</param>
/// <param name="separatorTokens">The separator tokens between items.</param>
/// <param name="closeToken">The closing delimiter token.</param>
public sealed class DelimitedSyntaxList<T>(
    Token? openToken,
    IReadOnlyList<T> items,
    IReadOnlyList<Token> separatorTokens,
    Token? closeToken) : IReadOnlyList<T>, IHasSourceLocation
    where T : IHasSourceLocation
{
    /// <summary>
    /// Gets an empty list instance with no opening or closing delimiters.
    /// </summary>
    public static DelimitedSyntaxList<T> Empty { get; } = new(null, Array.Empty<T>(), Array.Empty<Token>(), null);

    /// <summary>
    /// Gets the opening delimiter token.
    /// </summary>
    public Token? OpenToken { get; } = openToken;

    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<T> Items { get; } = items;

    /// <summary>
    /// Gets the separator tokens between items.
    /// </summary>
    public IReadOnlyList<Token> SeparatorTokens { get; } = separatorTokens;

    /// <summary>
    /// Gets the closing delimiter token.
    /// </summary>
    public Token? CloseToken { get; } = closeToken;

    /// <summary>
    /// Gets the source location of this list. If both opening and closing delimiter tokens are present
    /// returns the merged location of both tokens. If only the opening delimiter token is present, returns its location.
    /// Otherwise, returns <c>SourceLocation.Unknown</c>.
    /// </summary>
    public SourceLocation Location
    {
        get
        {
            if (OpenToken.HasValue && CloseToken.HasValue) return SourceLocation.Merge(OpenToken.Value.Location, CloseToken.Value.Location);
            else if (OpenToken.HasValue) return OpenToken.Value.Location;
            else return SourceLocation.Unknown;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this list is present in the source, i.e., has an opening delimiter token.
    /// </summary>
    public bool IsPresent => OpenToken.HasValue;

    /// <summary>
    /// Gets the number of items in the list.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="index">The item index.</param>
    public T this[int index] => Items[index];

    /// <summary>
    /// Writes this list to the supplied syntax writer if an opening delimiter token is present.
    /// Writes the opening delimiter (using the current pending suggested trivia), then each element
    /// (interleaved with separator tokens, with suggested trivia set before each element), then the
    /// closing delimiter. When <see cref="IsPresent"/> is <see langword="false"/> this method
    /// does nothing.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="writeElement">A delegate that writes a single element to the writer.</param>
    public void WriteTo(
        Text.SyntaxWriter writer,
        System.Action<T, Text.SyntaxWriter> writeElement)
    {
        if (!OpenToken.HasValue)
        {
            return;
        }

        writer.WriteToken(OpenToken.Value);
        for (var i = 0; i < Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(SeparatorTokens[i - 1]);
                writer.SuggestTrivia(" ");
            }

            writeElement(Items[i], writer);
        }

        writer.WriteToken(CloseToken!.Value);
    }

    /// <summary>
    /// Writes this list to the supplied syntax writer if an opening delimiter token is present.
    /// Writes the opening delimiter (using <paramref name="openLeadingTrivia"/> as explicit trivia),
    /// then each element (interleaved with separator tokens, with suggested trivia set before each
    /// element), then the closing delimiter. When <see cref="IsPresent"/> is <see langword="false"/>
    /// this method does nothing.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="openLeadingTrivia">The explicit leading trivia for the opening delimiter token.</param>
    /// <param name="writeElement">A delegate that writes a single element to the writer.</param>
    public void WriteTo(
        Text.SyntaxWriter writer,
        string openLeadingTrivia,
        System.Action<T, Text.SyntaxWriter> writeElement)
    {
        if (!OpenToken.HasValue)
        {
            return;
        }

        writer.SuggestTrivia(openLeadingTrivia);
        WriteTo(writer, writeElement);
    }

    /// <summary>
    /// Rewrites this list by applying the supplied delegate to each element and separator token, and optionally to the opening and closing delimiter tokens.
    /// </summary>
    /// <param name="rewriteElement">A delegate that rewrites a single element.</param>
    /// <param name="rewriteOpenToken">A delegate that rewrites the opening delimiter token.</param>
    /// <param name="rewriteSeparatorToken">A delegate that rewrites a separator token.</param>
    /// <param name="rewriteCloseToken">A delegate that rewrites the closing delimiter token.</param>
    /// <returns>The rewritten list.</returns>
    public DelimitedSyntaxList<T> Rewrite(
        Func<T, T> rewriteElement,
        Func<Token, Token> rewriteOpenToken,
        Func<Token, Token> rewriteSeparatorToken,
        Func<Token, Token> rewriteCloseToken)
    {
        var rewrittenOpenToken = OpenToken.HasValue ? rewriteOpenToken(OpenToken.Value) : default;
        var rewrittenCloseToken = CloseToken.HasValue ? rewriteCloseToken(CloseToken.Value) : default;

        var changed = !Equals(rewrittenOpenToken, OpenToken) || !Equals(rewrittenCloseToken, CloseToken);

        T[]? rewrittenItems = null;
        for (var i = 0; i < Items.Count; i++)
        {
            var originalItem = Items[i];
            var rewrittenItem = rewriteElement(originalItem);

            if (!changed)
            {
                if (ReferenceEquals(rewrittenItem, originalItem))
                {
                    continue;
                }

                changed = true;
                rewrittenItems = new T[Items.Count];
                for (var j = 0; j < i; j++)
                {
                    rewrittenItems[j] = Items[j];
                }
            }

            rewrittenItems![i] = rewrittenItem;
        }

        Token[]? rewrittenSeparatorTokens = null;
        for (var i = 0; i < SeparatorTokens.Count; i++)
        {
            var originalSeparatorToken = SeparatorTokens[i];
            var rewrittenSeparatorToken = rewriteSeparatorToken(originalSeparatorToken);

            if (!changed)
            {
                if (Equals(rewrittenSeparatorToken, originalSeparatorToken))
                {
                    continue;
                }

                changed = true;
                rewrittenSeparatorTokens = new Token[SeparatorTokens.Count];
                for (var j = 0; j < i; j++)
                {
                    rewrittenSeparatorTokens[j] = SeparatorTokens[j];
                }
            }

            rewrittenSeparatorTokens ??= new Token[SeparatorTokens.Count];
            rewrittenSeparatorTokens[i] = rewrittenSeparatorToken;
        }

        if (!changed)
        {
            return this;
        }

        rewrittenItems ??= Items.Count == 0 ? Array.Empty<T>() : Items.ToArray();
        rewrittenSeparatorTokens ??= SeparatorTokens.Count == 0 ? Array.Empty<Token>() : SeparatorTokens.ToArray();
        return new DelimitedSyntaxList<T>(rewrittenOpenToken, rewrittenItems, rewrittenSeparatorTokens, rewrittenCloseToken);
    }

    /// <summary>
    /// Returns an enumerator over the list items.
    /// </summary>
    /// <returns>An enumerator over the list items.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
