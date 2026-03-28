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
    {
        Blocks = blocks;
    }

    /// <summary>
    /// Gets the blocks contained in the region.
    /// </summary>
    public IReadOnlyList<BlockSyntax> Blocks { get; }
}
