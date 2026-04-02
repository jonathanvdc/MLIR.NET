namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents an SSA value use that could not be resolved to a definition.
/// </summary>
public sealed class UnresolvedValue : Value
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnresolvedValue"/> class from a syntax token.
    /// </summary>
    public UnresolvedValue(SyntaxToken token)
        : base(token)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnresolvedValue"/> class for a synthetic value.
    /// </summary>
    public UnresolvedValue(string name)
        : base(name)
    {
    }
}
