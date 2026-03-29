namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// qualified(input)
/// </summary>
public sealed class QualifiedDirectiveChunk : DirectiveChunk
{
    /// <summary>
    /// The operand passed to the directive.
    /// </summary>
    public DirectiveOperand Operand { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public QualifiedDirectiveChunk(DirectiveOperand operand)
    {
        Operand = operand;
    }
}
