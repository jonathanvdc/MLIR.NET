namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a bound operation whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownOperation : Operation
{
    private readonly IReadOnlyList<Region> regions;
    private readonly IReadOnlyList<NamedAttribute> attributes;
    private readonly TypeReference? typeSignatureReference;
    private readonly IReadOnlyList<ValueReference> resultValues;
    private readonly IReadOnlyList<ValueReference> operandValues;
    private readonly IReadOnlyList<BlockReference> successorReferences;
    private readonly IReadOnlyDictionary<string, object?> properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownOperation"/> class.
    /// </summary>
    public UnknownOperation(
        OperationSyntax syntax,
        string name,
        Dialects.OperationDefinition? definition,
        IReadOnlyList<Region> regions,
        IReadOnlyList<NamedAttribute> attributes,
        TypeReference? typeSignatureReference,
        IReadOnlyList<ValueReference> resultValues,
        IReadOnlyList<ValueReference> operandValues,
        IReadOnlyList<BlockReference> successorReferences,
        IReadOnlyDictionary<string, object?> properties)
        : base(
            syntax,
            name,
            definition)
    {
        this.regions = regions;
        this.attributes = attributes;
        this.typeSignatureReference = typeSignatureReference;
        this.resultValues = resultValues;
        this.operandValues = operandValues;
        this.successorReferences = successorReferences;
        this.properties = properties;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<Region> Regions => regions;

    /// <inheritdoc/>
    public override IReadOnlyList<NamedAttribute> Attributes => attributes;

    /// <inheritdoc/>
    public override TypeReference? TypeSignatureReference => typeSignatureReference;

    /// <inheritdoc/>
    public override IReadOnlyList<ValueReference> ResultValues => resultValues;

    /// <inheritdoc/>
    public override IReadOnlyList<ValueReference> OperandValues => operandValues;

    /// <inheritdoc/>
    public override IReadOnlyList<BlockReference> SuccessorReferences => successorReferences;

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, object?> Properties => properties;
}
