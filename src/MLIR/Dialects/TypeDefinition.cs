namespace MLIR.Dialects;

using MLIR.Semantics;

/// <summary>
/// Describes a concrete dialect-defined type (<c>TypeDef</c> in ODS).
/// </summary>
/// <remarks>
/// Concrete type definitions are also valid type constraints, so this class derives from
/// <see cref="TypeConstraintDefinition"/>.
/// </remarks>
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
    : TypeConstraintDefinition(name, assemblyFormat)
{
    /// <summary>
    /// Gets the canonical type name.
    /// </summary>
    /// <remarks>
    /// This narrows <see cref="TypeConstraintDefinition.Name"/> back to a non-null contract for
    /// concrete <c>TypeDef</c> registrations.
    /// </remarks>
    public new string Name { get; } = name;

    /// <summary>
    /// Gets the typed type-reference factory.
    /// </summary>
    public System.Func<TypeReferenceConstructionContext, TypeReference> Factory { get; } =
        factory ?? (static context => new UnknownTypeReference(context.Syntax, context.Name, context.Definition, context.Location));
}
