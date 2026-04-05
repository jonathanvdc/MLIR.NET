namespace MLIR.Syntax;

/// <summary>
/// Represents the body of an MLIR operation.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should use
/// <see cref="Text.SyntaxWriter.IndentLevel"/> for indentation decisions and call
/// <see cref="Text.SyntaxWriter.WriteToken(SyntaxToken)"/> for the first token to
/// consume any pending suggested trivia.
/// </remarks>
public abstract class OperationBodySyntax : SyntaxNode
{
}
