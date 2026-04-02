namespace MLIR.Syntax.Attributes;

using MLIR.Syntax;

/// <summary>
/// Represents an attribute value preserved as structured-but-opaque raw syntax text.
/// </summary>
public sealed class OpaqueAttributeValueSyntax(RawSyntaxText rawText) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the preserved raw syntax text.
    /// </summary>
    public RawSyntaxText RawText { get; } = rawText;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = RawText;
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteRaw(RawText, defaultLeadingTrivia);
    }
}
