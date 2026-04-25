namespace TableGen;

using System;
using System.Collections.Generic;
using System.Text;
using MLIR.Text;

/// <summary>
/// Processes C-style preprocessor directives in TableGen source files.
/// </summary>
/// <remarks>
/// The following directives are supported:
/// <list type="bullet">
/// <item><description><c>#define SYMBOL</c> — adds SYMBOL to the defines set</description></item>
/// <item><description><c>#ifndef SYMBOL</c> — includes content if SYMBOL is not defined</description></item>
/// <item><description><c>#ifdef SYMBOL</c> — includes content if SYMBOL is defined</description></item>
/// <item><description><c>#else</c> — switches the active branch of a conditional</description></item>
/// <item><description><c>#endif</c> — closes a conditional block</description></item>
/// </list>
/// Inactive lines are replaced with blank lines to preserve line numbers for diagnostics.
/// The process method's <c>defines</c> parameter is updated in-place so that symbols defined in one file
/// remain visible when other files are processed.
/// </remarks>
public static class Preprocessor
{
    /// <summary>
    /// Processes preprocessor directives in <paramref name="source"/> using the given
    /// shared <paramref name="defines"/> set.
    /// </summary>
    /// <param name="source">The source text to process.</param>
    /// <param name="defines">
    /// The set of currently defined preprocessor symbols.  Updated in-place when
    /// <c>#define</c> directives are encountered in active regions.
    /// </param>
    /// <returns>
    /// The source text with inactive regions replaced by blank lines and preprocessor
    /// directive lines removed.
    /// </returns>
    public static string Process(string source, ISet<string> defines)
    {
        return Process(new OriginalSourceDocument(source), defines).Text;
    }

    /// <summary>
    /// Processes preprocessor directives in <paramref name="sourceDocument"/> using the given
    /// shared <paramref name="defines"/> set.
    /// </summary>
    /// <param name="sourceDocument">The source document to process.</param>
    /// <param name="defines">
    /// The set of currently defined preprocessor symbols.  Updated in-place when
    /// <c>#define</c> directives are encountered in active regions.
    /// </param>
    /// <returns>
    /// A source document view whose text contains the preprocessed output and whose spans resolve
    /// back to the original source spans covered by the input document.
    /// </returns>
    public static PreprocessedSourceDocument Process(SourceDocument sourceDocument, ISet<string> defines)
    {
        var source = sourceDocument.Text;
        var output = new StringBuilder(source.Length);
        var segments = new List<PreprocessedSourceMapSegment>();

        // Stack of (ownActive, parentActive) pairs.
        // ownActive:    whether this block's own condition is true.
        // parentActive: whether the enclosing context was active when this block was entered.
        // A region is included only when both ownActive && parentActive are true for
        // every frame on the stack.
        var condStack = new Stack<(bool ownActive, bool parentActive)>();

        bool IsActive()
        {
            // A line is active only if every nested conditional frame is active in both its own branch
            // and its inherited parent context.
            foreach (var (ownActive, parentActive) in condStack)
            {
                if (!ownActive || !parentActive)
                {
                    return false;
                }
            }

            return true;
        }

        var lineStart = 0;
        while (lineStart <= source.Length)
        {
            var newlineOffset = source.IndexOf('\n', lineStart);
            var hasNewline = newlineOffset >= 0;
            var lineEnd = hasNewline ? newlineOffset : source.Length;
            var nextLineStart = hasNewline ? newlineOffset + 1 : source.Length + 1;
            var rawLine = source.Substring(lineStart, lineEnd - lineStart);

            // Normalize \r\n line endings.
            if (rawLine.Length > 0 && rawLine[rawLine.Length - 1] == '\r')
            {
                rawLine = rawLine.Substring(0, rawLine.Length - 1);
            }

            var trimmed = rawLine.TrimStart();

            if (trimmed.StartsWith("#ifndef ") || trimmed.StartsWith("#ifndef\t"))
            {
                var symbol = trimmed.Substring("#ifndef".Length).Trim();
                var parentActive = IsActive();
                condStack.Push((parentActive && !defines.Contains(symbol), parentActive));
                AppendSyntheticLineBreak(sourceDocument, output, segments, lineStart);
                lineStart = nextLineStart;
                continue;
            }

            if (trimmed.StartsWith("#ifdef ") || trimmed.StartsWith("#ifdef\t"))
            {
                var symbol = trimmed.Substring("#ifdef".Length).Trim();
                var parentActive = IsActive();
                condStack.Push((parentActive && defines.Contains(symbol), parentActive));
                AppendSyntheticLineBreak(sourceDocument, output, segments, lineStart);
                lineStart = nextLineStart;
                continue;
            }

            if (IsEndif(trimmed))
            {
                if (condStack.Count > 0)
                {
                    condStack.Pop();
                }

                AppendSyntheticLineBreak(sourceDocument, output, segments, lineStart);
                lineStart = nextLineStart;
                continue;
            }

            if (IsElse(trimmed))
            {
                if (condStack.Count > 0)
                {
                    var (ownActive, parentActive) = condStack.Pop();
                    // Simply flip the own-active state; the parent context stays the same.
                    condStack.Push((!ownActive, parentActive));
                }

                AppendSyntheticLineBreak(sourceDocument, output, segments, lineStart);
                lineStart = nextLineStart;
                continue;
            }

            if (trimmed.StartsWith("#define ") || trimmed.StartsWith("#define\t"))
            {
                if (IsActive())
                {
                    var rest = trimmed.Substring("#define".Length).Trim();
                    // Accept only the symbol name; ignore any replacement value.
                    var spaceIdx = IndexOfWhitespace(rest);
                    var symbol = spaceIdx >= 0 ? rest.Substring(0, spaceIdx) : rest;
                    if (symbol.Length > 0)
                    {
                        defines.Add(symbol);
                    }
                }

                AppendSyntheticLineBreak(sourceDocument, output, segments, lineStart);
                lineStart = nextLineStart;
                continue;
            }

            // Ordinary source line: emit when active, blank line when inactive.
            if (IsActive())
            {
                AppendMappedText(sourceDocument, output, segments, rawLine, lineStart);
                output.Append(rawLine);
            }

            AppendMappedLineBreak(sourceDocument, output, segments, hasNewline ? newlineOffset : lineStart + rawLine.Length);
            lineStart = nextLineStart;
        }

        return new PreprocessedSourceDocument(output.ToString(), segments);
    }

