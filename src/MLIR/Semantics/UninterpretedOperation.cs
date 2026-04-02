using MLIR.Dialects;
using MLIR.Syntax;

namespace MLIR.Semantics;

/// <summary>
/// Represents a bound operation whose dialect-specific semantic type is unknown.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UninterpretedOperation"/> class.
/// </remarks>
public sealed class UninterpretedOperation(OperationSyntax syntax, string name) : Operation(syntax)
{
    /// <inheritdoc/>
    public override string Name { get; } = name;

    /// <inheritdoc/>
    public override OperationDefinition? Definition => null;

    /// <inheritdoc/>
    public override IReadOnlyList<Region> Regions => [];

    /// <inheritdoc/>
    public override NamedAttributeCollection Attributes => NamedAttributeCollection.Empty;

    /// <inheritdoc/>
    public override TypeReference? TypeSignatureReference => null;

    /// <inheritdoc/>
    public override IReadOnlyList<ValueReference> ResultValues => [];

    /// <inheritdoc/>
    public override IReadOnlyList<ValueReference> OperandValues => [];

    /// <inheritdoc/>
    public override IReadOnlyList<BlockReference> SuccessorReferences => [];

    /// <inheritdoc/>
    public override Operation RewriteChildren(SemanticRewriter rewriter) => this;
}
