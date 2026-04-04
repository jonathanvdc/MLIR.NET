namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an operation description extracted from ODS.
/// </summary>
public sealed class OperationModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationModel"/> class.
    /// </summary>
    public OperationModel(
        string name,
        string? className = null,
        IReadOnlyList<OperandModel>? operands = null,
        IReadOnlyList<ResultModel>? results = null,
        IReadOnlyList<AttributeUseModel>? attributes = null,
        string? summary = null,
        string? description = null,
        AssemblyFormatModel? assemblyFormat = null,
        IReadOnlyList<string>? traits = null,
        string? assemblyExtensionKind = null)
    {
        Name = name;
        ClassName = className;
        Operands = operands ?? EmptyOperands;
        Results = results ?? EmptyResults;
        Attributes = attributes ?? EmptyAttributes;
        Summary = summary;
        Description = description;
        AssemblyFormat = assemblyFormat;
        Traits = traits ?? EmptyTraits;
        AssemblyFormatCode = assemblyExtensionKind;
    }

    /// <summary>
    /// Gets the canonical operation name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the generated C# class name, if one was specified explicitly.
    /// </summary>
    public string? ClassName { get; }

    /// <summary>
    /// Gets the declared operands.
    /// </summary>
    public IReadOnlyList<OperandModel> Operands { get; }

    /// <summary>
    /// Gets the declared results.
    /// </summary>
    public IReadOnlyList<ResultModel> Results { get; }

    /// <summary>
    /// Gets the declared attribute uses.
    /// </summary>
    public IReadOnlyList<AttributeUseModel> Attributes { get; }

    /// <summary>
    /// Gets the operation summary, if known.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    /// Gets the operation description, if known.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the declarative assembly format, if known.
    /// </summary>
    public AssemblyFormatModel? AssemblyFormat { get; }

    /// <summary>
    /// Gets the declared trait names.
    /// </summary>
    public IReadOnlyList<string> Traits { get; }

    /// <summary>
    /// Gets C# code for any custom assembly format, if specified.
    /// </summary>
    public string? AssemblyFormatCode { get; }

    private static readonly IReadOnlyList<string> EmptyTraits = new string[0];
    private static readonly IReadOnlyList<OperandModel> EmptyOperands = new OperandModel[0];
    private static readonly IReadOnlyList<ResultModel> EmptyResults = new ResultModel[0];
    private static readonly IReadOnlyList<AttributeUseModel> EmptyAttributes = new AttributeUseModel[0];
}
