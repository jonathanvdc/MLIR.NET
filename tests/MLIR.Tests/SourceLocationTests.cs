namespace MLIR.Tests;

using System.IO;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

/// <summary>
/// Tests for the offset-based source location model: <see cref="SourceDocument"/>,
/// <see cref="StringDocument"/>, <see cref="DerivedSourceDocument"/>,
/// <see cref="SourceLocation"/>, and the updated <see cref="Token"/> API.
/// </summary>
public sealed class SourceLocationTests
{
    // -----------------------------------------------------------------------
    // SourceDocument – offset-to-line/column mapping
    // -----------------------------------------------------------------------

    [Fact]
    public void SourceDocument_SingleLine_ReturnsLine1ForAnyOffset()
    {
        var doc = new StringDocument(string.Empty, "hello");

        AssertPosition(doc.GetPosition(0), null, 1, 1);
        AssertPosition(doc.GetPosition(2), null, 1, 3);
        AssertPosition(doc.GetPosition(5), null, 1, 6);
    }

    [Fact]
    public void SourceDocument_MultiLine_ReturnsCorrectLineAndColumn()
    {
        // "ab\ncd\nef"  offsets: a=0,b=1,\n=2,c=3,d=4,\n=5,e=6,f=7
        var doc = new StringDocument(string.Empty, "ab\ncd\nef");

        AssertPosition(doc.GetPosition(0), null, 1, 1); // 'a'
        AssertPosition(doc.GetPosition(1), null, 1, 2); // 'b'
        AssertPosition(doc.GetPosition(2), null, 1, 3); // '\n'  – still line 1
        AssertPosition(doc.GetPosition(3), null, 2, 1); // 'c'
        AssertPosition(doc.GetPosition(4), null, 2, 2); // 'd'
        AssertPosition(doc.GetPosition(6), null, 3, 1); // 'e'
        AssertPosition(doc.GetPosition(7), null, 3, 2); // 'f'
    }

    [Fact]
    public void SourceDocument_EmptyString_ReturnsLine1Column1()
    {
        var doc = new StringDocument(string.Empty, string.Empty);

        AssertPosition(doc.GetPosition(0), null, 1, 1);
    }

    [Fact]
    public void SourceDocument_EmptyText_HasLengthZero()
    {
        var doc = new StringDocument(string.Empty, string.Empty);

        Assert.Equal(string.Empty, doc.GetText(0, doc.Length));
        Assert.Equal(0, doc.Length);
        AssertPosition(doc.GetPosition(0), null, 1, 1);
    }

    [Fact]
    public void SourceDocument_OffsetAtStartOfEachLine_ReturnsColumn1()
    {
        // "line1\nline2\nline3"
        var doc = new StringDocument(string.Empty, "line1\nline2\nline3");

        AssertPosition(doc.GetPosition(0), null, 1, 1);
        AssertPosition(doc.GetPosition(6), null, 2, 1);
        AssertPosition(doc.GetPosition(12), null, 3, 1);
    }

    [Fact]
    public void SourceDocument_LengthMatchesTextLength()
    {
        var text = "hello world";
        var doc = new StringDocument(string.Empty, text);
        Assert.Equal(text.Length, doc.Length);
    }

    [Fact]
    public void SourceDocument_FileName_IsAvailableWhenProvided()
    {
        var doc = new StringDocument("example.mlir", "hello");

        Assert.Equal("example.mlir", doc.Identifier);
        AssertPosition(doc.GetPosition(0), "example.mlir", 1, 1);
    }

    [Fact]
    public void StringDocument_ResolveSpan_ReturnsOriginalSpan()
    {
        var doc = new StringDocument("example.mlir", "hello");

        var resolved = doc.ResolveSpan(1, 3);

        Assert.Same(doc, resolved.PrimarySpan.Document);
        Assert.Equal(1, resolved.PrimarySpan.Start);
        Assert.Equal(3, resolved.PrimarySpan.Length);
        Assert.Single(resolved.OriginSpans);
        Assert.Same(doc, resolved.OriginSpans[0].Document);
    }

