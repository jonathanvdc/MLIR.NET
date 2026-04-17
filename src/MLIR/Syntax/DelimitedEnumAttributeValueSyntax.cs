namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents an enum attribute value, which consists of a comma-separated list of tokens, enclosed in angle brackets.
/// For example, an enum attribute value might look like <c>&lt;foo, bar, baz&gt;</c>, where <c>foo</c>, <c>bar</c>, and <c>baz</c> are individual tokens that together represent the value of the enum attribute.
/// </summary>
/// <param name="elements">The list of tokens representing the enum attribute value.</param>
public sealed class DelimitedEnumAttributeValueSyntax(DelimitedSyntaxList<Token> elements) : EnumAttributeValueSyntax
{
    /// <summary>
    /// Gets the list of tokens representing the enum attribute value.
    /// For example, for an enum attribute value like <c>&lt;foo, bar, baz&gt;</c>, this list would contain three tokens: <c>foo</c>, <c>bar</c>, and <c>baz</c>.
    /// The source location of the entire enum attribute value is determined by the location of the elements in this list.
    /// </summary>
    /// <remarks>
    /// The <see cref="Elements"/> list should not be empty for a valid enum attribute value. If it is empty, the source location will be reported as unknown.
    /// </remarks>
    public DelimitedSyntaxList<Token> ElementList { get; } = elements;

    /// <inheritdoc/>
    public override IReadOnlyList<Token> Elements => ElementList.Items;

    /// <inheritdoc/>
    public override SourceLocation Location => ElementList.Location;

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        var rewrittenElements = ElementList.Rewrite(rewriter.VisitToken, rewriter.VisitToken, rewriter.VisitToken, rewriter.VisitToken);
        if (ReferenceEquals(rewrittenElements, Elements)) return this;
        else return new DelimitedEnumAttributeValueSyntax(rewrittenElements);
    }

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        ElementList.WriteTo(writer, static (t, w) => w.WriteToken(t));
    }
}
