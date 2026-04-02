namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a semantic SSA value defined by an operation result.
/// </summary>
public sealed class OperationResult : Value
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult"/> class from a syntax token.
    /// </summary>
    public OperationResult(SyntaxToken token)
        : base(token)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult"/> class for a synthetic value.
    /// </summary>
    public OperationResult(string name)
        : base(name)
    {
    }

    /// <summary>
    /// Gets the defining operation for this result.
    /// </summary>
    public Operation? DefiningOperation { get; private set; }

    /// <summary>
    /// Gets the zero-based result index within the defining operation.
    /// </summary>
    public int ResultIndex { get; private set; } = -1;

    internal void Bind(Operation definingOperation, int resultIndex)
    {
        DefiningOperation = definingOperation;
        ResultIndex = resultIndex;
    }
}
