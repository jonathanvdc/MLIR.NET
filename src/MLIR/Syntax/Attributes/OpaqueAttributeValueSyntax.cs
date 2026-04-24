namespace MLIR.Syntax.Attributes;

using MLIR.Text;

using MLIR.Semantics;
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
    public override SourceLocation Location => RawText.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteRaw(RawText);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new OpaqueAttributeValueSyntax(rewriter.VisitRawText(RawText));
    }
}
