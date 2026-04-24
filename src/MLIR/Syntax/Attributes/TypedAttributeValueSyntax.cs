namespace MLIR.Syntax.Attributes;

using MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a typed attribute value of the form <c>value : type</c>.
/// </summary>
public sealed class TypedAttributeValueSyntax(
    AttributeValueSyntax attributeSyntax,
    Token colonToken,
    TypeSyntax typeSyntax) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the attribute payload syntax.
    /// </summary>
    public AttributeValueSyntax AttributeSyntax { get; } = attributeSyntax;

    /// <summary>
    /// Gets the colon token between payload and type.
    /// </summary>
    public Token ColonToken { get; } = colonToken;

    /// <summary>
    /// Gets the trailing self type syntax.
    /// </summary>
    public TypeSyntax TypeSyntax { get; } = typeSyntax;

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(AttributeSyntax.Location, TypeSyntax.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        AttributeSyntax.WriteTo(writer);
        writer.WriteToken(ColonToken, " ");
        writer.SuggestTrivia(" ");
        TypeSyntax.WriteTo(writer);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new TypedAttributeValueSyntax(
            (AttributeValueSyntax)rewriter.Visit(AttributeSyntax),
            rewriter.VisitToken(ColonToken),
            (TypeSyntax)rewriter.Visit(TypeSyntax));
    }
}
