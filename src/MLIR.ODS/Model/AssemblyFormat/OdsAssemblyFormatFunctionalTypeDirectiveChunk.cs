namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// functional-type(inputs, outputs)
/// </summary>
public sealed class OdsAssemblyFormatFunctionalTypeDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The input operands passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Inputs { get; }

    /// <summary>
    /// The output operands passed to the directive.
    /// </summary>
    public OdsAssemblyFormatDirectiveOperand Outputs { get; }

    /// <summary>
    /// Creates the directive.
    /// </summary>
    public OdsAssemblyFormatFunctionalTypeDirectiveChunk(
        OdsAssemblyFormatDirectiveOperand inputs,
        OdsAssemblyFormatDirectiveOperand outputs)
    {
        Inputs = inputs;
        Outputs = outputs;
    }
}
