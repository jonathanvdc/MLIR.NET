namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a bound operation whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownOperation : OperationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownOperation"/> class.
    /// </summary>
    public UnknownOperation(
        OperationSyntax syntax,
        string name,
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
            null,
            regions,
            attributes,
            typeSignatureReference,
            resultValues,
            operandValues,
            successorReferences,
            properties)
    {
    }
}
