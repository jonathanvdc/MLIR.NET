namespace MLIR.Dialects;

/// <summary>
/// Describes a dialect-defined type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeDefinition"/> class.
/// </remarks>
/// <param name="name">The canonical type name.</param>
/// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
public sealed class TypeDefinition(string name, ITypeAssemblyFormat? assemblyFormat = null)
{
    /// <summary>
    /// Gets the canonical type name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the custom assembly interpretation hook, if one is registered.
    /// </summary>
    public ITypeAssemblyFormat? AssemblyFormat { get; } = assemblyFormat;
}
