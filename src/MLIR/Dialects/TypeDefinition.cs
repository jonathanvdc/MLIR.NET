namespace MLIR.Dialects;

using MLIR.Semantics;

/// <summary>
/// Describes a dialect-defined type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeDefinition"/> class.
/// </remarks>
/// <param name="name">The canonical type name.</param>
/// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
/// <param name="factory">The typed type-reference factory.</param>
public sealed class TypeDefinition(
    string name,
    ITypeAssemblyFormat? assemblyFormat = null,
    System.Func<TypeReferenceConstructionContext, TypeReference>? factory = null)
{
    /// <summary>
    /// Gets the canonical type name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the custom assembly interpretation hook, if one is registered.
    /// </summary>
    public ITypeAssemblyFormat? AssemblyFormat { get; } = assemblyFormat;

    /// <summary>
    /// Gets the typed type-reference factory.
    /// </summary>
    public System.Func<TypeReferenceConstructionContext, TypeReference> Factory { get; } =
        factory ?? (static context => new UnknownTypeReference(context.Syntax, context.Name, context.Definition, context.Location));
}
