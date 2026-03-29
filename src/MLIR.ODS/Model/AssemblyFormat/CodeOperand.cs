namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A raw C++ expression/string literal passed to a custom directive parameter.
/// </summary>
public sealed class CodeOperand : DirectiveOperand
{
    /// <summary>
    /// The raw C++ code string.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Creates a code operand.
    /// </summary>
    public CodeOperand(string code)
    {
        Code = code;
    }
}
