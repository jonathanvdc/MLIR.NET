namespace MLIR.Dialects;

using MLIR.Semantics;

/// <summary>
/// Describes a context-directed type constraint (<c>Type</c> in ODS) that can parse and bind type references.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeConstraintDefinition"/> class.
/// </remarks>
/// <param name="name">The logical constraint name, if one is known.</param>
/// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
public class TypeConstraintDefinition(
    string? name = null,
    ITypeAssemblyFormat? assemblyFormat = null)
{
    /// <summary>
    /// Gets the logical constraint name, if one is known.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Gets the custom assembly interpretation hook, if one is registered.
    /// </summary>
    public ITypeAssemblyFormat? AssemblyFormat { get; } = assemblyFormat;
}
