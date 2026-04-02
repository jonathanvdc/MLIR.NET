namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic block within a region.
/// </summary>
public sealed class Block
{
    private readonly List<BlockArgument> arguments;
    private readonly List<Operation> operations;

    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class from a concrete syntax node.
    /// </summary>
    public Block(BlockSyntax syntax, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
        : this(new BlockReference(syntax.LabelToken), arguments, operations)
    {
        Syntax = syntax;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class as a synthetic block with no corresponding source text.
    /// </summary>
    public Block(BlockReference labelReference, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        LabelReference = labelReference;
        this.arguments = new List<BlockArgument>(arguments.Count);
        this.operations = new List<Operation>(operations.Count);
        foreach (var argument in arguments)
        {
            AttachArgument(argument, invalidateSyntax: false);
        }

        foreach (var operation in operations)
        {
            AttachOperation(operation, invalidateSyntax: false);
        }
    }

    /// <summary>
    /// Gets or sets the concrete syntax node for the block, or null if this is a synthetic block with no corresponding source text.
    /// </summary>
    public BlockSyntax? Syntax { get; private set; }

    /// <summary>
    /// Gets the semantic reference to the block label.
    /// </summary>
    public BlockReference LabelReference { get; }

    /// <summary>
    /// Gets the region that owns this block.
    /// </summary>
    public Region? ParentRegion { get; private set; }

    /// <summary>
    /// Gets the semantic block arguments.
    /// </summary>
    public IReadOnlyList<BlockArgument> Arguments => arguments;

    /// <summary>
    /// Gets the operations contained in the block.
    /// </summary>
    public IReadOnlyList<Operation> Operations => operations;

    /// <summary>
    /// Gets the block label, including the leading <c>^</c>.
    /// </summary>
    public string Label => LabelReference.Label;

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => LabelReference.Location;

    /// <summary>
    /// Adds an argument to the block.
    /// </summary>
    public void AddArgument(BlockArgument argument)
    {
        AttachArgument(argument, invalidateSyntax: true);
    }

    private void AttachArgument(BlockArgument argument, bool invalidateSyntax)
    {
        arguments.Add(argument);
        argument.Bind(this, arguments.Count - 1);
        if (invalidateSyntax)
        {
            InvalidateSyntax();
        }
    }

    /// <summary>
    /// Adds an operation to the block.
    /// </summary>
    public void AddOperation(Operation operation)
    {
        AttachOperation(operation, invalidateSyntax: true);
    }

    private void AttachOperation(Operation operation, bool invalidateSyntax)
    {
        operations.Add(operation);
        operation.Bind(this);
        if (invalidateSyntax)
        {
            InvalidateSyntax();
        }
    }

    /// <summary>
    /// Invalidates any cached syntax for this block and its ancestors.
    /// </summary>
    public void InvalidateSyntax()
    {
        Syntax = null;
        ParentRegion?.InvalidateSyntax();
    }

    internal void Bind(Region parentRegion)
    {
        ParentRegion = parentRegion;
    }
}
