namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A variable element in an oilist clause.
/// </summary>
public sealed class OilistVariableElement : OilistElement
{
    /// <summary>
    /// The name of the referenced variable.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a variable oilist element.
    /// </summary>
    public OilistVariableElement(string name)
    {
        Name = name;
    }
}
