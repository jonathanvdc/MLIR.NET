namespace MLIR.Syntax;

using System.Collections;
using System.Collections.Generic;

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
    IReadOnlyList<SyntaxToken> separatorTokens) : IReadOnlyList<T>
{
    /// <summary>
    /// Gets an empty list instance.
    /// </summary>
    public static SeparatedSyntaxList<T> Empty { get; } = new(System.Array.Empty<T>(), System.Array.Empty<SyntaxToken>());

    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<T> Items { get; } = items;

    /// <summary>
    /// Gets the separator tokens between items. Contains exactly <c>Count - 1</c> tokens for a
    /// non-empty list and zero tokens for an empty list.
    /// </summary>
    public IReadOnlyList<SyntaxToken> SeparatorTokens { get; } = separatorTokens;

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
    /// Writes this list to the supplied syntax writer. Each element is interleaved with its
    /// preceding separator token. The first element receives <paramref name="firstLeadingTrivia"/>
    /// as its fallback leading trivia; subsequent elements receive a single space as their fallback.
    /// Does nothing when the list is empty.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="firstLeadingTrivia">The fallback leading trivia to use for the first element.</param>
    /// <param name="writeElement">A delegate that writes a single element to the writer.</param>
    public void WriteTo(
        Text.SyntaxWriter writer,
        string firstLeadingTrivia,
        System.Action<T, Text.SyntaxWriter, string> writeElement)
    {
        for (var i = 0; i < Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(SeparatorTokens[i - 1], string.Empty);
            }

            writeElement(Items[i], writer, i > 0 ? " " : firstLeadingTrivia);
        }
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
