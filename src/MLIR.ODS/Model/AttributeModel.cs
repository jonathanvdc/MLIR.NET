namespace MLIR.ODS.Model;

/// <summary>
/// Represents an attribute description extracted from ODS.
/// </summary>
public sealed class AttributeModel(string name, string recordName, string? className = null)
{
    /// <summary>
    /// Gets the canonical attribute name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the originating ODS record name.
    /// </summary>
    public string RecordName { get; } = recordName;

    /// <summary>
    /// Gets the generated C# class name, if one was specified explicitly.
    /// </summary>
    public string? ClassName { get; } = className;
}
