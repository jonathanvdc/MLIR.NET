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
        IReadOnlyList<TypeModel>? types = null)
    {
        Name = name;
        CppNamespace = cppNamespace;
        Summary = summary;
        Description = description;
        HasConstantMaterializer = hasConstantMaterializer;
        Operations = operations ?? EmptyOperations;
        Attributes = attributes ?? EmptyAttributes;
        Types = types ?? EmptyTypes;
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
    /// Gets the attribute descriptions defined by the dialect.
    /// </summary>
    public IReadOnlyList<AttributeModel> Attributes { get; }

    /// <summary>
    /// Gets the type descriptions defined by the dialect.
    /// </summary>
    public IReadOnlyList<TypeModel> Types { get; }

    private static readonly IReadOnlyList<OperationModel> EmptyOperations = new OperationModel[0];
    private static readonly IReadOnlyList<AttributeModel> EmptyAttributes = new AttributeModel[0];
    private static readonly IReadOnlyList<TypeModel> EmptyTypes = new TypeModel[0];
}
