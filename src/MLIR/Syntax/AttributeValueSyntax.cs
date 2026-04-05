using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Represents the syntax of an attribute value.
/// </summary>
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
    /// Writes the attribute value to the supplied syntax writer.
    /// </summary>
    public abstract void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        WriteTo(writer, defaultLeadingTrivia: string.Empty);
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
