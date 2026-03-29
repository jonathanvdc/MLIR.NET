namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A nested type(...) directive used as an operand to another directive,
/// e.g. qualified(type($results)).
/// </summary>
public sealed class OdsAssemblyFormatTypeDirectiveOperand : OdsAssemblyFormatDirectiveOperand
{
    /// <summary>
    /// The operand passed to the nested type directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a type directive operand.
    /// </summary>
    public OdsAssemblyFormatTypeDirectiveOperand(OdsAssemblyFormatDirectiveOperand operand)
    {
        Operand = operand;
    }
}
