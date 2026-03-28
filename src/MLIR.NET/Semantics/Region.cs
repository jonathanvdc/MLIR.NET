namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic region nested under an operation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Region"/> class.
/// </remarks>
/// <param name="syntax">The concrete syntax node for the region.</param>
/// <param name="blocks">The semantic blocks contained in the region.</param>
public sealed class Region(RegionSyntax syntax, IReadOnlyList<Block> blocks)
{
    /// <summary>
    /// Gets the concrete syntax node for the region.
    /// </summary>
    public RegionSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the semantic blocks contained in the region.
    /// </summary>
    public IReadOnlyList<Block> Blocks { get; } = blocks;
}
