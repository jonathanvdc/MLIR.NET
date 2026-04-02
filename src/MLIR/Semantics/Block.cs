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
    private readonly Dictionary<string, Value> valuesByName = [];
    private readonly List<OpSuccessor> uses = [];
    private readonly string label;

    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class from a concrete syntax node.
    /// </summary>
    public Block(BlockSyntax syntax, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        Syntax = syntax;
        label = syntax.LabelToken.Text;
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
    /// Initializes a new instance of the <see cref="Block"/> class as a synthetic block with no corresponding source text.
    /// </summary>
    /// <param name="label">The block label, including the leading <c>^</c>.</param>
    /// <param name="arguments">The block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public Block(string label, IReadOnlyList<BlockArgument> arguments, IReadOnlyList<Operation> operations)
    {
        this.label = label;
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
    public string Label => label;

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => Syntax != null ? SourceLocation.FromToken(Syntax.LabelToken) : SourceLocation.Unknown;

    /// <summary>
    /// Gets the uses of this block as a successor of an operation.
    /// </summary>
    public IReadOnlyList<OpSuccessor> Uses => uses;

    /// <summary>
    /// Adds an argument to the block.
    /// </summary>
    public void AddArgument(BlockArgument argument)
    {
        AttachArgument(argument, invalidateSyntax: true);
    }

    private void AttachArgument(BlockArgument argument, bool invalidateSyntax)
    {
        AssignValueName(argument, argument.Name, uniquify: true);
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
        var renamedResult = false;
        foreach (var result in operation.Results)
        {
            var originalName = result.Name;
            var finalName = AssignValueName(result, result.Name, uniquify: true);
            renamedResult |= originalName != finalName;
        }

        if (renamedResult)
        {
            operation.InvalidateSyntax();
        }

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

    /// <summary>
    /// Determines whether the given SSA value name is available within this block.
    /// </summary>
    public bool IsValueNameAvailable(string name)
    {
        return !valuesByName.ContainsKey(name);
    }

    /// <summary>
    /// Gets a unique SSA value name for this block based on the supplied preferred name.
    /// </summary>
    public string GetUniqueValueName(string preferredName)
    {
        if (IsValueNameAvailable(preferredName))
        {
            return preferredName;
        }

        var suffix = 1;
        while (true)
        {
            var candidate = preferredName + "_" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (IsValueNameAvailable(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    internal void AddOperationFromSyntax(Operation operation)
    {
        AttachOperation(operation, invalidateSyntax: false);
    }

    internal void Bind(Region parentRegion)
    {
        ParentRegion = parentRegion;
    }

    internal void AddUse(OpSuccessor successor)
    {
        uses.Add(successor);
    }

    internal void RemoveUse(OpSuccessor successor)
    {
        uses.Remove(successor);
    }

    internal string AssignValueName(Value value, string preferredName, bool uniquify)
    {
        if (valuesByName.TryGetValue(value.Name, out var existing) && ReferenceEquals(existing, value))
        {
            valuesByName.Remove(value.Name);
        }

        var finalName = preferredName;
        if (valuesByName.TryGetValue(preferredName, out var conflicting) && !ReferenceEquals(conflicting, value))
        {
            if (!uniquify)
            {
                throw new InvalidOperationException($"The block already defines an SSA value named '{preferredName}'.");
            }

            finalName = GetUniqueValueName(preferredName);
        }

        value.SetNameWithoutValidation(finalName);
        valuesByName[finalName] = value;
        return finalName;
    }
}
