namespace MLIR.Dialects;

/// <summary>
/// Describes a dialect-defined attribute value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AttributeDefinition"/> class.
/// </remarks>
/// <param name="name">The canonical attribute name.</param>
/// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
public sealed class AttributeDefinition(string name, IAttributeAssemblyFormat? assemblyFormat = null)
{
    /// <summary>
    /// Gets the canonical attribute name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the custom assembly interpretation hook, if one is registered.
    /// </summary>
    public IAttributeAssemblyFormat? AssemblyFormat { get; } = assemblyFormat;
}
