namespace MLIR.Syntax;

using MLIR.Text;

using MLIR.Semantics;

/// <summary>
/// Represents the body of an MLIR operation.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should use
/// <see cref="Text.SyntaxWriter.IndentLevel"/> for indentation decisions and call
/// <see cref="Text.SyntaxWriter.WriteToken(Token)"/> for the first token to
/// consume any pending suggested trivia.
/// </remarks>
public abstract class OperationBodySyntax : SyntaxNode
{
    /// <summary>
    /// Gets the merged source location spanning this operation body, or
    /// <see cref="SourceLocation.Unknown"/> when no source-backed tokens are present.
    /// </summary>
    /// <remarks>
    /// Overriding classes should compute this by merging the locations of all their
    /// contributing tokens and subtrees. The default implementation returns
    /// <see cref="SourceLocation.Unknown"/> for bodies that have not yet been updated.
    /// </remarks>
    public override SourceLocation Location => SourceLocation.Unknown;
}
