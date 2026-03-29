namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// functional-type(inputs, outputs)
/// </summary>
public sealed class FunctionalTypeDirectiveChunk : DirectiveChunk
{
    /// <summary>
    /// The input operands passed to the directive.
    /// </summary>
    public DirectiveOperand Inputs { get; }

    /// <summary>
    /// The output operands passed to the directive.
    /// </summary>
    public DirectiveOperand Outputs { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public FunctionalTypeDirectiveChunk(
        DirectiveOperand inputs,
        DirectiveOperand outputs)
    {
        Inputs = inputs;
        Outputs = outputs;
    }
}
