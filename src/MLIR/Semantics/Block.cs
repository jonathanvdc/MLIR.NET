namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic block within a region.
/// </summary>
public sealed class Block
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class from a concrete syntax node.
    /// The block label reference is derived from the syntax node's label token.
    /// </summary>
    /// <param name="syntax">The concrete syntax node for the block.</param>
    /// <param name="arguments">The semantic block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public Block(BlockSyntax syntax, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        Syntax = syntax;
        LabelReference = new BlockReference(syntax.LabelToken);
        Arguments = arguments;
        Operations = operations;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class as a synthetic block with no corresponding source text.
    /// </summary>
    /// <param name="labelReference">The semantic reference to the block label.</param>
    /// <param name="arguments">The semantic block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public Block(BlockReference labelReference, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        Syntax = null;
        LabelReference = labelReference;
        Arguments = arguments;
        Operations = operations;
    }

    /// <summary>
    /// Gets the concrete syntax node for the block, or null if this is a synthetic block with no corresponding source text.
    /// </summary>
    public BlockSyntax? Syntax { get; }

    /// <summary>
    /// Gets the semantic reference to the block label.
    /// </summary>
    public BlockReference LabelReference { get; }

    /// <summary>
    /// Gets the semantic block arguments.
    /// </summary>
    public IReadOnlyList<BlockArgument> Arguments { get; }

    /// <summary>
    /// Gets the operations contained in the block.
    /// </summary>
    public IReadOnlyList<Operation> Operations { get; }

    /// <summary>
    /// Gets the block label, including the leading <c>^</c>.
    /// </summary>
    public string Label => LabelReference.Label;

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => LabelReference.Location;
}
