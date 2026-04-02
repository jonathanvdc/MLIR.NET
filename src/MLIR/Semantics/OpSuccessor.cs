namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a successor use owned by an operation.
/// </summary>
public sealed class OpSuccessor
{
    private Block? block;
    private string label;

    internal OpSuccessor(Operation owner, int index, BlockReference reference)
    {
        Owner = owner;
        Index = index;
        Token = reference.Token;
        label = reference.Label;
    }

    /// <summary>
    /// Gets the operation that owns this successor slot.
    /// </summary>
    public Operation Owner { get; }

    /// <summary>
    /// Gets the zero-based successor index within the owning operation.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the syntax token for the block label, or null if this is a synthetic successor with no corresponding source token.
    /// </summary>
    public SyntaxToken? Token { get; }

    /// <summary>
    /// Gets or sets the successor block used by this slot.
    /// </summary>
    public Block? Block
    {
        get => block;
        set
        {
            if (ReferenceEquals(this.block, value))
            {
                return;
            }

            this.block?.RemoveUse(this);
            this.block = value;
            this.block?.AddUse(this);
            Owner.InvalidateSyntax();
        }
    }

    /// <summary>
    /// Gets the block label, derived from the successor block when one is set, or from the original reference otherwise.
    /// </summary>
    public string Label => block?.Label ?? label;

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => Token.HasValue ? SourceLocation.FromToken(Token.Value) : SourceLocation.Unknown;
}
