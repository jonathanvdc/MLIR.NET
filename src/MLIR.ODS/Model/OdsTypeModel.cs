namespace MLIR.ODS.Model;

/// <summary>
/// Represents a type description extracted from ODS.
/// </summary>
public sealed class OdsTypeModel(string name)
{
    /// <summary>
    /// Gets the canonical type name.
    /// </summary>
    public string Name { get; } = name;
}
