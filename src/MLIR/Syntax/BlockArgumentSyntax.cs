namespace MLIR.Syntax;

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
        : this(new SyntaxToken(name), new SyntaxToken(":"), new RawTypeSyntax(type))
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

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(NameToken);
        writer.WriteToken(ColonToken, string.Empty);
        writer.SuggestTrivia(" ");
        TypeSyntax.WriteTo(writer);
    }
}
