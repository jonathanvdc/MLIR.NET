namespace MLIR.Semantics;

/// <summary>
/// Represents an operand use owned by an operation.
/// </summary>
public sealed class OpOperand
{
    private Value? value;

    internal OpOperand(Operation owner, int index, Value? value)
    {
        Owner = owner;
        Index = index;
        this.value = value;
        this.value?.AddUse(this);
    }

    /// <summary>
    /// Gets the operation that owns this operand slot.
    /// </summary>
    public Operation Owner { get; }

    /// <summary>
    /// Gets the zero-based operand index within the owning operation.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets or sets the SSA value used by this operand slot.
    /// </summary>
    public Value? Value
    {
        get => value;
        set
        {
            if (ReferenceEquals(this.value, value))
            {
                return;
            }

            this.value?.RemoveUse(this);
            this.value = value;
            this.value?.AddUse(this);
            Owner.InvalidateSyntax();
        }
    }
}
