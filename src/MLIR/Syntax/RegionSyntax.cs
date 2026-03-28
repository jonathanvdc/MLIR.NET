namespace MLIR.Syntax;

using System.Collections.Generic;
using System;
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

    internal void WriteTo(
        SyntaxWriter writer,
        int indentLevel,
        Action<SyntaxWriter, OperationSyntax, int, string> writeOperation)
    {
        writer.WriteToken(OpenBraceToken, " ");

        foreach (var block in Blocks)
        {
            block.WriteTo(writer, indentLevel, writeOperation);
        }

        writer.WriteToken(CloseBraceToken, "\n", indentLevel);
    }
}
