namespace MLIR.Syntax;

/// <summary>
/// Represents a type preserved as raw syntax text.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RawTypeSyntax"/> class.
/// </remarks>
/// <param name="rawText">The preserved raw syntax text.</param>
public sealed class RawTypeSyntax(RawSyntaxText rawText) : TypeSyntax
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
