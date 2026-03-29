namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// type(input)
/// </summary>
public sealed class TypeDirectiveChunk : DirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public DirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public TypeDirectiveChunk(DirectiveOperand operand)
    {
        Operand = operand;
    }
}
