namespace MLIR.Dialects;

using System;
using MLIR.Semantics;

/// <summary>
/// Describes a context-directed type constraint (<c>Type</c> in ODS) that can parse and bind type references.
/// </summary>
public class TypeConstraintDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeConstraintDefinition"/> class.
    /// </summary>
    /// <param name="name">The logical constraint name, if one is known.</param>
    /// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
    /// <param name="assemblyFormatFactory">The optional custom assembly interpretation hook factory.</param>
    public TypeConstraintDefinition(
        string? name = null,
        ITypeAssemblyFormat? assemblyFormat = null,
        Func<TypeConstraintDefinition, ITypeAssemblyFormat>? assemblyFormatFactory = null)
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
    public ITypeAssemblyFormat? AssemblyFormat { get; }
}
