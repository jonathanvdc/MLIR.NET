namespace MLIR.Generators.Emitters.Common;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Converts Markdown text from ODS description fields to structured XML
/// documentation comment content lines suitable for C# source generation.
/// </summary>
/// <remarks>
/// <para>
/// This converter handles the subset of CommonMark Markdown relevant to MLIR ODS
/// documentation:
/// </para>
/// <list type="bullet">
/// <item>Fenced code blocks (<c>```lang ... ```</c>) → <c>&lt;code language="lang"&gt;...&lt;/code&gt;</c></item>
/// <item>ATX headings (<c># through ######</c>) → <c>&lt;para&gt;&lt;b&gt;text&lt;/b&gt;&lt;/para&gt;</c></item>
/// <item>Section-header lines (single-line paragraph ending in <c>:</c>) → <c>&lt;para&gt;&lt;b&gt;text&lt;/b&gt;&lt;/para&gt;</c></item>
/// <item>Paragraphs separated by blank lines → <c>&lt;para&gt;...&lt;/para&gt;</c></item>
/// <item>Inline code (<c>`code`</c>) → <c>&lt;c&gt;code&lt;/c&gt;</c></item>
/// <item>Inline links (<c>[text](url)</c>) → <c>&lt;see href="url"&gt;text&lt;/see&gt;</c></item>
/// </list>
/// <para>
/// All output is XML-escaped so that it is safe to embed in XML documentation comments.
/// Leading indentation common to all non-empty lines is stripped before parsing,
/// which matches the indentation style used in MLIR ODS <c>[{...}]</c> multi-line strings.
/// </para>
/// </remarks>
internal static class MarkdownXmlDocConverter
{
    /// <summary>
    /// Converts the supplied Markdown <paramref name="description"/> text to a sequence
    /// of XML doc comment inner lines suitable for embedding inside a
    /// <c>&lt;remarks&gt;</c> element.
    /// </summary>
    /// <remarks>
    /// Callers should prefix each returned line with <c>/// </c> and surround the
    /// whole sequence with opening and closing <c>&lt;remarks&gt;</c> lines.
    /// The returned lines never have a trailing newline; they are ready to be
    /// passed to <c>StringBuilder.AppendLine</c>.
    /// </remarks>
    public static IReadOnlyList<string> ConvertToRemarksLines(string description)
    {
        var dedented = Dedent(description);
        var blocks = ParseBlocks(dedented);
        var lines = new List<string>();
        foreach (var block in blocks)
        {
            RenderBlock(block, lines);
        }

        return lines;
    }

    // -----------------------------------------------------------------------
    // Dedenting
    // -----------------------------------------------------------------------

    /// <summary>
    /// Strips the common leading whitespace from all non-blank lines in
    /// <paramref name="text"/>, matching the behaviour of Python's
    /// <c>textwrap.dedent</c>.
    /// </summary>
    private static string Dedent(string text)
    {
        var rawLines = SplitLines(text);

        // Compute the minimum indentation across all non-blank lines.
        var minIndent = int.MaxValue;
        foreach (var line in rawLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var indent = CountLeadingSpaces(line);
            if (indent < minIndent)
            {
                minIndent = indent;
            }
        }

        if (minIndent == int.MaxValue || minIndent == 0)
        {
            return text;
        }

        var result = new StringBuilder();
        for (var i = 0; i < rawLines.Count; i++)
        {
            var line = rawLines[i];
            if (i > 0)
            {
                result.Append('\n');
            }

            // Preserve blank lines as-is; strip minIndent spaces from the rest.
            if (string.IsNullOrWhiteSpace(line))
            {
                result.Append(line);
            }
            else
            {
                result.Append(line.Length >= minIndent ? line.Substring(minIndent) : line);
            }
        }

        return result.ToString();
    }

    private static int CountLeadingSpaces(string line)
    {
        var count = 0;
        while (count < line.Length && (line[count] == ' ' || line[count] == '\t'))
        {
            count++;
        }

        return count;
    }

    // -----------------------------------------------------------------------
    // Block types
    // -----------------------------------------------------------------------

    private abstract class Block { }

