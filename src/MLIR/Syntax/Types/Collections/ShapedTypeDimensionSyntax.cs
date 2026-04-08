using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents one dimension in a ranked builtin shaped type.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should call
/// <see cref="Text.SyntaxWriter.WriteToken(Token)"/> for the first token to consume
/// any pending suggested trivia.
/// </remarks>
public abstract class ShapedTypeDimensionSyntax : SyntaxNode
{
}
