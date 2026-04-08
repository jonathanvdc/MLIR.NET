namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents the top-level generic MLIR concrete syntax tree.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModuleSyntax"/> class.
/// </remarks>
/// <param name="operations">The top-level operations in the module.</param>
/// <param name="endOfFileToken">The end-of-file token that carries any trailing trivia.</param>
public sealed class ModuleSyntax(IReadOnlyList<OperationSyntax> operations, Token endOfFileToken) : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSyntax"/> class.
    /// </summary>
    /// <param name="operations">The top-level operations in the module.</param>
    public ModuleSyntax(IReadOnlyList<OperationSyntax> operations)
        : this(operations, TokenFactory.EndOfFile())
    {
    }

    /// <summary>
    /// Gets the top-level operations in the module.
    /// </summary>
    public IReadOnlyList<OperationSyntax> Operations { get; } = operations;

    /// <summary>
    /// Gets the end-of-file token that carries trailing trivia.
    /// </summary>
    public Token EndOfFileToken { get; } = endOfFileToken;

    /// <inheritdoc/>
    public override SourceLocation Location
    {
        get
        {
            if (Operations.Count > 0)
            {
                var firstOpLocation = Operations[0].Location;
                var lastOpLocation = Operations[Operations.Count - 1].Location;
                return SourceLocation.Merge(firstOpLocation, lastOpLocation);
            }
            else
            {
                return EndOfFileToken.Location;
            }
        }
    }

    /// <summary>
    /// Writes this module to the supplied syntax writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    public override void WriteTo(SyntaxWriter writer)
    {
        writer.IndentLevel = 0;
        for (var i = 0; i < Operations.Count; i++)
        {
            if (i > 0)
            {
                writer.SuggestTrivia("\n");
            }

            writer.WriteOperation(Operations[i]);
        }

        writer.Write(EndOfFileToken.LeadingTrivia ?? string.Empty);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new ModuleSyntax(
            rewriter.VisitList(Operations),
            rewriter.VisitToken(EndOfFileToken));
    }
}
