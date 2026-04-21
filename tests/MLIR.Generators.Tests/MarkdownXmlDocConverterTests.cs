namespace MLIR.Generators.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Generators.Emitters.Common;
using Xunit;

/// <summary>
/// Unit tests for <see cref="MarkdownXmlDocConverter"/>.
/// </summary>
public sealed class MarkdownXmlDocConverterTests
{
    // -----------------------------------------------------------------------
    // ConvertToRemarksLines — block-level tests
    // -----------------------------------------------------------------------

    [Fact]
    public void SimpleParagraphIsWrappedInParaTags()
    {
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines("Hello, world.");

        Assert.Equal(["<para>", "Hello, world.", "</para>"], lines);
    }

    [Fact]
    public void TwoParagraphsProduceTwoParaBlocks()
    {
        var input = "First paragraph.\n\nSecond paragraph.";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Contains("<para>", lines);
        Assert.Contains("First paragraph.", lines);
        Assert.Contains("Second paragraph.", lines);
        Assert.Equal(2, lines.Count(static l => l == "<para>"));
        Assert.Equal(2, lines.Count(static l => l == "</para>"));
    }

    [Fact]
    public void MultiLineParagraphLinesAreAllInsideSinglePara()
    {
        var input = "Line one.\nLine two.\nLine three.";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<para>", "Line one.", "Line two.", "Line three.", "</para>"], lines);
    }

    [Fact]
    public void FencedCodeBlockWithLanguageEmitsCodeTagWithLanguageAttribute()
    {
        var input = "```mlir\n%0 = arith.constant 1 : i32\n```";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<code language=\"mlir\">", "%0 = arith.constant 1 : i32", "</code>"], lines);
    }

    [Fact]
    public void FencedCodeBlockWithoutLanguageEmitsPlainCodeTag()
    {
        var input = "```\nsome code\n```";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<code>", "some code", "</code>"], lines);
    }

    [Fact]
    public void CodeBlockContentIsXmlEscaped()
    {
        var input = "```\n<op a=\"b\"> & </op>\n```";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<code>", "&lt;op a=\"b\"&gt; &amp; &lt;/op&gt;", "</code>"], lines);
    }

    [Fact]
    public void AtxHeadingLevel1IsRenderedBold()
    {
        var input = "# My Heading";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<para>", "<b>My Heading</b>", "</para>"], lines);
    }

    [Fact]
    public void AtxHeadingLevel4IsRenderedBold()
    {
        var input = "#### Deep Section";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<para>", "<b>Deep Section</b>", "</para>"], lines);
    }

    [Fact]
    public void SectionHeaderLineEndingWithColonIsRenderedBold()
    {
        var input = "Example:";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<para>", "<b>Example:</b>", "</para>"], lines);
    }

    [Fact]
    public void MultiLineParagraphEndingWithColonIsNotRenderedBold()
    {
        // Only a *single-line* paragraph that ends in ':' is promoted to a heading.
        var input = "Some longer description that\nends with a colon:";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        // Must be a plain paragraph, not a heading.
        Assert.Contains("<para>", lines);
        Assert.DoesNotContain("<b>Some longer description that</b>", lines);
        Assert.DoesNotContain("<b>ends with a colon:</b>", lines);
    }

    [Fact]
    public void DedentsIndentedDescription()
    {
        // ODS multi-line strings ([{...}]) produce consistently-indented content.
        var input = "    First line.\n    Second line.";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Equal(["<para>", "First line.", "Second line.", "</para>"], lines);
    }

    [Fact]
    public void DedentsIndentedDescriptionWithCodeBlock()
    {
        var input = "    Text.\n\n    ```mlir\n    %0 = op\n    ```";
        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(input);

        Assert.Contains("<para>", lines);
        Assert.Contains("Text.", lines);
        Assert.Contains("<code language=\"mlir\">", lines);
        Assert.Contains("%0 = op", lines);
        Assert.Contains("</code>", lines);
    }

    // -----------------------------------------------------------------------
    // ConvertInline — inline-level tests
    // -----------------------------------------------------------------------

    [Fact]
    public void InlineCodeBackticksAreConvertedToCTag()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("Use `func.constant` here.");

        Assert.Equal("Use <c>func.constant</c> here.", result);
    }

    [Fact]
    public void MultipleInlineCodeSpansAreAllConverted()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("`a` and `b`");

        Assert.Equal("<c>a</c> and <c>b</c>", result);
    }

    [Fact]
    public void InlineLinkIsConvertedToSeeHref()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("See [rationale](../Rationale.md#section).");

        Assert.Equal("See <see href=\"../Rationale.md#section\">rationale</see>.", result);
    }

    [Fact]
    public void InlineLinkUrlIsXmlEscaped()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("[x](<url&thing>)");

        Assert.Equal("<see href=\"&lt;url&amp;thing&gt;\">x</see>", result);
    }

    [Fact]
    public void InlineCodeContentIsXmlEscaped()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("Use `<op>` here.");

        Assert.Equal("Use <c>&lt;op&gt;</c> here.", result);
    }

    [Fact]
    public void AmpersandInPlainTextIsXmlEscaped()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("foo & bar");

        Assert.Equal("foo &amp; bar", result);
    }

    [Fact]
    public void LessThanInPlainTextIsXmlEscaped()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("tensor<16xf32>");

        Assert.Equal("tensor&lt;16xf32&gt;", result);
    }

    [Fact]
    public void UnmatchedBacktickIsPassedThroughAsLiteralCharacter()
    {
        // A lone backtick with no matching close should be treated as a literal character.
        var result = MarkdownXmlDocConverter.ConvertInline("one ` tick");

        Assert.Equal("one ` tick", result);
    }

    [Fact]
    public void UnmatchedOpenBracketIsPassedThroughAsLiteralCharacter()
    {
        var result = MarkdownXmlDocConverter.ConvertInline("foo [bar baz");

        Assert.Equal("foo [bar baz", result);
    }

    // -----------------------------------------------------------------------
    // End-to-end: description similar to the func.constant issue example
    // -----------------------------------------------------------------------

    [Fact]
    public void FullFuncConstantDescriptionConvertsCorrectly()
    {
        // The issue shows how a real MLIR ODS description (with 4-space indent)
        // should be converted to structured XML doc comment lines.
        var description = """
            The `func.constant` operation produces an SSA value from a symbol reference
            to a `func.func` operation

            Example:

            ```mlir
            // Reference to function @myfn.
            %2 = func.constant @myfn : (tensor<16xf32>, f32) -> tensor<16xf32>
            ```

            MLIR does not allow direct references to functions in SSA operands because
            the compiler is multithreaded, and disallowing SSA values to directly
            reference a function simplifies this
            ([rationale](../Rationale/Rationale.md#multithreading-the-compiler)).
            """;

        var lines = MarkdownXmlDocConverter.ConvertToRemarksLines(description);
        var joined = string.Join("\n", lines);

        // First paragraph contains inline-code-converted text.
        Assert.Contains("<c>func.constant</c>", joined);
        Assert.Contains("<c>func.func</c>", joined);

        // "Example:" section-header line is promoted to a bold heading.
        Assert.Contains("<b>Example:</b>", joined);

        // Code block with language tag; angle brackets inside are XML-escaped.
        Assert.Contains("<code language=\"mlir\">", joined);
        Assert.Contains("tensor&lt;16xf32&gt;", joined);
        Assert.Contains("</code>", joined);

        // Last paragraph contains a converted link.
        Assert.Contains("<see href=\"../Rationale/Rationale.md#multithreading-the-compiler\">rationale</see>", joined);
    }
}