    [Fact]
    public void DerivedSourceDocument_GetPosition_UsesPrimaryOriginalSpan()
    {
        var original = new StringDocument("source.td", "first\nsecond\nthird");
        var view = new FixedDerivedSourceDocument("derived", new OriginalSourceSpan(original, 6, 6));

        var position = view.GetPosition(0);

        AssertPosition(position, "source.td", 2, 1);
    }

    [Fact]
    public void DerivedSourceDocument_CanResolveMultipleOriginalSpans()
    {
        var first = new StringDocument("first.td", "one\ntwo");
        var second = new StringDocument("second.td", "three\nfour");
        var primary = new OriginalSourceSpan(first, 4, 3);
        var secondary = new OriginalSourceSpan(second, 6, 4);
        var view = new FixedDerivedSourceDocument("derived", primary, secondary);
        var location = new SourceLocation(view, 0, view.Length);

        var resolved = location.Resolve();

        Assert.NotNull(resolved);
        Assert.Same(first, resolved!.PrimarySpan.Document);
        Assert.Equal(2, resolved.OriginSpans.Count);
        Assert.Same(first, resolved.OriginSpans[0].Document);
        Assert.Same(second, resolved.OriginSpans[1].Document);
        Assert.Equal(2, location.Position.Line);
        Assert.Equal(1, location.Position.Column);
        Assert.Equal("first.td", location.Position.Identifier);
    }

    // -----------------------------------------------------------------------
    // SourceLocation – document-relative, computed line/column
    // -----------------------------------------------------------------------

    [Fact]
    public void SourceLocation_Unknown_IsNotKnown()
    {
        var loc = SourceLocation.Unknown;

        Assert.False(loc.IsKnown);
        Assert.Equal(0, loc.Position.Line);
        Assert.Equal(0, loc.Position.Column);
    }

    [Fact]
    public void SourceLocation_WithDocument_IsKnown()
    {
        var doc = new StringDocument(string.Empty, "hello");
        var loc = new SourceLocation(doc, 0, 5);

        Assert.True(loc.IsKnown);
        Assert.Equal(1, loc.Position.Line);
        Assert.Equal(1, loc.Position.Column);
    }

    [Fact]
    public void SourceLocation_DerivesLineColumnFromDocument()
    {
        // "ab\ncd"  – 'c' is at offset 3 → line 2, column 1
        var doc = new StringDocument(string.Empty, "ab\ncd");
        var loc = new SourceLocation(doc, 3, 1);

        Assert.Equal(2, loc.Position.Line);
        Assert.Equal(1, loc.Position.Column);
    }

    [Fact]
    public void SourceLocation_SpanProperties_AreConsistent()
    {
        var doc = new StringDocument(string.Empty, "hello world");
        var loc = new SourceLocation(doc, 6, 5); // "world"

        Assert.Equal(6, loc.Start);
        Assert.Equal(5, loc.Length);
        Assert.Equal(11, loc.End);
    }

    [Fact]
    public void SourceLocation_Position_ReportsLineAndColumn()
    {
        var doc = new StringDocument(string.Empty, "hello\nworld");
        var loc = new SourceLocation(doc, 6, 5); // "world" at line 2, col 1

        Assert.Equal(2, loc.Position.Line);
        Assert.Equal(1, loc.Position.Column);
    }

    [Fact]
    public void SourceLocation_FileName_ComesFromDocument()
    {
        var doc = new StringDocument("example.mlir", "hello");
        var loc = new SourceLocation(doc, 0, 5);

        Assert.Equal("example.mlir", loc.Position.Identifier);
    }

    [Fact]
    public void Diagnostic_ToString_IncludesFileNameWhenKnown()
    {
        var doc = new StringDocument("example.mlir", "hello\nworld");
        var diagnostic = new Diagnostic("Something happened.", new SourceLocation(doc, 6, 5));

        Assert.Equal("example.mlir(2,1): Something happened.", diagnostic.ToString());
    }

