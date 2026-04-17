namespace MLIR.Syntax;

using System.Collections;
using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Represents a token-separated list of syntax items without opening and closing delimiter tokens.
/// Unlike <see cref="DelimitedSyntaxList{T}"/>, this type does not carry surrounding bracket,
/// parenthesis, or other delimiter tokens. Use it for bare comma-separated constructs such as
/// <c>%a, %b, %c</c> or <c>1, 2, 3</c>.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SeparatedSyntaxList{T}"/> class.
/// </remarks>
/// <param name="items">The list items.</param>
/// <param name="separatorTokens">The separator tokens between items. There should be exactly
/// <c>Count - 1</c> separator tokens for a non-empty list, or zero tokens for an empty list.</param>
public sealed class SeparatedSyntaxList<T>(
    IReadOnlyList<T> items,
    IReadOnlyList<Token> separatorTokens) : IReadOnlyList<T>, IHasSourceLocation
    where T : IHasSourceLocation
{
    /// <summary>
    /// Gets an empty list instance.
    /// </summary>
    public static SeparatedSyntaxList<T> Empty { get; } = new(System.Array.Empty<T>(), System.Array.Empty<Token>());

    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<T> Items { get; } = items;

    /// <summary>
    /// Gets the separator tokens between items. Contains exactly <c>Count - 1</c> tokens for a
    /// non-empty list and zero tokens for an empty list.
    /// </summary>
    public IReadOnlyList<Token> SeparatorTokens { get; } = separatorTokens;

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
    /// Gets the source location of this list. If the list is non-empty, returns the location of the first item.
    /// </summary>
    public SourceLocation Location
    {
        get
        {
            if (Count > 0)
            {
                return SourceLocation.Merge(Items[0].Location, Items[Count - 1].Location);
            }
            else
            {
                return SourceLocation.Unknown;
            }
        }
    }

    /// <summary>
    /// Writes this list to the supplied syntax writer. Each element is interleaved with its
    /// preceding separator token.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="writeElement">A delegate that writes a single element to the writer.</param>
    public void WriteTo(
        Text.SyntaxWriter writer,
        Action<T, Text.SyntaxWriter> writeElement)
    {
        for (var i = 0; i < Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(SeparatorTokens[i - 1]);
                writer.SuggestTrivia(" ");
            }

            writeElement(Items[i], writer);
        }
    }

    /// <summary>
    /// Rewrites this list by applying the supplied delegates to each element and separator token.
    /// </summary>
    /// <param name="rewriteElement">A delegate that rewrites a single element.</param>
    /// <param name="rewriteToken">A delegate that rewrites a single separator token.</param>
    /// <returns>The rewritten list.</returns>
    public SeparatedSyntaxList<T> Rewrite(Func<T, T> rewriteElement, Func<Token, Token> rewriteToken)
    {
        T[]? rewrittenItems = null;
        var itemComparer = EqualityComparer<T>.Default;
        for (var i = 0; i < Count; i++)
        {
            var originalItem = Items[i];
            var newItem = rewriteElement(originalItem);

            if (rewrittenItems is not null)
            {
                rewrittenItems[i] = newItem;
            }
            else if (!itemComparer.Equals(originalItem, newItem))
            {
                rewrittenItems = new T[Count];
                for (var j = 0; j < i; j++)
                {
                    rewrittenItems[j] = Items[j];
                }

                rewrittenItems[i] = newItem;
            }
        }

        Token[]? rewrittenSeparatorTokens = null;
        var tokenComparer = EqualityComparer<Token>.Default;
        for (var i = 0; i < SeparatorTokens.Count; i++)
        {
            var originalToken = SeparatorTokens[i];
            var newToken = rewriteToken(originalToken);

            if (rewrittenSeparatorTokens is not null)
            {
                rewrittenSeparatorTokens[i] = newToken;
            }
            else if (!tokenComparer.Equals(originalToken, newToken))
            {
                rewrittenSeparatorTokens = new Token[SeparatorTokens.Count];
                for (var j = 0; j < i; j++)
                {
                    rewrittenSeparatorTokens[j] = SeparatorTokens[j];
                }

                rewrittenSeparatorTokens[i] = newToken;
            }
        }

        if (rewrittenItems is null && rewrittenSeparatorTokens is null)
        {
            return this;
        }

        return new SeparatedSyntaxList<T>(
            rewrittenItems ?? (T[])Items,
            rewrittenSeparatorTokens ?? (Token[])SeparatorTokens);
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
