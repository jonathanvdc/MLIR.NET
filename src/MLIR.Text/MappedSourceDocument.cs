namespace MLIR.Text;

/// <summary>
/// Represents a derived source view whose spans resolve back to original source documents.
/// </summary>
/// <remarks>
/// The text of a mapped document is the buffer consumed by the lexer/parser. Diagnostic
/// coordinates are resolved through the configured <see cref="ISourceSpanMapper"/> so callers
/// do not accidentally report line/column values in generated or preprocessed text.
/// </remarks>
public sealed class MappedSourceDocument : SourceDocument
{
    private readonly ISourceSpanMapper mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedSourceDocument"/> class.
    /// </summary>
    /// <param name="text">The derived text buffer consumed by the lexer/parser.</param>
    /// <param name="mapper">The mapper used to resolve derived spans back to original source spans.</param>
    public MappedSourceDocument(string text, ISourceSpanMapper mapper)
        : base(text)
    {
        this.mapper = mapper;
    }

    /// <inheritdoc/>
    public override ResolvedSourceSpan ResolveSpan(int start, int length)
    {
        return mapper.Resolve(this, start, length);
    }

    /// <inheritdoc/>
    public override SourcePosition GetLineColumn(int offset)
    {
        var primary = ResolveSpan(offset, 0).PrimarySpan;
        return primary.Document.GetLineColumn(primary.Start);
    }
}
