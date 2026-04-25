namespace MLIR.Text;

/// <summary>
/// Represents a derived source document view whose spans resolve back to original source documents.
/// </summary>
/// <remarks>
/// The text of a source document view is the buffer consumed by the lexer/parser. Diagnostic
/// coordinates are resolved through <see cref="SourceDocument.ResolveSpan"/> so callers do not accidentally
/// report line/column values in generated, preprocessed, or otherwise transformed text.
/// </remarks>
public abstract class SourceDocumentView : SourceDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDocumentView"/> class.
    /// </summary>
    /// <param name="text">The derived text buffer consumed by the lexer/parser.</param>
    protected SourceDocumentView(string text)
        : base(text)
    {
    }

    /// <inheritdoc/>
    public sealed override SourcePosition GetLineColumn(int offset)
    {
        var primary = ResolveSpan(offset, 0).PrimarySpan;
        return primary.Document.GetLineColumn(primary.Start);
    }
}
