namespace MLIR.Syntax;

/// <summary>
/// Represents a block argument in a region block header.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlockArgumentSyntax"/> class.
/// </remarks>
/// <param name="nameToken">The SSA name token.</param>
/// <param name="colonToken">The separating colon token.</param>
/// <param name="type">The declared argument type.</param>
public sealed class BlockArgumentSyntax(SyntaxToken nameToken, SyntaxToken colonToken, RawSyntaxText type)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockArgumentSyntax"/> class.
    /// </summary>
    /// <param name="name">The SSA name of the block argument.</param>
    /// <param name="type">The declared argument type.</param>
    public BlockArgumentSyntax(string name, RawSyntaxText type)
        : this(new SyntaxToken(name), new SyntaxToken(":"), type)
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
    /// Gets the declared type text for the block argument.
    /// </summary>
    public RawSyntaxText Type { get; } = type;
}
