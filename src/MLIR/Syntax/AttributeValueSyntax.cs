using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Represents the syntax of an attribute value.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should call
/// <see cref="Text.SyntaxWriter.WriteToken(SyntaxToken)"/> for the first token to consume
/// any pending suggested trivia, and use explicit-trivia overloads for subsequent tokens.
/// </remarks>
public abstract class AttributeValueSyntax : SyntaxNode
{
    /// <summary>
    /// Attempts to project this value into preserved raw syntax text.
    /// </summary>
    public abstract bool TryGetRawText(out RawSyntaxText? rawText);

    /// <summary>
    /// Gets the preserved raw syntax text for this value.
    /// </summary>
    public RawSyntaxText GetRawText()
    {
        if (TryGetRawText(out var rawText))
        {
            return rawText!;
        }

        throw new System.InvalidOperationException("This attribute value does not provide a raw syntax-text projection.");
    }

    /// <summary>
    /// Gets the source location of this attribute value, if known.
    /// </summary>
    public virtual SourceLocation Location
    {
        get
        {
            if (TryGetRawText(out var rawText))
            {
                return rawText!.Location;
            }

            return SourceLocation.Unknown;
        }
    }
}
