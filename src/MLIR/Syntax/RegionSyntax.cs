namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Semantics;
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
public sealed class RegionSyntax(SyntaxToken openBraceToken, IReadOnlyList<BlockSyntax> blocks, SyntaxToken closeBraceToken) : SyntaxNode
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
    /// Gets the merged source location spanning from the opening brace to the closing brace.
    /// Returns an unknown location when neither brace token has source information.
    /// </summary>
    public override SourceLocation Location =>
        SourceLocation.Merge(OpenBraceToken.Location, CloseBraceToken.Location);

    /// <summary>
    /// Writes this region to the supplied syntax writer.
    /// Uses <see cref="Text.SyntaxWriter.IndentLevel"/> as the indentation level of the
    /// containing operation when computing trivia for the closing brace.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    public override void WriteTo(SyntaxWriter writer)
    {
        var containingOpIndentLevel = writer.IndentLevel;

        writer.WriteToken(OpenBraceToken, " ");

        foreach (var block in Blocks)
        {
            writer.IndentLevel = containingOpIndentLevel;
            block.WriteTo(writer);
        }

        writer.IndentLevel = containingOpIndentLevel;
        writer.WriteToken(CloseBraceToken, "\n" + new string(' ', containingOpIndentLevel * 2));
    }
}
