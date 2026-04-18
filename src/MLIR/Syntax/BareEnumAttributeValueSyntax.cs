namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents an enum attribute value, which consists of a comma-separated list of tokens, without angle brackets.
/// For example, an enum attribute value might look like <c>foo, bar, baz</c>, where <c>foo</c>, <c>bar</c>, and <c>baz</c> are individual tokens that together represent the value of the enum attribute.
/// </summary>
/// <param name="elements">The list of tokens representing the enum attribute value.</param>
public sealed class BareEnumAttributeValueSyntax(SeparatedSyntaxList<Token> elements) : EnumAttributeValueSyntax
{
    /// <summary>
    /// Gets the list of tokens representing the enum attribute value.
    /// For example, for an enum attribute value like <c>foo, bar, baz</c>, this list would contain three tokens: <c>foo</c>, <c>bar</c>, and <c>baz</c>.
    /// The source location of the entire enum attribute value is determined by the location of the elements in this list.
    /// </summary>
    /// <remarks>
    /// The <see cref="Elements"/> list should not be empty for a valid enum attribute value.
    /// </remarks>
    public SeparatedSyntaxList<Token> ElementList { get; } = elements;

    /// <inheritdoc/>
    public override IReadOnlyList<Token> Elements => ElementList.Items;

    /// <inheritdoc/>
    public override SourceLocation Location => ElementList.Location;

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        var rewrittenElements = ElementList.Rewrite(rewriter.VisitToken, rewriter.VisitToken);
        if (ReferenceEquals(rewrittenElements, ElementList)) return this;
        else return new BareEnumAttributeValueSyntax(rewrittenElements);
    }

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        ElementList.WriteTo(writer, static (t, w) => w.WriteToken(t));
    }
}
