namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a single block within an MLIR region.
/// </summary>
public sealed class BlockSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockSyntax"/> class.
    /// </summary>
    /// <param name="label">The block label, including the leading <c>^</c>.</param>
    /// <param name="arguments">The block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public BlockSyntax(string label, IReadOnlyList<BlockArgumentSyntax> arguments, IReadOnlyList<OperationSyntax> operations)
    {
        Label = label;
        Arguments = arguments;
        Operations = operations;
    }

    /// <summary>
    /// Gets the block label, including the leading <c>^</c>.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the block arguments.
    /// </summary>
    public IReadOnlyList<BlockArgumentSyntax> Arguments { get; }

    /// <summary>
    /// Gets the operations contained in the block.
    /// </summary>
    public IReadOnlyList<OperationSyntax> Operations { get; }
}
