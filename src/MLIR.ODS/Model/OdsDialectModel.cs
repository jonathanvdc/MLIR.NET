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
        string? className = null,
        IReadOnlyList<OdsOperationModel>? operations = null,
        IReadOnlyList<OdsAttributeModel>? attributes = null,
        IReadOnlyList<OdsTypeModel>? types = null)
    {
        Name = name;
        ClassName = className;
        Operations = operations ?? EmptyOperations;
        Attributes = attributes ?? EmptyAttributes;
        Types = types ?? EmptyTypes;
    }

    /// <summary>
    /// Gets the canonical dialect namespace.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the generated registration class name, if the dialect definition specified one.
    /// </summary>
    public string? ClassName { get; }

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