    /// <summary>An ATX heading or a section-header line (ends in <c>:</c>).</summary>
    private sealed class HeadingBlock : Block
    {
        public HeadingBlock(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    /// <summary>A fenced code block delimited by <c>```</c> markers.</summary>
    private sealed class FencedCodeBlock : Block
    {
        public FencedCodeBlock(string? language, List<string> codeLines)
        {
            Language = language;
            CodeLines = codeLines;
        }

        /// <summary>The language tag on the opening fence, or <see langword="null"/> if none was supplied.</summary>
        public string? Language { get; }

        public List<string> CodeLines { get; }
    }

    /// <summary>One or more consecutive non-blank lines forming a paragraph.</summary>
    private sealed class ParagraphBlock : Block
    {
        public ParagraphBlock(List<string> lines)
        {
            Lines = lines;
        }

        public List<string> Lines { get; }
    }

    // -----------------------------------------------------------------------
    // Parser
    // -----------------------------------------------------------------------

    /// <summary>
    /// Splits <paramref name="text"/> into logical blocks.
    /// Blank lines between blocks are consumed as separators and do not produce blocks.
    /// </summary>
    private static List<Block> ParseBlocks(string text)
    {
        var result = new List<Block>();
        var rawLines = SplitLines(text);

        var i = 0;
        while (i < rawLines.Count)
        {
            var line = rawLines[i];

            // Skip blank separator lines between blocks.
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // ATX heading: optionally preceded by up to 3 spaces, then 1-6 '#' characters.
            var trimmedLine = line.TrimStart();
            if (TryParseAtxHeading(trimmedLine, out var headingText))
            {
                result.Add(new HeadingBlock(headingText!));
                i++;
                continue;
            }

            // Fenced code block: optionally preceded by up to 3 spaces, then '```'.
            if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
            {
                var language = trimmedLine.Substring(3).Trim();
                if (string.IsNullOrEmpty(language))
                {
                    language = null;
                }

                i++;
                var codeLines = new List<string>();
                while (i < rawLines.Count)
                {
                    var codeLine = rawLines[i];
                    if (codeLine.TrimStart().StartsWith("```", StringComparison.Ordinal))
                    {
                        i++;
                        break;
                    }

                    codeLines.Add(codeLine);
                    i++;
                }

                result.Add(new FencedCodeBlock(language, codeLines));
                continue;
            }

            // Paragraph: collect consecutive non-blank lines that are not a heading
            // or code-fence opener.
            var paraLines = new List<string>();
            while (i < rawLines.Count)
            {
                var paraLine = rawLines[i];
                if (string.IsNullOrWhiteSpace(paraLine))
                {
                    break;
                }

                var trimmedPara = paraLine.TrimStart();
                if (TryParseAtxHeading(trimmedPara, out _) ||
                    trimmedPara.StartsWith("```", StringComparison.Ordinal))
                {
                    break;
                }

                paraLines.Add(paraLine);
                i++;
            }

            if (paraLines.Count > 0)
            {
                // A single-line paragraph whose trimmed text ends with ':' is treated as a
                // section header (e.g. "Example:", "Note:") and rendered in bold, mirroring
                // how MLIR ODS descriptions use such lines as informal section dividers.
                if (paraLines.Count == 1)
                {
                    var singleLine = paraLines[0].Trim();
                    if (IsSectionHeaderLine(singleLine))
                    {
                        result.Add(new HeadingBlock(singleLine));
                        continue;
                    }
                }

                result.Add(new ParagraphBlock(paraLines));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="trimmedLine"/> begins with 1-6
    /// <c>#</c> characters followed by a space or end-of-line, and sets
    /// <paramref name="headingText"/> to the heading content.
    /// </summary>
    private static bool TryParseAtxHeading(string trimmedLine, out string? headingText)
    {
        var level = 0;
        while (level < trimmedLine.Length && trimmedLine[level] == '#')
        {
            level++;
        }

        if (level >= 1 && level <= 6)
        {
            if (level == trimmedLine.Length)
            {
                // Heading with no text.
                headingText = string.Empty;
                return true;
            }

            if (trimmedLine[level] == ' ')
            {
                headingText = trimmedLine.Substring(level + 1).Trim();
                return true;
            }
        }

        headingText = null;
        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="trimmedLine"/> looks like an
    /// MLIR-style section header: non-empty, ends with <c>:</c>, contains no sentence
    /// break characters (no period followed by space), and is short enough to plausibly
    /// be a label rather than a full sentence.
    /// </summary>
    private static bool IsSectionHeaderLine(string trimmedLine)
    {
        if (string.IsNullOrEmpty(trimmedLine) || !trimmedLine.EndsWith(":", StringComparison.Ordinal))
        {
            return false;
        }

        // Reject lines that look like a full sentence ("...Note: see below.").
        if (trimmedLine.IndexOf(". ", StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        // Reject very long lines — real section headers in MLIR descriptions are short.
        return trimmedLine.Length <= 80;
    }

    // -----------------------------------------------------------------------
    // Renderer
    // -----------------------------------------------------------------------

    /// <summary>
    /// Renders a parsed <paramref name="block"/> into a sequence of XML doc comment lines
    /// and appends them to <paramref name="output"/>.
    /// </summary>
    private static void RenderBlock(Block block, List<string> output)
    {
        if (block is HeadingBlock heading)
        {
            output.Add("<para>");
            output.Add("<b>" + ConvertInline(heading.Text) + "</b>");
            output.Add("</para>");
        }
        else if (block is FencedCodeBlock code)
        {
            var openTag = code.Language != null
                ? "<code language=\"" + code.Language + "\">"
                : "<code>";
            output.Add(openTag);
            foreach (var codeLine in code.CodeLines)
            {
                output.Add(EscapeXml(codeLine));
            }

            output.Add("</code>");
        }
        else if (block is ParagraphBlock para)
        {
            output.Add("<para>");
            foreach (var paraLine in para.Lines)
            {
                output.Add(ConvertInline(paraLine));
            }

            output.Add("</para>");
        }
    }

    // -----------------------------------------------------------------------
    // Inline Markdown converter
    // -----------------------------------------------------------------------

    /// <summary>
    /// Converts inline Markdown in <paramref name="text"/> to XML doc equivalents
    /// while XML-escaping all literal characters.
    /// </summary>
    /// <remarks>
    /// Handles:
    /// <list type="bullet">
    /// <item>Inline code (<c>`code`</c>) → <c>&lt;c&gt;code&lt;/c&gt;</c></item>
    /// <item>Inline links (<c>[text](url)</c>) → <c>&lt;see href="url"&gt;text&lt;/see&gt;</c></item>
    /// <item>All other text is XML-escaped character by character.</item>
    /// </list>
    /// </remarks>
    internal static string ConvertInline(string text)
    {
        var result = new StringBuilder(text.Length);
        var i = 0;

        while (i < text.Length)
        {
            var ch = text[i];

            // Inline code: `code`
            if (ch == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    var code = text.Substring(i + 1, end - i - 1);
                    result.Append("<c>");
                    result.Append(EscapeXml(code));
                    result.Append("</c>");
                    i = end + 1;
                    continue;
                }
            }

            // Inline link: [text](url)
            if (ch == '[')
            {
                var closeBracket = text.IndexOf(']', i + 1);
                if (closeBracket > i &&
                    closeBracket + 1 < text.Length &&
                    text[closeBracket + 1] == '(')
                {
                    var closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket + 1)
                    {
                        var linkText = text.Substring(i + 1, closeBracket - i - 1);
                        var linkUrl = text.Substring(closeBracket + 2, closeParen - closeBracket - 2);
                        result.Append("<see href=\"");
                        result.Append(EscapeXml(linkUrl));
                        result.Append("\">");
                        // Recursively convert any inline Markdown inside the link text.
                        result.Append(ConvertInline(linkText));
                        result.Append("</see>");
                        i = closeParen + 1;
                        continue;
                    }
                }
            }

            // Regular character — XML-escape it.
            switch (ch)
            {
                case '&':
                    result.Append("&amp;");
                    break;
                case '<':
                    result.Append("&lt;");
                    break;
                case '>':
                    result.Append("&gt;");
                    break;
                default:
                    result.Append(ch);
                    break;
            }

            i++;
        }

        return result.ToString();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns an XML-escaped copy of <paramref name="text"/> with
    /// <c>&amp;</c>, <c>&lt;</c>, and <c>&gt;</c> replaced by their entity references.
    /// </summary>
    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    /// <summary>
    /// Splits <paramref name="text"/> on <c>\n</c>, stripping trailing <c>\r</c>
    /// from each segment.
    /// </summary>
    private static List<string> SplitLines(string text)
    {
        var segments = text.Split('\n');
        var lines = new List<string>(segments.Length);
        foreach (var segment in segments)
        {
            lines.Add(segment.TrimEnd('\r'));
        }

        return lines;
    }
}
