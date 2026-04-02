namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic region nested under an operation.
/// </summary>
public sealed class Region
{
    private readonly List<Block> blocks;
    private readonly Dictionary<string, Block> blocksByLabel = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Region"/> class.
    /// </summary>
    public Region(RegionSyntax? syntax, IReadOnlyList<Block> blocks)
    {
        Syntax = syntax;
        this.blocks = new List<Block>(blocks.Count);
        foreach (var block in blocks)
        {
            AttachBlock(block, invalidateSyntax: false);
        }
    }

    /// <summary>
    /// Gets or sets the concrete syntax node for the region, or null if this is a synthetic region with no corresponding source text.
    /// </summary>
    public RegionSyntax? Syntax { get; private set; }

    /// <summary>
    /// Gets the operation that owns this region.
    /// </summary>
    public Operation? ParentOperation { get; private set; }

    /// <summary>
    /// Gets the semantic blocks contained in the region.
    /// </summary>
    public IReadOnlyList<Block> Blocks => blocks;

    /// <summary>
    /// Adds a block to the region.
    /// </summary>
    public void AddBlock(Block block)
    {
        AttachBlock(block, invalidateSyntax: true);
    }

    private void AttachBlock(Block block, bool invalidateSyntax)
    {
        if (blocksByLabel.ContainsKey(block.Label))
        {
            throw new InvalidOperationException($"The region already contains a block labeled '{block.Label}'.");
        }

        blocksByLabel[block.Label] = block;
        blocks.Add(block);
        block.Bind(this);
        if (invalidateSyntax)
        {
            InvalidateSyntax();
        }
    }

    /// <summary>
    /// Invalidates any cached syntax for this region and its ancestors.
    /// </summary>
    public void InvalidateSyntax()
    {
        Syntax = null;
        ParentOperation?.InvalidateSyntax();
    }

    internal void Bind(Operation parentOperation)
    {
        ParentOperation = parentOperation;
    }
}
