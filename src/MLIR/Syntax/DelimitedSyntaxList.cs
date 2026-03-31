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
    /// Writes the opening delimiter, then each element (interleaved with separator tokens), then
    /// the closing delimiter. When <see cref="IsPresent"/> is <see langword="false"/> this method
    /// does nothing.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="openLeadingTrivia">The fallback leading trivia to use for the opening delimiter token.</param>
    /// <param name="writeElement">A delegate that writes a single element to the writer.</param>
    public void WriteTo(
        Text.SyntaxWriter writer,
        string openLeadingTrivia,
        System.Action<T, Text.SyntaxWriter, string> writeElement)
    {
        if (!OpenToken.HasValue)
        {
            return;
        }

        writer.WriteToken(OpenToken.Value, openLeadingTrivia);
        for (var i = 0; i < Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(SeparatorTokens[i - 1], string.Empty);
            }

            writeElement(Items[i], writer, i > 0 ? " " : string.Empty);
        }

        writer.WriteToken(CloseToken!.Value, string.Empty);
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
