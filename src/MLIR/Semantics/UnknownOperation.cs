namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a bound operation whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownOperation : Operation
{
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
        : base(syntax, name, definition, regions, attributes, typeSignatureReference, resultValues, operandValues, successorReferences)
    {
    }
}
