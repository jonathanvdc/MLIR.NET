namespace MLIR.ODS.Model;

using System.Collections.Generic;

/// <summary>
/// Represents an operation description extracted from ODS.
/// </summary>
public sealed class OdsOperationModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OdsOperationModel"/> class.
    /// </summary>
    public OdsOperationModel(
        string name,
        string? className = null,
        IReadOnlyList<string>? operands = null,
        IReadOnlyList<string>? results = null,
        IReadOnlyList<string>? attributes = null,
        bool hasCustomAssemblyFormat = false)
    {
        Name = name;
        ClassName = className;
        Operands = operands ?? EmptyItems;
        Results = results ?? EmptyItems;
        Attributes = attributes ?? EmptyItems;
        HasCustomAssemblyFormat = hasCustomAssemblyFormat;
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

    private static readonly IReadOnlyList<string> EmptyItems = new string[0];
}
