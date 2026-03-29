namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A nested type(...) directive used as an operand to another directive,
/// e.g. qualified(type($results)).
/// </summary>
public sealed class TypeDirectiveOperand : DirectiveOperand
{
    /// <summary>
    /// The operand passed to the nested type directive.
    /// </summary>
    public DirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a type directive operand.
    /// </summary>
    public TypeDirectiveOperand(DirectiveOperand operand)
    {
        Operand = operand;
    }
}
