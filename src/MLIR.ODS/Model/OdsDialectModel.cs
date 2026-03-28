namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an interpreted dialect description derived from TableGen.
/// </summary>
public sealed class OdsDialectModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OdsDialectModel"/> class.
    /// </summary>
    public OdsDialectModel(
        string name,
        string? cppNamespace = null,
        string? summary = null,
        string? description = null,
        bool hasConstantMaterializer = false,
        IReadOnlyList<OdsOperationModel>? operations = null,
        IReadOnlyList<OdsAttributeModel>? attributes = null,
        IReadOnlyList<OdsTypeModel>? types = null)
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
    public IReadOnlyList<OdsOperationModel> Operations { get; }

    /// <summary>
    /// Gets the attribute descriptions defined by the dialect.
    /// </summary>
    public IReadOnlyList<OdsAttributeModel> Attributes { get; }

    /// <summary>
    /// Gets the type descriptions defined by the dialect.
    /// </summary>
    public IReadOnlyList<OdsTypeModel> Types { get; }

    private static readonly IReadOnlyList<OdsOperationModel> EmptyOperations = new OdsOperationModel[0];
    private static readonly IReadOnlyList<OdsAttributeModel> EmptyAttributes = new OdsAttributeModel[0];
    private static readonly IReadOnlyList<OdsTypeModel> EmptyTypes = new OdsTypeModel[0];
}
