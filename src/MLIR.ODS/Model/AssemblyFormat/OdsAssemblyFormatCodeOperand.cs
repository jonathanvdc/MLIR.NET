namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A raw C++ expression/string literal passed to a custom directive parameter.
/// </summary>
public sealed class OdsAssemblyFormatCodeOperand : OdsAssemblyFormatDirectiveOperand
{
    /// <summary>
    /// The raw C++ code string.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Creates a code operand.
    /// </summary>
    public OdsAssemblyFormatCodeOperand(string code)
    {
        Code = code;
    }
}
