namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents the top-level generic MLIR concrete syntax tree.
/// </summary>
public sealed class ModuleSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSyntax"/> class.
    /// </summary>
    /// <param name="operations">The top-level operations in the module.</param>
    public ModuleSyntax(IReadOnlyList<OperationSyntax> operations)
        : this(operations, new SyntaxToken(string.Empty))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSyntax"/> class.
    /// </summary>
    /// <param name="operations">The top-level operations in the module.</param>
    /// <param name="endOfFileToken">The end-of-file token that carries any trailing trivia.</param>
    public ModuleSyntax(IReadOnlyList<OperationSyntax> operations, SyntaxToken endOfFileToken)
    {
        Operations = operations;
        EndOfFileToken = endOfFileToken;
    }

    /// <summary>
    /// Gets the top-level operations in the module.
    /// </summary>
    public IReadOnlyList<OperationSyntax> Operations { get; }

    /// <summary>
    /// Gets the end-of-file token that carries trailing trivia.
    /// </summary>
    public SyntaxToken EndOfFileToken { get; }
}
