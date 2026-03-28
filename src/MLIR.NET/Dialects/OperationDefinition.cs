namespace MLIR.Dialects;

using System.Collections.Generic;

/// <summary>
/// Describes the semantic behavior of a dialect operation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OperationDefinition"/> class.
/// </remarks>
/// <param name="name">The canonical operation name without MLIR string-literal quoting.</param>
/// <param name="operandDefinitions">The declarative operand segments, if available.</param>
/// <param name="resultDefinitions">The declarative result segments, if available.</param>
/// <param name="regionDefinitions">The declarative region segments, if available.</param>
/// <param name="successorDefinitions">The declarative successor segments, if available.</param>
/// <param name="attributeDefinitions">The declarative attribute definitions, if available.</param>
/// <param name="operandCount">The exact number of operands expected by the operation, if constrained.</param>
/// <param name="resultCount">The exact number of results expected by the operation, if constrained.</param>
/// <param name="regionCount">The exact number of regions expected by the operation, if constrained.</param>
/// <param name="successorCount">The exact number of successors expected by the operation, if constrained.</param>
/// <param name="requiredAttributes">The attribute names that must be present on the operation.</param>
/// <param name="verifier">The optional verifier for the operation.</param>
/// <param name="assemblyFormat">The optional custom assembly format.</param>
public sealed class OperationDefinition(
    string name,
    IReadOnlyList<OperationSegmentDefinition>? operandDefinitions = null,
    IReadOnlyList<OperationSegmentDefinition>? resultDefinitions = null,
    IReadOnlyList<OperationSegmentDefinition>? regionDefinitions = null,
    IReadOnlyList<OperationSegmentDefinition>? successorDefinitions = null,
    IReadOnlyList<AttributeDefinition>? attributeDefinitions = null,
    int? operandCount = null,
    int? resultCount = null,
    int? regionCount = null,
    int? successorCount = null,
    IReadOnlyList<string>? requiredAttributes = null,
    IOperationVerifier? verifier = null,
    IOperationAssemblyFormat? assemblyFormat = null)
{
    /// <summary>
    /// Gets the canonical operation name without MLIR string-literal quoting.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the declarative operand segments, if available.
    /// </summary>
    public IReadOnlyList<OperationSegmentDefinition> OperandDefinitions { get; } = operandDefinitions ?? EmptySegmentDefinitions;

    /// <summary>
    /// Gets the declarative result segments, if available.
    /// </summary>
    public IReadOnlyList<OperationSegmentDefinition> ResultDefinitions { get; } = resultDefinitions ?? EmptySegmentDefinitions;

    /// <summary>
    /// Gets the declarative region segments, if available.
    /// </summary>
    public IReadOnlyList<OperationSegmentDefinition> RegionDefinitions { get; } = regionDefinitions ?? EmptySegmentDefinitions;

    /// <summary>
    /// Gets the declarative successor segments, if available.
    /// </summary>
    public IReadOnlyList<OperationSegmentDefinition> SuccessorDefinitions { get; } = successorDefinitions ?? EmptySegmentDefinitions;

    /// <summary>
    /// Gets the declarative attribute definitions, if available.
    /// </summary>
    public IReadOnlyList<AttributeDefinition> AttributeDefinitions { get; } = attributeDefinitions ?? EmptyAttributeDefinitions;

    /// <summary>
    /// Gets the exact number of operands expected by the operation, if constrained.
    /// </summary>
    public int? OperandCount { get; } = operandCount;

    /// <summary>
    /// Gets the exact number of results expected by the operation, if constrained.
    /// </summary>
    public int? ResultCount { get; } = resultCount;

    /// <summary>
    /// Gets the exact number of regions expected by the operation, if constrained.
    /// </summary>
    public int? RegionCount { get; } = regionCount;

    /// <summary>
    /// Gets the exact number of successors expected by the operation, if constrained.
    /// </summary>
    public int? SuccessorCount { get; } = successorCount;

    /// <summary>
    /// Gets the attribute names that must be present on the operation.
    /// </summary>
    public IReadOnlyList<string> RequiredAttributes { get; } = requiredAttributes ?? EmptyRequiredAttributes;

    /// <summary>
    /// Gets the verifier for the operation, if one is registered.
    /// </summary>
    public IOperationVerifier? Verifier { get; } = verifier;

    /// <summary>
    /// Gets the custom assembly format for the operation, if one is registered.
    /// </summary>
    public IOperationAssemblyFormat? AssemblyFormat { get; } = assemblyFormat;

    private static readonly IReadOnlyList<OperationSegmentDefinition> EmptySegmentDefinitions = new OperationSegmentDefinition[0];
    private static readonly IReadOnlyList<AttributeDefinition> EmptyAttributeDefinitions = new AttributeDefinition[0];
    private static readonly IReadOnlyList<string> EmptyRequiredAttributes = new string[0];
}
