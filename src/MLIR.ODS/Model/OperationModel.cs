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
        IReadOnlyList<string>? operands = null,
        IReadOnlyList<string>? results = null,
        IReadOnlyList<string>? attributes = null,
        bool hasCustomAssemblyFormat = false,
        string? summary = null,
        string? description = null,
        AssemblyFormatModel? assemblyFormat = null,
        IReadOnlyList<string>? traits = null)
    {
        Name = name;
        ClassName = className;
        Operands = operands ?? EmptyItems;
        Results = results ?? EmptyItems;
        Attributes = attributes ?? EmptyItems;
        HasCustomAssemblyFormat = hasCustomAssemblyFormat;
        Summary = summary;
        Description = description;
        AssemblyFormat = assemblyFormat;
        Traits = traits ?? EmptyItems;
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
    /// Gets the declared operand segment names.
    /// </summary>
    public IReadOnlyList<string> Operands { get; }

    /// <summary>
    /// Gets the declared result segment names.
    /// </summary>
    public IReadOnlyList<string> Results { get; }

    /// <summary>
    /// Gets the declared attribute names.
    /// </summary>
    public IReadOnlyList<string> Attributes { get; }

    /// <summary>
    /// Gets a value indicating whether the operation declares a custom assembly format.
    /// </summary>
    public bool HasCustomAssemblyFormat { get; }

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

    private static readonly IReadOnlyList<string> EmptyItems = new string[0];
}
