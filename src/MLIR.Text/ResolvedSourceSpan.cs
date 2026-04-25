namespace MLIR.Text;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents the original source coverage for a span in a parsed source document.
/// </summary>
/// <remarks>
/// A contiguous span in a mapped document may originate from several disjoint spans, and
/// eventually from several source files. <see cref="PrimarySpan"/> is the single best span
/// for classic diagnostic display, while <see cref="OriginSpans"/> preserves the complete
/// original-source coverage for richer diagnostics.
/// </remarks>
public sealed class ResolvedSourceSpan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolvedSourceSpan"/> class.
    /// </summary>
    /// <param name="primarySpan">The primary original source span for diagnostic display.</param>
    /// <param name="originSpans">All original source spans covered by the resolved span.</param>
    public ResolvedSourceSpan(OriginalSourceSpan primarySpan, IReadOnlyList<OriginalSourceSpan> originSpans)
    {
        PrimarySpan = primarySpan;
        OriginSpans = originSpans ?? Array.Empty<OriginalSourceSpan>();
    }

    /// <summary>
    /// Gets the best single original source span for classic diagnostic display.
    /// </summary>
    public OriginalSourceSpan PrimarySpan { get; }

    /// <summary>
    /// Gets all original source spans covered by the resolved span.
    /// </summary>
    public IReadOnlyList<OriginalSourceSpan> OriginSpans { get; }
}