    /// <summary>
    /// Appends active source text and records its one-to-one source mapping.
    /// </summary>
    private static void AppendMappedText(
        SourceDocument sourceDocument,
        StringBuilder output,
        List<PreprocessedSourceMapSegment> segments,
        string text,
        int sourceStart)
    {
        if (text.Length == 0)
        {
            return;
        }

        AddSourceSegments(sourceDocument, segments, output.Length, text.Length, sourceStart, text.Length);
    }

    /// <summary>
    /// Appends a line break that corresponds to an original source line break or end-of-line point.
    /// </summary>
    private static void AppendMappedLineBreak(
        SourceDocument sourceDocument,
        StringBuilder output,
        List<PreprocessedSourceMapSegment> segments,
        int sourceOffset)
    {
        AddSourceSegments(sourceDocument, segments, output.Length, 1, sourceOffset, sourceOffset < sourceDocument.Length ? 1 : 0);
        output.Append('\n');
    }

    /// <summary>
    /// Appends a synthetic line break for a directive or inactive line and anchors it at the
    /// original line start.
    /// </summary>
    private static void AppendSyntheticLineBreak(
        SourceDocument sourceDocument,
        StringBuilder output,
        List<PreprocessedSourceMapSegment> segments,
        int sourceOffset)
    {
        AddSourceSegments(sourceDocument, segments, output.Length, 1, sourceOffset, 0);
        output.Append('\n');
    }

    /// <summary>
    /// Adds mapping segments from an output range to the original spans resolved from an input range.
    /// </summary>
    private static void AddSourceSegments(
        SourceDocument sourceDocument,
        List<PreprocessedSourceMapSegment> segments,
        int outputStart,
        int outputLength,
        int sourceStart,
        int sourceLength)
    {
        var resolved = sourceDocument.ResolveSpan(sourceStart, sourceLength);
        if (resolved.OriginSpans.Count == 0)
        {
            segments.Add(new PreprocessedSourceMapSegment(outputStart, outputLength, resolved.PrimarySpan));
            return;
        }

        if (resolved.OriginSpans.Count == 1)
        {
            segments.Add(new PreprocessedSourceMapSegment(outputStart, outputLength, resolved.OriginSpans[0]));
            return;
        }

        var segmentOutputStart = outputStart;
        foreach (var originSpan in resolved.OriginSpans)
        {
            var segmentLength = Math.Min(originSpan.Length, outputStart + outputLength - segmentOutputStart);
            if (segmentLength <= 0)
            {
                continue;
            }

            segments.Add(new PreprocessedSourceMapSegment(segmentOutputStart, segmentLength, originSpan));
            segmentOutputStart += segmentLength;
            if (segmentOutputStart >= outputStart + outputLength)
            {
                break;
            }
        }

        if (segmentOutputStart < outputStart + outputLength)
        {
            segments.Add(new PreprocessedSourceMapSegment(
                segmentOutputStart,
                outputStart + outputLength - segmentOutputStart,
                resolved.PrimarySpan));
        }
    }

    /// <summary>
    /// Determines whether a trimmed line is an <c>#endif</c> directive.
    /// </summary>
    /// <param name="trimmed">The line text with leading whitespace removed.</param>
    /// <returns><see langword="true"/> when the line is an <c>#endif</c> directive; otherwise <see langword="false"/>.</returns>
    private static bool IsEndif(string trimmed)
    {
        if (!trimmed.StartsWith("#endif"))
        {
            return false;
        }

        return trimmed.Length == "#endif".Length
            || char.IsWhiteSpace(trimmed["#endif".Length])
            || trimmed["#endif".Length] == '/';
    }

    /// <summary>
    /// Determines whether a trimmed line is an <c>#else</c> directive.
    /// </summary>
    /// <param name="trimmed">The line text with leading whitespace removed.</param>
    /// <returns><see langword="true"/> when the line is an <c>#else</c> directive; otherwise <see langword="false"/>.</returns>
    private static bool IsElse(string trimmed)
    {
        if (!trimmed.StartsWith("#else"))
        {
            return false;
        }

        return trimmed.Length == "#else".Length
            || char.IsWhiteSpace(trimmed["#else".Length])
            || trimmed["#else".Length] == '/';
    }

    /// <summary>
    /// Finds the first whitespace character in a string.
    /// </summary>
    /// <param name="s">The string to inspect.</param>
    /// <returns>The index of the first whitespace character, or <c>-1</c> if none exists.</returns>
    private static int IndexOfWhitespace(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (char.IsWhiteSpace(s[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
