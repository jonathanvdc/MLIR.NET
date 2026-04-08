using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Represents the syntax of a type.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should call
/// <see cref="Text.SyntaxWriter.WriteToken(SyntaxToken)"/> for the first token to consume
/// any pending suggested trivia, and use explicit-trivia overloads for subsequent tokens.
/// </remarks>
public abstract class TypeSyntax : SyntaxNode
{
}
