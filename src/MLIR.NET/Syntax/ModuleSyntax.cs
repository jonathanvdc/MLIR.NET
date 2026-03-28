namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents the top-level generic MLIR concrete syntax tree.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModuleSyntax"/> class.
/// </remarks>
/// <param name="operations">The top-level operations in the module.</param>
/// <param name="endOfFileToken">The end-of-file token that carries any trailing trivia.</param>
public sealed class ModuleSyntax(IReadOnlyList<OperationSyntax> operations, SyntaxToken endOfFileToken)
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
    /// Gets the top-level operations in the module.
    /// </summary>
    public IReadOnlyList<OperationSyntax> Operations { get; } = operations;

    /// <summary>
    /// Gets the end-of-file token that carries trailing trivia.
    /// </summary>
    public SyntaxToken EndOfFileToken { get; } = endOfFileToken;

    internal void WriteTo(SyntaxWriter writer)
    {
        for (var i = 0; i < Operations.Count; i++)
        {
            writer.WriteOperation(Operations[i], 0, i > 0 ? "\n" : string.Empty);
        }

        writer.Write(EndOfFileToken.LeadingTrivia);
    }
}
