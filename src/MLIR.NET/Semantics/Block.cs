namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic block within a region.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Block"/> class.
/// </remarks>
/// <param name="syntax">The concrete syntax node for the block.</param>
/// <param name="arguments">The semantic block arguments.</param>
/// <param name="operations">The operations contained in the block.</param>
public sealed class Block(BlockSyntax syntax, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<OperationBase> operations)
{
    /// <summary>
    /// Gets the concrete syntax node for the block.
    /// </summary>
    public BlockSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the semantic block arguments.
    /// </summary>
    public IReadOnlyList<BlockArgument> Arguments { get; } = arguments;

    /// <summary>
    /// Gets the operations contained in the block.
    /// </summary>
    public IReadOnlyList<OperationBase> Operations { get; } = operations;

    /// <summary>
    /// Gets the block label, including the leading <c>^</c>.
    /// </summary>
    public string Label => Syntax.Label;

    /// <summary>
    /// Gets the typed reference to the block label.
    /// </summary>
    public BlockReference LabelReference { get; } = new BlockReference(syntax.LabelToken);

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => LabelReference.Location;
}
