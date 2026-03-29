namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A reference wrapper ref(...), typically passed to custom directives.
/// </summary>
public sealed class OdsAssemblyFormatRefDirectiveOperand : OdsAssemblyFormatDirectiveOperand
{
    /// <summary>
    /// The operand wrapped by the ref directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a ref directive operand.
    /// </summary>
    public OdsAssemblyFormatRefDirectiveOperand(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}
