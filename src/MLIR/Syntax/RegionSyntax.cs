namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents a region nested under an MLIR operation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RegionSyntax"/> class.
/// </remarks>
/// <param name="openBraceToken">The opening brace token.</param>
/// <param name="blocks">The blocks contained in the region.</param>
/// <param name="closeBraceToken">The closing brace token.</param>
public sealed class RegionSyntax(SyntaxToken openBraceToken, IReadOnlyList<BlockSyntax> blocks, SyntaxToken closeBraceToken)
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
    /// Gets the opening brace token.
    /// </summary>
    public SyntaxToken OpenBraceToken { get; } = openBraceToken;

    /// <summary>
    /// Gets the blocks contained in the region.
    /// </summary>
    public IReadOnlyList<BlockSyntax> Blocks { get; } = blocks;

    /// <summary>
    /// Gets the closing brace token.
    /// </summary>
    public SyntaxToken CloseBraceToken { get; } = closeBraceToken;

    /// <summary>
    /// Writes this region to the supplied syntax writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="indentLevel">The indentation level of the containing operation.</param>
    public void WriteTo(
        SyntaxWriter writer,
        int indentLevel)
    {
        writer.WriteToken(OpenBraceToken, " ");

        foreach (var block in Blocks)
        {
            block.WriteTo(writer, indentLevel);
        }

        writer.WriteToken(CloseBraceToken, "\n", indentLevel);
    }
}
