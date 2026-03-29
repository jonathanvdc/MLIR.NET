namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// ref(input)
/// </summary>
public sealed class OdsAssemblyFormatRefDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatRefDirectiveChunk(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}
