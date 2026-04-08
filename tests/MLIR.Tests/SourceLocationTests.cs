namespace MLIR.Tests;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

/// <summary>
/// Tests for the offset-based source location model: <see cref="SourceDocument"/>,
/// <see cref="SourceLocation"/>, and the updated <see cref="SyntaxToken"/> API.
/// </summary>
public sealed class SourceLocationTests
{
    // -----------------------------------------------------------------------
    // SourceDocument – offset-to-line/column mapping
    // -----------------------------------------------------------------------

    [Fact]
    public void SourceDocument_SingleLine_ReturnsLine1ForAnyOffset()
    {
        var doc = new SourceDocument("hello");

        Assert.Equal((1, 1), doc.GetLineColumn(0));
        Assert.Equal((1, 3), doc.GetLineColumn(2));
        Assert.Equal((1, 6), doc.GetLineColumn(5));
    }

    [Fact]
    public void SourceDocument_MultiLine_ReturnsCorrectLineAndColumn()
    {
        // "ab\ncd\nef"  offsets: a=0,b=1,\n=2,c=3,d=4,\n=5,e=6,f=7
        var doc = new SourceDocument("ab\ncd\nef");

        Assert.Equal((1, 1), doc.GetLineColumn(0)); // 'a'
        Assert.Equal((1, 2), doc.GetLineColumn(1)); // 'b'
        Assert.Equal((1, 3), doc.GetLineColumn(2)); // '\n'  – still line 1
        Assert.Equal((2, 1), doc.GetLineColumn(3)); // 'c'
        Assert.Equal((2, 2), doc.GetLineColumn(4)); // 'd'
        Assert.Equal((3, 1), doc.GetLineColumn(6)); // 'e'
        Assert.Equal((3, 2), doc.GetLineColumn(7)); // 'f'
    }

    [Fact]
    public void SourceDocument_EmptyString_ReturnsLine1Column1()
    {
        var doc = new SourceDocument(string.Empty);

        Assert.Equal((1, 1), doc.GetLineColumn(0));
    }

    [Fact]
    public void SourceDocument_NullText_TreatedAsEmpty()
    {
        var doc = new SourceDocument(null!);

        Assert.Equal(string.Empty, doc.Text);
        Assert.Equal(0, doc.Length);
        Assert.Equal((1, 1), doc.GetLineColumn(0));
    }

    [Fact]
    public void SourceDocument_OffsetAtStartOfEachLine_ReturnsColumn1()
    {
        // "line1\nline2\nline3"
        var doc = new SourceDocument("line1\nline2\nline3");

        Assert.Equal((1, 1), doc.GetLineColumn(0));
        Assert.Equal((2, 1), doc.GetLineColumn(6));
        Assert.Equal((3, 1), doc.GetLineColumn(12));
    }

    [Fact]
    public void SourceDocument_LengthMatchesTextLength()
    {
        var text = "hello world";
        var doc = new SourceDocument(text);
        Assert.Equal(text.Length, doc.Length);
    }

    // -----------------------------------------------------------------------
    // SourceLocation – document-relative, computed line/column
    // -----------------------------------------------------------------------

    [Fact]
    public void SourceLocation_Unknown_IsNotKnown()
    {
        var loc = SourceLocation.Unknown;

        Assert.False(loc.IsKnown);
        Assert.Equal(0, loc.Line);
        Assert.Equal(0, loc.Column);
        Assert.Equal(string.Empty, loc.ToString());
    }

    [Fact]
    public void SourceLocation_WithDocument_IsKnown()
    {
        var doc = new SourceDocument("hello");
        var loc = new SourceLocation(doc, 0, 5);

        Assert.True(loc.IsKnown);
        Assert.Equal(1, loc.Line);
        Assert.Equal(1, loc.Column);
    }

    [Fact]
    public void SourceLocation_DerivesLineColumnFromDocument()
    {
        // "ab\ncd"  – 'c' is at offset 3 → line 2, column 1
        var doc = new SourceDocument("ab\ncd");
        var loc = new SourceLocation(doc, 3, 1);

        Assert.Equal(2, loc.Line);
        Assert.Equal(1, loc.Column);
    }

    [Fact]
    public void SourceLocation_SpanProperties_AreConsistent()
    {
        var doc = new SourceDocument("hello world");
        var loc = new SourceLocation(doc, 6, 5); // "world"

        Assert.Equal(6, loc.Start);
        Assert.Equal(5, loc.Length);
        Assert.Equal(11, loc.End);
    }

