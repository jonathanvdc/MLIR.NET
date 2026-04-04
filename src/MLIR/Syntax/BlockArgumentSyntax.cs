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
public sealed class BlockArgumentSyntax(SyntaxToken nameToken, SyntaxToken colonToken, TypeSyntax typeSyntax)
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

    /// <summary>
    /// Writes this block argument to the supplied syntax writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia to use when syntax does not carry explicit trivia.</param>
    public void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(NameToken, defaultLeadingTrivia);
        writer.WriteToken(ColonToken, string.Empty);
        TypeSyntax.WriteTo(writer, " ");
    }
}
