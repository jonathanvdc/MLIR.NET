namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// qualified(input)
/// </summary>
public sealed class OdsAssemblyFormatQualifiedDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatQualifiedDirectiveChunk(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}
