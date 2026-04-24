namespace MLIR.Semantics;

using MLIR.Text;

using MLIR.Syntax;

/// <summary>
/// Represents a successor use owned by an operation.
/// </summary>
public sealed class OpSuccessor
{
    private Block? block;

    internal OpSuccessor(Operation owner, int index, Block? block)
    {
        Owner = owner;
        Index = index;
        this.block = block;
        this.block?.AddUse(this);
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
    /// Gets the syntax token for the block label at the use site, derived from the owning operation's
    /// generic body syntax, or null if no such token is available.
    /// </summary>
    public Token? LabelToken
    {
        get
        {
            if (Owner.Syntax?.Body is GenericOperationBodySyntax body && Index < body.SuccessorList.Items.Count)
            {
                return body.SuccessorList.Items[Index];
            }

            return null;
        }
    }

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
    /// Gets the block label, derived from the successor block when one is set, or from the original
    /// source token otherwise.
    /// </summary>
    public string Label => block?.Label ?? LabelToken?.Text ?? string.Empty;

    /// <summary>
    /// Gets the use-site source location of the block label, if available from the owning operation's syntax.
    /// </summary>
    public SourceLocation Location
    {
        get
        {
            var token = LabelToken;
            return token.HasValue ? token.Value.Location : SourceLocation.Unknown;
        }
    }
}
