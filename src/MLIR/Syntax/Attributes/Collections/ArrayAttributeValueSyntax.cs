namespace MLIR.Syntax.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a bracketed array-style attribute value.
/// </summary>
public sealed class ArrayAttributeValueSyntax : AttributeValueSyntax
{
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
    }

    /// <summary>
    /// Gets the bracketed item list.
    /// </summary>
    public DelimitedSyntaxList<AttributeValueSyntax> Items { get; }

    /// <inheritdoc/>
    public override SourceLocation Location => Items.OpenToken.HasValue ? Items.OpenToken.Value.Location : SourceLocation.Unknown;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        Items.WriteTo(writer, static (item, w) => item.WriteTo(w));
    }
}
