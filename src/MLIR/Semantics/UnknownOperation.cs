namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a bound operation whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownOperation : Operation
{
    private readonly string name;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownOperation"/> class.
    /// </summary>
    public UnknownOperation(
        OperationSyntax syntax,
        string name,
        Dialects.OperationDefinition? definition,
        IReadOnlyList<Region> regions,
        NamedAttributeCollection attributes,
        TypeReference? typeSignatureReference,
        IReadOnlyList<OperationResult> resultValues,
        IReadOnlyList<Value?> operandValues,
        IReadOnlyList<BlockReference> successorReferences)
        : base(syntax, regions, attributes, typeSignatureReference, resultValues, operandValues, successorReferences)
    {
        this.name = name;
        Definition = definition;
    }

    /// <inheritdoc />
    public override string Name => name;

    /// <inheritdoc />
    public override Dialects.OperationDefinition? Definition { get; }
}
