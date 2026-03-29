namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A variable reference used as a directive operand.
/// </summary>
public sealed class VariableOperand : DirectiveOperand
{
    /// <summary>
    /// The name of the referenced variable.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a variable operand.
    /// </summary>
    public VariableOperand(string name)
    {
        Name = name;
    }
}