    [Fact]
    public void SourceLocation_Default_EqualsUnknown()
    {
        var loc = default(SourceLocation);
        Assert.False(loc.IsKnown);
        Assert.Equal(SourceLocation.Unknown, loc);
    }

    // -----------------------------------------------------------------------
    // Token – synthetic vs. source-backed tokens
    // -----------------------------------------------------------------------

    [Fact]
    public void SyntaxToken_Synthetic_HasNoSourceLocation()
    {
        var token = TokenFactory.Identifier("hello");

        Assert.False(token.HasSourceLocation);
        Assert.False(token.Location.IsKnown);
        Assert.Equal(SourceLocation.Unknown, token.Location);
    }

    [Fact]
    public void SyntaxToken_Synthetic_WithLeadingTrivia_HasNoSourceLocation()
    {
        var token = TokenFactory.Identifier("world", "  ");

        Assert.False(token.HasSourceLocation);
        Assert.Equal("  ", token.LeadingTrivia);
        Assert.Equal("world", token.Text);
    }

    [Fact]
    public void SyntaxToken_FullText_CombinesTriviaAndText()
    {
        var token = TokenFactory.Identifier("op", " ");
        Assert.Equal(" op", token.FullText);
    }

    [Fact]
    public void SyntaxToken_WithText_PreservesLeadingTrivia()
    {
        var original = TokenFactory.Identifier("old", "  ");
        var updated = original.WithText("new");

        Assert.Equal("new", updated.Text);
        Assert.Equal("  ", updated.LeadingTrivia);
        Assert.False(updated.HasSourceLocation);
    }

    [Fact]
    public void SyntaxToken_WithText_OnSyntheticToken_ProducesSyntheticToken()
    {
        var token = TokenFactory.Identifier("foo");
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
        Assert.Equal(1, op.NameToken.Location.Position.Line);
        Assert.Equal(6, op.NameToken.Location.Position.Column);
    }

    [Fact]
    public void ParsedTokens_MultiLine_HaveCorrectLineColumn()
    {
        // The operation is on the second line.
        var module = Parser.ParseModule("\n\"test.op\"() : () -> ()");
        var op = module.Operations[0];

        Assert.Equal(2, op.NameToken.Location.Position.Line);
        Assert.Equal(1, op.NameToken.Location.Position.Column);
    }

    [Fact]
    public void ParsedResultToken_HasSourceLocation()
    {
        var module = Parser.ParseModule("%result = \"test.op\"() : () -> i32");
        var op = module.Operations[0];

        var resultToken = op.ResultList[0];
        Assert.True(resultToken.HasSourceLocation);
        Assert.Equal(1, resultToken.Location.Position.Line);
        Assert.Equal(1, resultToken.Location.Position.Column);
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
        Assert.Equal(nameToken.Location.Position.Line, renamed.Location.Position.Line);
        Assert.Equal(nameToken.Location.Position.Column, renamed.Location.Position.Column);
    }

    [Fact]
    public void SourceLocation_SameLine_MultipleLookups_AreConsistent()
    {
        // Calling Line and Column twice on the same location must return the same values.
        var doc = new StringDocument(string.Empty, "abc\ndef");
        var loc = new SourceLocation(doc, 4, 3); // "def" at line 2, col 1

        Assert.Equal(loc.Position.Line, loc.Position.Line);
        Assert.Equal(loc.Position.Column, loc.Position.Column);
    }

    // -----------------------------------------------------------------------
    // SourceLocation.Merge – span-merging helper
    // -----------------------------------------------------------------------

    [Fact]
    public void SourceLocation_Merge_BothUnknown_ReturnsUnknown()
    {
        var result = SourceLocation.Merge(SourceLocation.Unknown, SourceLocation.Unknown);
        Assert.False(result.IsKnown);
    }

