namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic block within a region.
/// </summary>
public sealed class Block
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class.
    /// </summary>
    /// <param name="syntax">The concrete syntax node for the block, or null for a synthetic block with no corresponding source text.</param>
    /// <param name="arguments">The semantic block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public Block(BlockSyntax? syntax, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        Syntax = syntax;
        Arguments = arguments;
        Operations = operations;
        LabelReference = syntax != null ? new BlockReference(syntax.LabelToken) : null;
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
    /// Gets the block label, including the leading <c>^</c>, or null if this is a synthetic block with no label.
    /// </summary>
    public string? Label => Syntax?.Label;

    /// <summary>
    /// Gets the typed reference to the block label, or null if this is a synthetic block with no label.
    /// </summary>
    public BlockReference? LabelReference { get; }

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => LabelReference?.Location ?? SourceLocation.Unknown;
}
