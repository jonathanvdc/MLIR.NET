namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a semantic block argument.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlockArgument"/> class.
/// </remarks>
public sealed class BlockArgument(BlockArgumentSyntax syntax, TypeReference typeReference) : Value(syntax.NameToken)
{
    /// <summary>
    /// Gets the concrete syntax node for the block argument.
    /// </summary>
    public BlockArgumentSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the block that owns this argument.
    /// </summary>
    public Block? Owner { get; private set; }

    /// <summary>
    /// Gets the zero-based argument index within the owning block.
    /// </summary>
    public int Index { get; private set; } = -1;

    private TypeReference type = typeReference;

    /// <summary>
    /// Gets the semantic type reference for the argument type.
    /// </summary>
    public TypeReference Type
    {
        get => type;
        set
        {
            type = value;
            Owner?.InvalidateSyntax();
        }
    }

    internal void Bind(Block owner, int index)
    {
        Owner = owner;
        Index = index;
    }

    /// <inheritdoc/>
    protected override Block? GetOwningBlock()
    {
        return Owner;
    }

    /// <inheritdoc/>
    protected override void OnNameChanged()
    {
        Owner?.InvalidateSyntax();
    }
}
