namespace MLIR.Syntax.Attributes.Collections;

using MLIR.Text;

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
        Token openToken,
        IReadOnlyList<AttributeValueSyntax> items,
        IReadOnlyList<Token> separatorTokens,
        Token closeToken)
    {
        Items = new DelimitedSyntaxList<AttributeValueSyntax>(openToken, items, separatorTokens, closeToken);
    }

    /// <summary>
    /// Gets the bracketed item list.
    /// </summary>
    public DelimitedSyntaxList<AttributeValueSyntax> Items { get; }

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(
            Items.OpenToken.HasValue ? Items.OpenToken.Value.Location : SourceLocation.Unknown,
            Items.CloseToken.HasValue ? Items.CloseToken.Value.Location : SourceLocation.Unknown);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        Items.WriteTo(writer, static (item, w) => item.WriteTo(w));
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new ArrayAttributeValueSyntax(
            rewriter.VisitToken(Items.OpenToken!.Value),
            rewriter.VisitList(Items.Items),
            rewriter.VisitTokenList(Items.SeparatorTokens),
            rewriter.VisitToken(Items.CloseToken!.Value));
    }
}
