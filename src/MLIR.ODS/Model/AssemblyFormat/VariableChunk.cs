namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A variable reference such as $operand, $attr, $region, or $result.
/// </summary>
public sealed class VariableChunk : Chunk
{
    /// <summary>
    /// The name of the referenced variable (without the leading '$').
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True when the variable is marked as the anchor of an optional group using ^.
    /// </summary>
    public bool IsAnchor { get; }

    /// <summary>
    /// Creates a variable reference.
    /// </summary>
    public VariableChunk(string name, bool isAnchor = false)
    {
        Name = name;
        IsAnchor = isAnchor;
    }
}