    [Fact]
    public void SourceLocation_Merge_FirstUnknown_ReturnsSecond()
    {
        var doc = new StringDocument(string.Empty, "hello world");
        var second = new SourceLocation(doc, 6, 5);

        var result = SourceLocation.Merge(SourceLocation.Unknown, second);

        Assert.Equal(second, result);
    }

    [Fact]
    public void SourceLocation_Merge_SecondUnknown_ReturnsFirst()
    {
        var doc = new StringDocument(string.Empty, "hello world");
        var first = new SourceLocation(doc, 0, 5);

        var result = SourceLocation.Merge(first, SourceLocation.Unknown);

        Assert.Equal(first, result);
    }

    [Fact]
    public void SourceLocation_Merge_AdjacentSpans_CoversFullRange()
    {
        // "hello world": merge "hello" (0..5) with "world" (6..11) → full string (0..11)
        var doc = new StringDocument(string.Empty, "hello world");
        var first = new SourceLocation(doc, 0, 5);  // "hello"
        var second = new SourceLocation(doc, 6, 5); // "world"

        var result = SourceLocation.Merge(first, second);

        Assert.True(result.IsKnown);
        Assert.Equal(0, result.Start);
        Assert.Equal(11, result.End);
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void SourceLocation_Merge_OverlappingSpans_CoversFullRange()
    {
        var doc = new StringDocument(string.Empty, "abcdef");
        var first = new SourceLocation(doc, 1, 3);  // "bcd" (1..4)
        var second = new SourceLocation(doc, 2, 3); // "cde" (2..5)

        var result = SourceLocation.Merge(first, second);

        Assert.Equal(1, result.Start);
        Assert.Equal(5, result.End);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void SourceLocation_Merge_DifferentDocuments_ReturnsFirst()
    {
        var doc1 = new StringDocument(string.Empty, "abc");
        var doc2 = new StringDocument(string.Empty, "abc");
        var first = new SourceLocation(doc1, 0, 1);
        var second = new SourceLocation(doc2, 0, 1);

        var result = SourceLocation.Merge(first, second);

        // Cannot merge locations from different documents; first is returned unchanged.
        Assert.Same(doc1, result.Document);
        Assert.Equal(first.Start, result.Start);
        Assert.Equal(first.Length, result.Length);
    }

    // -----------------------------------------------------------------------
    // CST node merged spans – verified against parsed operations/nodes
    // -----------------------------------------------------------------------

    [Fact]
    public void OperationSyntax_Location_CoversFullSpanWithResult()
    {
        // "%0 = \"test.op\"() : () -> i32"
        // %0 starts at col 1, i32 ends at col 29 → span is 0..29 (length 29)
        var module = Parser.ParseModule("%0 = \"test.op\"() : () -> i32");
        var op = module.Operations[0];

        Assert.True(op.Location.IsKnown);
        // Starts at the first result token (%0 at offset 0)
        Assert.Equal(0, op.Location.Start);
        // Ends after the full trailing type signature
        Assert.Equal(5, module.Operations[0].NameToken.Location.Start); // "test.op" starts at offset 5
        Assert.True(op.Location.End > op.NameToken.Location.End);
    }

    [Fact]
    public void OperationSyntax_Location_NoResults_StartsAtNameToken()
    {
        // "\"test.op\"() : () -> ()"
        // Name token starts at offset 0
        var module = Parser.ParseModule("\"test.op\"() : () -> ()");
        var op = module.Operations[0];

        Assert.True(op.Location.IsKnown);
        Assert.Equal(op.NameToken.Location.Start, op.Location.Start);
        // End covers the full type signature
        Assert.True(op.Location.End > op.NameToken.Location.End);
    }

    [Fact]
    public void RegionSyntax_Location_CoversOpenToCloseBrace()
    {
        // Parse an operation with a region and verify region location spans { to }
        var src = "\"test.region\"() {\n^bb0:\n  \"test.inner\"() : () -> ()\n} : () -> ()";
        var module = Parser.ParseModule(src);
        var op = module.Operations[0];
        var body = op.Body as MLIR.Syntax.GenericOperationBodySyntax;

        Assert.NotNull(body);
        Assert.NotEmpty(body.Regions);
        var region = body.Regions[0];

        Assert.True(region.Location.IsKnown);
        Assert.Equal(region.OpenBraceToken.Location.Start, region.Location.Start);
        Assert.Equal(region.CloseBraceToken.Location.End, region.Location.End);
    }

    [Fact]
    public void BlockSyntax_Location_CoversLabelThroughLastOperation()
    {
        // A block with a label and one operation
        var src = "\"test.op\"() {\n^bb0:\n  \"test.inner\"() : () -> ()\n} : () -> ()";
        var module = Parser.ParseModule(src);
        var op = module.Operations[0];
        var body = op.Body as MLIR.Syntax.GenericOperationBodySyntax;
        var block = body!.Regions[0].Blocks[0];

        Assert.True(block.Location.IsKnown);
        // Block label starts at the '^' token
        Assert.Equal(block.LabelToken.Location.Start, block.Location.Start);
        // End extends to the last operation's end
        var lastOp = block.Operations[0];
        Assert.Equal(lastOp.Location.End, block.Location.End);
    }

    [Fact]
    public void RawSyntaxText_Location_MergesAllTokens()
    {
        // Verify that RawSyntaxText.Location covers its full token range
        var module = Parser.ParseModule("\"test.op\"() : () -> i32");
        var op = module.Operations[0];
        var body = op.Body as MLIR.Syntax.GenericOperationBodySyntax;
        var rawType = body!.TypeSignatureSyntax as MLIR.Syntax.Types.Collections.FunctionTypeSyntax;

        // The function type starts at '(' and ends at 'i32'
        Assert.NotNull(rawType);
        Assert.True(rawType.Location.IsKnown);
        Assert.True(rawType.Location.End > rawType.Location.Start);
    }

    [Fact]
    public void SourceLocation_Merge_IsCommutative()
    {
        // Merge(a, b) should equal Merge(b, a) for same-document spans.
        var doc = new StringDocument(string.Empty, "hello world");
        var a = new SourceLocation(doc, 0, 5);
        var b = new SourceLocation(doc, 6, 5);

        var ab = SourceLocation.Merge(a, b);
        var ba = SourceLocation.Merge(b, a);

        Assert.Equal(ab.Start, ba.Start);
        Assert.Equal(ab.End, ba.End);
    }

    private static void AssertPosition(SourcePosition position, string? fileName, int line, int column)
    {
        Assert.Equal(fileName ?? string.Empty, position.Identifier ?? string.Empty);
        Assert.Equal(line, position.Line);
        Assert.Equal(column, position.Column);
    }

    private sealed class FixedDerivedSourceDocument : DerivedSourceDocument
    {
        private readonly string text;
        private readonly OriginalSourceSpan primarySpan;
        private readonly OriginalSourceSpan[] originSpans;

        public FixedDerivedSourceDocument(string text, OriginalSourceSpan primarySpan, params OriginalSourceSpan[] additionalSpans)
        {
            this.text = text;
            this.primarySpan = primarySpan;
            originSpans = new OriginalSourceSpan[additionalSpans.Length + 1];
            originSpans[0] = primarySpan;
            for (var i = 0; i < additionalSpans.Length; i++)
            {
                originSpans[i + 1] = additionalSpans[i];
            }
        }

        public override string Identifier => "derived";

        public override int Length => text.Length;

        public override TextReader Open(int offset)
        {
            var reader = new StringReader(text);
            while (offset > 0)
            {
                reader.Read();
                offset--;
            }

            return reader;
        }

        public override string GetText(int offset, int length)
        {
            return text.Substring(offset, length);
        }

        public override ResolvedSourceSpan ResolveSpan(int start, int length)
        {
            return new ResolvedSourceSpan(primarySpan, originSpans);
        }
    }
}
