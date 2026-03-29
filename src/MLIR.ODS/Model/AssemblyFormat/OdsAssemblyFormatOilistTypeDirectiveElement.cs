namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A type directive element in an oilist clause.
/// </summary>
public sealed class OdsAssemblyFormatOilistTypeDirectiveElement : OdsAssemblyFormatOilistElement
{
    /// <summary>
    /// The operand passed to the type directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a type directive oilist element.
    /// </summary>
    public OdsAssemblyFormatOilistTypeDirectiveElement(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}
