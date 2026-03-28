namespace MLIR.ODS.Model;

/// <summary>
/// Represents a type description extracted from ODS.
/// </summary>
public sealed class OdsTypeModel(string name, string? className = null)
{
    /// <summary>
    /// Gets the canonical type name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the generated C# class name, if one was specified explicitly.
    /// </summary>
    public string? ClassName { get; } = className;
}
