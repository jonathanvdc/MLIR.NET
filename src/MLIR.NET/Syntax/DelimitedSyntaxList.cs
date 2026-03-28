namespace MLIR.Syntax;

using System.Collections;
using System.Collections.Generic;

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
    SyntaxToken? openToken,
    IReadOnlyList<T> items,
    IReadOnlyList<SyntaxToken> separatorTokens,
    SyntaxToken? closeToken) : IReadOnlyList<T>
{
    /// <summary>
    /// Gets the opening delimiter token.
    /// </summary>
    public SyntaxToken? OpenToken { get; } = openToken;

    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<T> Items { get; } = items;

    /// <summary>
    /// Gets the separator tokens between items.
    /// </summary>
    public IReadOnlyList<SyntaxToken> SeparatorTokens { get; } = separatorTokens;

    /// <summary>
    /// Gets the closing delimiter token.
    /// </summary>
    public SyntaxToken? CloseToken { get; } = closeToken;

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
