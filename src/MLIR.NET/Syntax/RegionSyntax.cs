namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a region nested under an MLIR operation.
/// </summary>
public sealed class RegionSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegionSyntax"/> class.
    /// </summary>
    /// <param name="blocks">The blocks contained in the region.</param>
    public RegionSyntax(IReadOnlyList<BlockSyntax> blocks)
        : this(new SyntaxToken("{"), blocks, new SyntaxToken("}"))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionSyntax"/> class.
    /// </summary>
    /// <param name="openBraceToken">The opening brace token.</param>
    /// <param name="blocks">The blocks contained in the region.</param>
    /// <param name="closeBraceToken">The closing brace token.</param>
    public RegionSyntax(SyntaxToken openBraceToken, IReadOnlyList<BlockSyntax> blocks, SyntaxToken closeBraceToken)
    {
        OpenBraceToken = openBraceToken;
        Blocks = blocks;
        CloseBraceToken = closeBraceToken;
    }

    /// <summary>
    /// Gets the opening brace token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; }

    /// <summary>
    /// Gets the blocks contained in the region.
    /// </summary>
    public IReadOnlyList<BlockSyntax> Blocks { get; }

    /// <summary>
    /// Gets the closing brace token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; }
}
