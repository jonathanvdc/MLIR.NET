namespace MLIR.Dialects;

using MLIR.Semantics;

/// <summary>
/// Describes a context-directed attribute constraint that can parse and bind attribute values.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AttributeConstraintDefinition"/> class.
/// </remarks>
/// <param name="name">The logical constraint name, if one is known.</param>
/// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
public class AttributeConstraintDefinition(
    string? name = null,
    IAttributeAssemblyFormat? assemblyFormat = null)
{
    /// <summary>
    /// Gets the logical constraint name, if one is known.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Gets the custom assembly interpretation hook, if one is registered.
    /// </summary>
    public IAttributeAssemblyFormat? AssemblyFormat { get; } = assemblyFormat;
}
