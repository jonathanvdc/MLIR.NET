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
    /// <param name="block">The block to add.</param>
    /// <param name="uniquify">
    /// When <see langword="true"/>, automatically renames the block label to avoid a conflict with
    /// an existing block in the region (analogous to <c>uniquify: true</c> for SSA value names).
    /// When <see langword="false"/> (the default), an <see cref="InvalidOperationException"/> is
    /// thrown if a block with the same label already exists.
    /// </param>
    public void AddBlock(Block block, bool uniquify = false)
    {
        AttachBlock(block, invalidateSyntax: true, uniquify: uniquify);
    }

    /// <summary>
    /// Returns a block label that does not conflict with any block already in this region,
    /// based on the supplied preferred label.
    /// </summary>
    public string GetUniqueLabelName(string preferredLabel)
    {
        if (!blocksByLabel.ContainsKey(preferredLabel))
        {
            return preferredLabel;
        }

        var suffix = 1;
        while (true)
        {
            var candidate = preferredLabel + "_" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!blocksByLabel.ContainsKey(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private void AttachBlock(Block block, bool invalidateSyntax, bool uniquify = false)
    {
        if (blocksByLabel.ContainsKey(block.Label))
        {
            if (!uniquify)
            {
                throw new InvalidOperationException($"The region already contains a block labeled '{block.Label}'.");
            }

            block.SetLabelWithoutValidation(GetUniqueLabelName(block.Label));
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
