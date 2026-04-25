namespace TableGen;

using System;
using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents TableGen preprocessor output as a source document view over original source text.
/// </summary>
public sealed class PreprocessedSourceDocument : SourceDocumentView
{
    private readonly IReadOnlyList<PreprocessedSourceMapSegment> segments;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreprocessedSourceDocument"/> class.
    /// </summary>
    /// <param name="text">The preprocessed text consumed by the parser.</param>
    /// <param name="segments">The source-map segments from preprocessed text back to original source spans.</param>
    internal PreprocessedSourceDocument(string text, IReadOnlyList<PreprocessedSourceMapSegment> segments)
        : base(text)
    {
        this.segments = segments;
    }

    /// <inheritdoc/>
    public override ResolvedSourceSpan ResolveSpan(int start, int length)
    {
        if (segments.Count == 0)
        {
            var emptyDocument = new OriginalSourceDocument(string.Empty);
            var emptySpan = new OriginalSourceSpan(emptyDocument, 0, 0);
            return new ResolvedSourceSpan(emptySpan, new[] { emptySpan });
        }

        var clampedStart = Math.Max(0, Math.Min(start, Length));
        var clampedEnd = Math.Max(clampedStart, Math.Min(clampedStart + Math.Max(0, length), Length));
        if (clampedEnd == clampedStart)
        {
            var pointSpan = ResolvePoint(clampedStart);
            return new ResolvedSourceSpan(pointSpan, new[] { pointSpan });
        }

        var origins = new List<OriginalSourceSpan>();
        foreach (var segment in segments)
        {
            var intersectionStart = Math.Max(clampedStart, segment.OutputStart);
            var intersectionEnd = Math.Min(clampedEnd, segment.OutputEnd);
            if (intersectionEnd <= intersectionStart)
            {
                continue;
            }

            origins.Add(MapIntersection(segment, intersectionStart, intersectionEnd));
        }

        if (origins.Count == 0)
        {
            var pointSpan = ResolvePoint(clampedStart);
            return new ResolvedSourceSpan(pointSpan, new[] { pointSpan });
        }

        return new ResolvedSourceSpan(origins[0], origins);
    }

    /// <summary>
    /// Resolves a point in preprocessed text to an original source point.
    /// </summary>
    private OriginalSourceSpan ResolvePoint(int offset)
    {
        PreprocessedSourceMapSegment? fallback = null;
        foreach (var segment in segments)
        {
            if (segment.OutputStart <= offset && offset < segment.OutputEnd)
            {
                return MapPoint(segment, offset);
            }

            if (segment.OutputStart <= offset)
            {
                fallback = segment;
            }
            else
            {
                break;
            }
        }

        return MapPoint(fallback ?? segments[0], offset);
    }

    /// <summary>
    /// Maps an intersected output range within a segment to its corresponding original span.
    /// </summary>
    private static OriginalSourceSpan MapIntersection(
        PreprocessedSourceMapSegment segment,
        int intersectionStart,
        int intersectionEnd)
    {
        var originalStart = MapOriginalStart(segment, intersectionStart);
        var requestedLength = intersectionEnd - intersectionStart;
        var availableLength = Math.Max(0, segment.OriginalSpan.End - originalStart);
        var originalLength = segment.OriginalSpan.Length == 0
            ? 0
            : Math.Min(requestedLength, availableLength);
        return new OriginalSourceSpan(segment.OriginalSpan.Document, originalStart, originalLength);
    }

    /// <summary>
    /// Maps an output point within a segment to an original zero-length span.
    /// </summary>
    private static OriginalSourceSpan MapPoint(PreprocessedSourceMapSegment segment, int offset)
    {
        return new OriginalSourceSpan(segment.OriginalSpan.Document, MapOriginalStart(segment, offset), 0);
    }

    /// <summary>
    /// Maps an output offset within a segment to the corresponding original source offset.
    /// </summary>
    private static int MapOriginalStart(PreprocessedSourceMapSegment segment, int outputOffset)
    {
        if (segment.OriginalSpan.Length == 0)
        {
            return segment.OriginalSpan.Start;
        }

        var relative = Math.Max(0, Math.Min(outputOffset - segment.OutputStart, segment.OriginalSpan.Length));
        return segment.OriginalSpan.Start + relative;
    }
}
