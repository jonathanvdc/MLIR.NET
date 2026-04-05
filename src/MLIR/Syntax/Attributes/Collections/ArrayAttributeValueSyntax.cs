namespace MLIR.Syntax.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a bracketed array-style attribute value.
/// </summary>
public sealed class ArrayAttributeValueSyntax : AttributeValueSyntax
{
    private readonly RawSyntaxText rawText;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayAttributeValueSyntax"/> class.
    /// </summary>
    public ArrayAttributeValueSyntax(
        SyntaxToken openToken,
        IReadOnlyList<AttributeValueSyntax> items,
        IReadOnlyList<SyntaxToken> separatorTokens,
        SyntaxToken closeToken)
    {
        Items = new DelimitedSyntaxList<AttributeValueSyntax>(openToken, items, separatorTokens, closeToken);

        var tokens = new List<SyntaxToken> { openToken };
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].TryGetRawText(out var itemRaw))
            {
                tokens.AddRange(itemRaw!.Tokens);
            }

            if (i < separatorTokens.Count)
            {
                tokens.Add(separatorTokens[i]);
            }
        }

        tokens.Add(closeToken);
        rawText = new RawSyntaxText(tokens);
    }

    /// <summary>
    /// Gets the bracketed item list.
    /// </summary>
    public DelimitedSyntaxList<AttributeValueSyntax> Items { get; }

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = this.rawText;
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        Items.WriteTo(writer, static (item, w) => item.WriteTo(w));
    }
}
