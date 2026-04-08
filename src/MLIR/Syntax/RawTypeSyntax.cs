using MLIR.Semantics;

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
    public override SourceLocation Location => RawText.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteRaw(RawText);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new RawTypeSyntax(rewriter.VisitRawText(RawText));
    }
}
