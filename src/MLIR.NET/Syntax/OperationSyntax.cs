namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a generic MLIR operation.
/// </summary>
public sealed class OperationSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationSyntax"/> class.
    /// </summary>
    /// <param name="results">The SSA results produced by the operation.</param>
    /// <param name="name">The operation name as written in the source.</param>
    /// <param name="operands">The SSA operands passed to the operation.</param>
    /// <param name="successors">The successor block labels referenced by the operation.</param>
    /// <param name="regions">The regions nested under the operation.</param>
    /// <param name="attributes">The named attributes attached to the operation.</param>
    /// <param name="typeSignature">The raw trailing type signature, if present.</param>
    public OperationSyntax(
        IReadOnlyList<string> results,
        string name,
        IReadOnlyList<string> operands,
        IReadOnlyList<string> successors,
        IReadOnlyList<RegionSyntax> regions,
        IReadOnlyList<NamedAttributeSyntax> attributes,
        RawSyntaxText? typeSignature)
    {
        Results = results;
        Name = name;
        Operands = operands;
        Successors = successors;
        Regions = regions;
        Attributes = attributes;
        TypeSignature = typeSignature;
    }

    /// <summary>
    /// Gets the SSA results produced by the operation.
    /// </summary>
    public IReadOnlyList<string> Results { get; }

    /// <summary>
    /// Gets the operation name as written in the source.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the SSA operands passed to the operation.
    /// </summary>
    public IReadOnlyList<string> Operands { get; }

    /// <summary>
    /// Gets the successor block labels referenced by the operation.
    /// </summary>
    public IReadOnlyList<string> Successors { get; }

    /// <summary>
    /// Gets the regions nested under the operation.
    /// </summary>
    public IReadOnlyList<RegionSyntax> Regions { get; }

    /// <summary>
    /// Gets the named attributes attached to the operation.
    /// </summary>
    public IReadOnlyList<NamedAttributeSyntax> Attributes { get; }

    /// <summary>
    /// Gets the raw trailing type signature, if present.
    /// </summary>
    public RawSyntaxText? TypeSignature { get; }
}
