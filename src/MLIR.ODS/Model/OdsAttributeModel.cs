namespace MLIR.ODS.Model;

/// <summary>
/// Represents an attribute description extracted from ODS.
/// </summary>
public sealed class OdsAttributeModel(string name)
{
    /// <summary>
    /// Gets the canonical attribute name.
    /// </summary>
    public string Name { get; } = name;
}
