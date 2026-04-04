using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Represents the syntax of a type.
/// </summary>
public abstract class TypeSyntax
{
    /// <summary>
    /// Attempts to project this type into preserved raw syntax text.
    /// </summary>
    public abstract bool TryGetRawText(out RawSyntaxText? rawText);

    /// <summary>
    /// Gets the preserved raw syntax text for this type.
    /// </summary>
    public RawSyntaxText GetRawText()
    {
        if (TryGetRawText(out var rawText))
        {
            return rawText!;
        }

        throw new System.InvalidOperationException("This type syntax does not provide a raw syntax-text projection.");
    }

    /// <summary>
    /// Writes the type syntax to the supplied syntax writer.
    /// </summary>
    public abstract void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia);

    /// <summary>
    /// Gets the source location of this type syntax, if available.
    /// </summary>
    public virtual SourceLocation Location
    {
        get
        {
            if (TryGetRawText(out var rawText) && rawText != null)
            {
                return rawText.Location;
            }
            else
            {
                return SourceLocation.Unknown;
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var writer = new Text.SyntaxWriter();
        WriteTo(writer, defaultLeadingTrivia: string.Empty);
        return writer.ToString().Trim();
    }
}
