namespace MLIR.Syntax;

/// <summary>
/// Represents a block argument in a region block header.
/// </summary>
public sealed class BlockArgumentSyntax
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
    /// Initializes a new instance of the <see cref="BlockArgumentSyntax"/> class.
    /// </summary>
    /// <param name="nameToken">The SSA name token.</param>
    /// <param name="colonToken">The separating colon token.</param>
    /// <param name="type">The declared argument type.</param>
    public BlockArgumentSyntax(SyntaxToken nameToken, SyntaxToken colonToken, RawSyntaxText type)
    {
        NameToken = nameToken;
        ColonToken = colonToken;
        Type = type;
    }

    /// <summary>
    /// Gets the SSA name token.
    /// </summary>
    public SyntaxToken NameToken { get; }

    /// <summary>
    /// Gets the separating colon token.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>
    /// Gets the SSA name of the block argument.
    /// </summary>
    public string Name => NameToken.Text;

    /// <summary>
    /// Gets the declared type text for the block argument.
    /// </summary>
    public RawSyntaxText Type { get; }
}
