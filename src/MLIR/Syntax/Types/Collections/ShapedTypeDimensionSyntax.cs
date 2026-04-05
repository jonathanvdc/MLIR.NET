using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents one dimension in a ranked builtin shaped type.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should call
/// <see cref="Text.SyntaxWriter.WriteToken(SyntaxToken)"/> for the first token to consume
/// any pending suggested trivia.
/// </remarks>
public abstract class ShapedTypeDimensionSyntax : SyntaxNode
{
    /// <summary>
    /// Attempts to project this dimension into preserved raw syntax text.
    /// </summary>
    public abstract bool TryGetRawText(out RawSyntaxText? rawText);

    /// <summary>
    /// Gets the preserved raw syntax text for this dimension.
    /// </summary>
    public RawSyntaxText GetRawText()
    {
        if (TryGetRawText(out var rawText))
        {
            return rawText!;
        }

        throw new InvalidOperationException("This shaped-type dimension does not provide a raw syntax-text projection.");
    }
}
