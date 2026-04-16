namespace MLIR.Dialects;

/// <summary>
/// Describes a concrete dialect-defined type (<c>TypeDef</c> in ODS).
/// </summary>
/// <remarks>
/// <para>
/// Concrete type definitions are also valid type constraints, so this class derives from
/// <see cref="TypeConstraintDefinition"/>.
/// </para>
/// <para>
/// A <see cref="TypeDefinition"/> carries registered metadata (canonical name and optional assembly
/// format) for a dialect type. Binding is driven by
/// <see cref="ITypeAssemblyFormat.Bind(MLIR.Syntax.TypeSyntax, TypeDefinition, MLIR.Semantics.Binder)"/>
/// when an assembly format is present. When no assembly format is registered, the binder falls back to
/// producing an <c>UnknownTypeReference</c> with the definition attached.
/// </para>
/// </remarks>
/// <param name="name">The canonical type name.</param>
/// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
public sealed class TypeDefinition(
    string name,
    ITypeAssemblyFormat? assemblyFormat = null)
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
}
