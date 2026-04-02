using MLIR.Dialects;
using MLIR.Syntax;

namespace MLIR.Semantics;

/// <summary>
/// Represents a bound operation whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UninterpretedOperation : Operation
{
    private readonly string name;

    /// <summary>
    /// Initializes a new instance of the <see cref="UninterpretedOperation"/> class.
    /// </summary>
    public UninterpretedOperation(OperationSyntax syntax, string name)
        : base(syntax)
    {
        this.name = name;
    }

    /// <inheritdoc />
    public override string Name => name;

    /// <inheritdoc />
    public override OperationDefinition? Definition => null;
}
