namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A reference wrapper ref(...), typically passed to custom directives.
/// </summary>
public sealed class RefDirectiveOperand : DirectiveOperand
{
    /// <summary>
    /// The operand wrapped by the ref directive.
    /// </summary>
    public DirectiveOperand Operand { get; }

    /// <summary>
    /// Creates a ref directive operand.
    /// </summary>
    public RefDirectiveOperand(DirectiveOperand operand)
    {
        Operand = operand;
    }
}
