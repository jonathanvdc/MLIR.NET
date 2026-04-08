namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents a block argument in a region block header.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlockArgumentSyntax"/> class.
/// </remarks>
/// <param name="nameToken">The SSA name token.</param>
/// <param name="colonToken">The separating colon token.</param>
/// <param name="typeSyntax">The declared argument type syntax.</param>
public sealed class BlockArgumentSyntax(SyntaxToken nameToken, SyntaxToken colonToken, TypeSyntax typeSyntax) : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockArgumentSyntax"/> class.
    /// </summary>
    /// <param name="name">The SSA name of the block argument.</param>
    /// <param name="type">The declared argument type.</param>
    public BlockArgumentSyntax(string name, RawSyntaxText type)
        : this(SyntaxTokenFactory.SsaName(name), SyntaxTokenFactory.Colon(), new RawTypeSyntax(type))
    {
    }

    /// <summary>
    /// Gets the SSA name token.
    /// </summary>
    public SyntaxToken NameToken { get; } = nameToken;

    /// <summary>
    /// Gets the separating colon token.
    /// </summary>
    public SyntaxToken ColonToken { get; } = colonToken;

    /// <summary>
    /// Gets the SSA name of the block argument.
    /// </summary>
    public string Name => NameToken.Text;

    /// <summary>
    /// Gets the declared type syntax for the block argument.
    /// </summary>
    public TypeSyntax TypeSyntax { get; } = typeSyntax;

    /// <summary>
    /// Gets the merged source location spanning from the argument name to the end of the type.
    /// Returns an unknown location when neither token has source information.
    /// </summary>
    public override SourceLocation Location =>
        SourceLocation.Merge(NameToken.Location, TypeSyntax.Location);

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(NameToken);
        writer.WriteToken(ColonToken);
        writer.SuggestTrivia(" ");
        TypeSyntax.WriteTo(writer);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new BlockArgumentSyntax(
            rewriter.VisitToken(NameToken),
            rewriter.VisitToken(ColonToken),
            (TypeSyntax)rewriter.Visit(TypeSyntax));
    }
}
