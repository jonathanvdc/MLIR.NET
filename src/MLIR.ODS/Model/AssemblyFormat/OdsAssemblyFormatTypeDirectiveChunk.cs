namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// type(input)
/// </summary>
public sealed class OdsAssemblyFormatTypeDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatTypeDirectiveChunk(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}
