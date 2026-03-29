namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// ref(input)
/// </summary>
public sealed class RefDirectiveChunk : DirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public DirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public RefDirectiveChunk(DirectiveOperand operand)
    {
        Operand = operand;
    }
}
