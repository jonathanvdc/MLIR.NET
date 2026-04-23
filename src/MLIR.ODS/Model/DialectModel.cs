namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an interpreted dialect description derived from TableGen.
/// </summary>
public sealed class DialectModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectModel"/> class.
    /// </summary>
    public DialectModel(
        string name,
        string? cppNamespace = null,
        string? summary = null,
        string? description = null,
        bool hasConstantMaterializer = false,
        IReadOnlyList<OperationModel>? operations = null,
        IReadOnlyList<AttributeModel>? attributes = null,
        IReadOnlyList<AttrModel>? attrs = null,
        IReadOnlyList<AttributeConstraintModel>? attributeConstraints = null,
        IReadOnlyList<TypeConstraintModel>? typeConstraints = null,
        IReadOnlyList<TypeModel>? types = null,
        bool isPrelude = false,
        IReadOnlyList<InterfaceModel>? interfaces = null)
    {
        Name = name;
        CppNamespace = cppNamespace;
        Summary = summary;
        Description = description;
        HasConstantMaterializer = hasConstantMaterializer;
        Operations = operations ?? EmptyOperations;
        Attributes = attributes ?? EmptyAttributes;
        Attrs = attrs ?? EmptyAttrs;
        AttributeConstraints = attributeConstraints ?? EmptyAttributeConstraints;
        TypeConstraints = typeConstraints ?? EmptyTypeConstraints;
        Types = types ?? EmptyTypes;
        IsPrelude = isPrelude;
        Interfaces = interfaces ?? EmptyInterfaces;
    }

    /// <summary>
    /// Creates the shared prelude dialect model that owns builtin constraints and types.
    /// </summary>
    public static DialectModel CreatePrelude(
        IReadOnlyList<AttributeConstraintModel> attributeConstraints,
        IReadOnlyList<AttrModel> attrs,
        IReadOnlyList<TypeConstraintModel> typeConstraints,
        IReadOnlyList<InterfaceModel>? interfaces = null)
    {
        return new DialectModel(
            "prelude",
            "::mlir::prelude",
            attributeConstraints: attributeConstraints,
            attrs: attrs,
            typeConstraints: typeConstraints,
            isPrelude: true,
            interfaces: interfaces);
    }

    /// <summary>
    /// Gets the canonical dialect namespace.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the dialect's C++ namespace, if known.
    /// </summary>
    public string? CppNamespace { get; }

    /// <summary>
    /// Gets the dialect summary, if known.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    /// Gets the dialect description, if known.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets a value indicating whether the dialect reports a constant materializer.
    /// </summary>
    public bool HasConstantMaterializer { get; }

    /// <summary>
    /// Gets the operation descriptions defined by the dialect.
    /// </summary>
    public IReadOnlyList<OperationModel> Operations { get; }

    /// <summary>
    /// Gets the concrete attribute definitions (<c>AttrDef</c>-style records) defined by the dialect.
    /// </summary>
    public IReadOnlyList<AttributeModel> Attributes { get; }

    /// <summary>
    /// Gets the upstream <c>Attr</c>-style descriptions available to generator code.
    /// These records are model data, not emitted C# attribute definitions.
    /// </summary>
    public IReadOnlyList<AttrModel> Attrs { get; }

    /// <summary>
    /// Gets the attribute constraint descriptions available to the dialect's generated code.
    /// </summary>
    public IReadOnlyList<AttributeConstraintModel> AttributeConstraints { get; }

    /// <summary>
    /// Gets the type constraint descriptions available to the dialect's generated code.
    /// </summary>
    public IReadOnlyList<TypeConstraintModel> TypeConstraints { get; }

    /// <summary>
    /// Gets the type descriptions defined by the dialect.
    /// </summary>
    public IReadOnlyList<TypeModel> Types { get; }

    /// <summary>
    /// Gets a value indicating whether this model represents the generated shared prelude.
    /// </summary>
    public bool IsPrelude { get; }

    /// <summary>
    /// Gets the interface definitions associated with this dialect.
    /// Interfaces are routed to a dialect based on their <c>cppNamespace</c>; unmatched
    /// interfaces are placed in the prelude.
    /// </summary>
    public IReadOnlyList<InterfaceModel> Interfaces { get; }

    private static readonly IReadOnlyList<OperationModel> EmptyOperations = new OperationModel[0];
    private static readonly IReadOnlyList<AttributeModel> EmptyAttributes = new AttributeModel[0];
    private static readonly IReadOnlyList<AttrModel> EmptyAttrs = new AttrModel[0];
    private static readonly IReadOnlyList<AttributeConstraintModel> EmptyAttributeConstraints = new AttributeConstraintModel[0];
    private static readonly IReadOnlyList<TypeConstraintModel> EmptyTypeConstraints = new TypeConstraintModel[0];
    private static readonly IReadOnlyList<TypeModel> EmptyTypes = new TypeModel[0];
    private static readonly IReadOnlyList<InterfaceModel> EmptyInterfaces = new InterfaceModel[0];
}
