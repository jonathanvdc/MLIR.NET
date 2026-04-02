namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a semantic block argument.
/// </summary>
public sealed class BlockArgument : Value
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockArgument"/> class.
    /// </summary>
    public BlockArgument(BlockArgumentSyntax syntax, TypeReference typeReference)
        : base(syntax.NameToken)
    {
        Syntax = syntax;
        TypeReference = typeReference;
    }

    /// <summary>
    /// Gets the concrete syntax node for the block argument.
    /// </summary>
    public BlockArgumentSyntax Syntax { get; }

    /// <summary>
    /// Gets the block that owns this argument.
    /// </summary>
    public Block? Owner { get; private set; }

    /// <summary>
    /// Gets the zero-based argument index within the owning block.
    /// </summary>
    public int Index { get; private set; } = -1;

    /// <summary>
    /// Gets the declared type text for the block argument.
    /// </summary>
    public RawSyntaxText Type => Syntax.RawType;

    /// <summary>
    /// Gets the semantic type reference for the argument type.
    /// </summary>
    public TypeReference TypeReference { get; private set; }

    /// <summary>
    /// Updates the semantic type reference for the argument type.
    /// </summary>
    public void SetTypeReference(TypeReference typeReference)
    {
        TypeReference = typeReference;
        Owner?.InvalidateSyntax();
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
