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
    /// The block label is taken from the syntax node.
    /// </summary>
    /// <param name="syntax">The concrete syntax node for the block.</param>
    /// <param name="arguments">The semantic block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public Block(BlockSyntax syntax, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        Syntax = syntax;
        Label = syntax.Label;
        Arguments = arguments;
        Operations = operations;
        LabelReference = new BlockReference(syntax.LabelToken);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class as a synthetic block with no corresponding source text.
    /// </summary>
    /// <param name="label">The block label, including the leading <c>^</c>.</param>
    /// <param name="arguments">The semantic block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public Block(string label, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        Syntax = null;
        Label = label;
        Arguments = arguments;
        Operations = operations;
        LabelReference = null;
    }

    /// <summary>
    /// Gets the concrete syntax node for the block, or null if this is a synthetic block with no corresponding source text.
    /// </summary>
    public BlockSyntax? Syntax { get; }

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
    public string Label { get; }

    /// <summary>
    /// Gets the typed reference to the block label, or null if this is a synthetic block with no label token.
    /// </summary>
    public BlockReference? LabelReference { get; }

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => LabelReference?.Location ?? SourceLocation.Unknown;
}
