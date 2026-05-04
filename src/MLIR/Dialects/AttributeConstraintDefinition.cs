namespace MLIR.Dialects;

using System;
using MLIR.Semantics;

/// <summary>
/// Describes a context-directed attribute constraint that can parse and bind attribute values.
/// </summary>
public class AttributeConstraintDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeConstraintDefinition"/> class.
    /// </summary>
    /// <param name="name">The logical constraint name, if one is known.</param>
    /// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
    /// <param name="assemblyFormatFactory">The optional custom assembly interpretation hook factory.</param>
    public AttributeConstraintDefinition(
        string? name = null,
        IAttributeAssemblyFormat? assemblyFormat = null,
        Func<AttributeConstraintDefinition, IAttributeAssemblyFormat>? assemblyFormatFactory = null)
    {
        Name = name;
        AssemblyFormat = assemblyFormat ?? assemblyFormatFactory?.Invoke(this);
    }

    /// <summary>
    /// Gets the logical constraint name, if one is known.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the custom assembly interpretation hook, if one is registered.
    /// </summary>
    public IAttributeAssemblyFormat? AssemblyFormat { get; }
}
