namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A type directive element in an oilist clause.
/// </summary>
public sealed class OilistTypeDirectiveElement : OilistElement
{
    /// <summary>
    /// The operand passed to the type directive.
    /// </summary>
    public DirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a type directive oilist element.
    /// </summary>
    public OilistTypeDirectiveElement(DirectiveOperand operand)
    {
        Operand = operand;
    }
}
