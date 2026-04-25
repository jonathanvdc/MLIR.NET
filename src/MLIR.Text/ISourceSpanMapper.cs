namespace MLIR.Text;

/// <summary>
/// Maps spans in a derived source document back to spans in original source documents.
/// </summary>
/// <remarks>
/// Mappers deal only in spans. Line, column, and file-name computation remains the
/// responsibility of <see cref="OriginalSourceDocument"/>.
/// </remarks>
public interface ISourceSpanMapper
{
    /// <summary>
    /// Resolves a span in <paramref name="document"/> to original source spans.
    /// </summary>
    /// <param name="document">The mapped document containing the span.</param>
    /// <param name="start">The zero-based start offset within <paramref name="document"/>.</param>
    /// <param name="length">The span length within <paramref name="document"/>.</param>
    /// <returns>The original-source mapping for the span.</returns>
    ResolvedSourceSpan Resolve(SourceDocument document, int start, int length);
}
