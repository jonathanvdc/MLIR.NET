namespace MLIR.Syntax.Attributes;

using MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a type attribute whose value is a nested type syntax node.
/// </summary>
public sealed class TypeAttributeValueSyntax(TypeSyntax typeSyntax) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the nested type syntax.
    /// </summary>
    public TypeSyntax TypeSyntax { get; } = typeSyntax;

    /// <inheritdoc/>
    public override SourceLocation Location => TypeSyntax.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        TypeSyntax.WriteTo(writer);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new TypeAttributeValueSyntax((TypeSyntax)rewriter.Visit(TypeSyntax));
    }
}
