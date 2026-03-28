namespace MLIR.Dialects;

using System.Collections.Generic;
using MLIR.Semantics;

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
/// <param name="attributeDefinitions">The declarative operation attribute definitions, if available.</param>
/// <param name="requiredAttributes">The attribute names that must be present on the operation.</param>
/// <param name="verifier">The optional verifier for the operation.</param>
/// <param name="assemblyFormat">The optional custom assembly format.</param>
/// <param name="factory">The optional typed operation factory.</param>
public sealed class OperationDefinition(
    string name,
    IReadOnlyList<OperationSegmentDefinition>? operandDefinitions = null,
    IReadOnlyList<OperationSegmentDefinition>? resultDefinitions = null,
    IReadOnlyList<OperationSegmentDefinition>? regionDefinitions = null,
    IReadOnlyList<OperationSegmentDefinition>? successorDefinitions = null,
    IReadOnlyList<OperationAttributeDefinition>? attributeDefinitions = null,
    IReadOnlyList<string>? requiredAttributes = null,
    IOperationVerifier? verifier = null,
    IOperationAssemblyFormat? assemblyFormat = null,
    System.Func<OperationConstructionContext, Operation>? factory = null)
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
    public IReadOnlyList<OperationAttributeDefinition> AttributeDefinitions { get; } = attributeDefinitions ?? EmptyAttributeDefinitions;

    /// <summary>
    /// Gets the exact number of operands expected by the operation, if constrained.
    /// </summary>
    public int? OperandCount => InferExactCount(OperandDefinitions);

    /// <summary>
    /// Gets the exact number of results expected by the operation, if constrained.
    /// </summary>
    public int? ResultCount => InferExactCount(ResultDefinitions);

    /// <summary>
    /// Gets the exact number of regions expected by the operation, if constrained.
    /// </summary>
    public int? RegionCount => InferExactCount(RegionDefinitions);

    /// <summary>
    /// Gets the exact number of successors expected by the operation, if constrained.
    /// </summary>
    public int? SuccessorCount => InferExactCount(SuccessorDefinitions);

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

    /// <summary>
    /// Gets the typed operation factory for the operation, if one is registered.
    /// </summary>
    public System.Func<OperationConstructionContext, Operation> Factory { get; } = factory ?? CreateDefaultFactory();

    private static readonly IReadOnlyList<OperationSegmentDefinition> EmptySegmentDefinitions = new OperationSegmentDefinition[0];
    private static readonly IReadOnlyList<OperationAttributeDefinition> EmptyAttributeDefinitions = new OperationAttributeDefinition[0];
    private static readonly IReadOnlyList<string> EmptyRequiredAttributes = new string[0];

    private static int? InferExactCount(IReadOnlyList<OperationSegmentDefinition> definitions)
    {
        if (definitions.Count == 0)
        {
            return null;
        }

        foreach (var definition in definitions)
        {
            if (definition.IsVariadic)
            {
                return null;
            }
        }

        return definitions.Count;
    }

    private static System.Func<OperationConstructionContext, Operation> CreateDefaultFactory()
    {
        return static context => new UnknownOperation(
            context.Syntax,
            context.Name,
            context.Definition,
            context.Regions,
            context.Attributes,
            context.TypeSignatureReference,
            context.ResultValues,
            context.OperandValues,
            context.SuccessorReferences);
    }
}