    [Fact]
    public void SourceLocation_ToString_FormatsLineColon()
    {
        var doc = new SourceDocument("hello\nworld");
        var loc = new SourceLocation(doc, 6, 5); // "world" at line 2, col 1

        Assert.Equal("2:1", loc.ToString());
    }

    [Fact]
    public void SourceLocation_Default_EqualsUnknown()
    {
        var loc = default(SourceLocation);
        Assert.False(loc.IsKnown);
        Assert.Equal(SourceLocation.Unknown, loc);
    }

    // -----------------------------------------------------------------------
    // SyntaxToken – synthetic vs. source-backed tokens
    // -----------------------------------------------------------------------

    [Fact]
    public void SyntaxToken_Synthetic_HasNoSourceLocation()
    {
        var token = new SyntaxToken("hello");

        Assert.False(token.HasSourceLocation);
        Assert.False(token.Location.IsKnown);
        Assert.Equal(SourceLocation.Unknown, token.Location);
    }

    [Fact]
    public void SyntaxToken_Synthetic_WithLeadingTrivia_HasNoSourceLocation()
    {
        var token = new SyntaxToken("world", "  ");

        Assert.False(token.HasSourceLocation);
        Assert.Equal("  ", token.LeadingTrivia);
        Assert.Equal("world", token.Text);
    }

    [Fact]
    public void SyntaxToken_FullText_CombinesTriviaAndText()
    {
        var token = new SyntaxToken("op", " ");
        Assert.Equal(" op", token.FullText);
    }

    [Fact]
    public void SyntaxToken_WithText_PreservesLeadingTrivia()
    {
        var original = new SyntaxToken("old", "  ");
        var updated = original.WithText("new");

        Assert.Equal("new", updated.Text);
        Assert.Equal("  ", updated.LeadingTrivia);
        Assert.False(updated.HasSourceLocation);
    }

    [Fact]
    public void SyntaxToken_WithText_OnSyntheticToken_ProducesSyntheticToken()
    {
        var token = new SyntaxToken("foo");
        var copy = token.WithText("bar");

        Assert.Equal("bar", copy.Text);
        Assert.False(copy.HasSourceLocation);
    }

    // -----------------------------------------------------------------------
    // Parser integration – tokens produced by parsing carry source locations
    // -----------------------------------------------------------------------

    [Fact]
    public void ParsedTokens_HaveSourceLocation()
    {
        var module = Parser.ParseModule("%0 = \"test.op\"() : () -> i32");
        var op = module.Operations[0];

        // The name token should resolve to line 1, column 6 (after "%0 = ")
        Assert.True(op.NameToken.HasSourceLocation);
        Assert.Equal(1, op.NameToken.Location.Line);
        Assert.Equal(6, op.NameToken.Location.Column);
    }

    [Fact]
    public void ParsedTokens_MultiLine_HaveCorrectLineColumn()
    {
        // The operation is on the second line.
        var module = Parser.ParseModule("\n\"test.op\"() : () -> ()");
        var op = module.Operations[0];

        Assert.Equal(2, op.NameToken.Location.Line);
        Assert.Equal(1, op.NameToken.Location.Column);
    }

    [Fact]
    public void ParsedResultToken_HasSourceLocation()
    {
        var module = Parser.ParseModule("%result = \"test.op\"() : () -> i32");
        var op = module.Operations[0];

        var resultToken = op.ResultList[0];
        Assert.True(resultToken.HasSourceLocation);
        Assert.Equal(1, resultToken.Location.Line);
        Assert.Equal(1, resultToken.Location.Column);
    }

    [Fact]
    public void WithText_OnParsedToken_KeepsLocation()
    {
        var module = Parser.ParseModule("%0 = \"test.op\"() : () -> i32");
        var nameToken = module.Operations[0].NameToken;

        // WithText should preserve the document reference so the renamed token still
        // resolves to the same source location.
        var renamed = nameToken.WithText("renamed.op");

        Assert.Equal("renamed.op", renamed.Text);
        Assert.Equal(nameToken.Location.Line, renamed.Location.Line);
        Assert.Equal(nameToken.Location.Column, renamed.Location.Column);
    }

    [Fact]
    public void SourceLocation_SameLine_MultipleLookups_AreConsistent()
    {
        // Calling Line and Column twice on the same location must return the same values.
        var doc = new SourceDocument("abc\ndef");
        var loc = new SourceLocation(doc, 4, 3); // "def" at line 2, col 1

        Assert.Equal(loc.Line, loc.Line);
        Assert.Equal(loc.Column, loc.Column);
    }
}
