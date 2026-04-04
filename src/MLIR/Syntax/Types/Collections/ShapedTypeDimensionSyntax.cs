using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents one dimension in a ranked builtin shaped type.
/// </summary>
public abstract class ShapedTypeDimensionSyntax
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

    /// <summary>
    /// Writes this dimension to the supplied syntax writer.
    /// </summary>
    public abstract void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia);
}
