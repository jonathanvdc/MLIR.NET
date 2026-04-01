namespace TableGen.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using TableGen.Evaluation;
using TableGen.Syntax;
using Xunit;

public sealed class IncludeTests
{
    [Fact]
    public void LexerRecognizesIncludeKeyword()
    {
        // include is parsed as an IncludeDirectiveSyntax, not as an identifier.
        var document = Document.Parse("include \"foo.td\"");

        Assert.Single(document.Syntax.Declarations);
        var include = Assert.IsType<IncludeDirectiveSyntax>(document.Syntax.Declarations[0]);
        Assert.Equal("foo.td", include.Path);
    }

    [Fact]
    public void ParserProducesIncludeDirectiveSyntaxNode()
    {
        const string source = "include \"mlir/OpBase.td\"";

        var document = Document.Parse(source);

        Assert.Single(document.Syntax.Declarations);
        var include = Assert.IsType<IncludeDirectiveSyntax>(document.Syntax.Declarations[0]);
        Assert.Equal("mlir/OpBase.td", include.Path);
    }

    [Fact]
    public void ParserMixesIncludeDirectivesAndOtherDeclarations()
    {
        const string source =
            "include \"base.td\"\n" +
            "def Example { int Width = 4; };";

        var document = Document.Parse(source);

        Assert.Equal(2, document.Syntax.Declarations.Count);
        Assert.IsType<IncludeDirectiveSyntax>(document.Syntax.Declarations[0]);
        Assert.IsType<DefSyntax>(document.Syntax.Declarations[1]);
    }

    [Fact]
    public void LoadExpandsIncludeUsingDictionaryResolver()
    {
        const string baseSource = "def Base { string Tag = \"base\"; };";
        const string mainSource =
            "include \"base.td\"\n" +
            "def Derived { string Tag = \"derived\"; };";

        var resolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string> { ["base.td"] = baseSource });

        var document = Document.Load(mainSource, resolver);

        Assert.Equal(2, document.Syntax.Declarations.Count);
        Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]); // from base.td
        Assert.IsType<DefSyntax>(document.Syntax.Declarations[1]); // from main
    }

    [Fact]
    public void LoadEvaluatesIncludedDeclarations()
    {
        const string baseSource =
            "class Base<string tag> { string Tag = tag; };";
        const string mainSource =
            "include \"base.td\"\n" +
            "def Example : Base<\"hello\">;";

        var resolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string> { ["base.td"] = baseSource });

        var document = Document.Load(mainSource, resolver);
        var record = document.Evaluate().Records.Single();

        Assert.Equal("Example", record.Name);
        Assert.Equal("hello", Assert.IsType<StringValue>(record.GetField("Tag")).Value);
    }

    [Fact]
    public void LoadPreventsDoubleInclusionOfSamePath()
    {
        const string sharedSource = "def Shared { int X = 1; };";
        const string mainSource =
            "include \"shared.td\"\n" +
            "include \"shared.td\"\n";

        var resolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string> { ["shared.td"] = sharedSource });

        var document = Document.Load(mainSource, resolver);

        // The def must appear exactly once despite the double include.
        Assert.Single(document.Syntax.Declarations);
    }

    [Fact]
    public void LoadHandlesTransitiveIncludes()
    {
        const string level2 = "def L2 { int V = 2; };";
        const string level1 =
            "include \"level2.td\"\n" +
            "def L1 { int V = 1; };";
        const string main =
            "include \"level1.td\"\n" +
            "def L0 { int V = 0; };";

        var resolver = new TableGenDictionaryIncludeResolver(new Dictionary<string, string>
        {
            ["level1.td"] = level1,
            ["level2.td"] = level2,
        });

        var document = Document.Load(main, resolver);

        // Declarations appear in include-expansion order: L2, L1, L0.
        var names = document.Evaluate().Records.Select(static r => r.Name).ToArray();
        Assert.Equal(["L2", "L1", "L0"], names);
    }

    [Fact]
    public void LoadThrowsOnUnresolvedInclude()
    {
        const string mainSource = "include \"missing.td\"";

        var resolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string>());

        var exception = Assert.Throws<InvalidOperationException>(
            () => Document.Load(mainSource, resolver));

        Assert.Contains("missing.td", exception.Message);
    }

    [Fact]
    public void LoadDiagnosticIncludesIncludingFileWhenKnown()
    {
        const string mainSource = "include \"missing.td\"";

        var resolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string>());

        var sourceFile = new TableGenSourceFile("my_ops.td");

        var exception = Assert.Throws<InvalidOperationException>(
            () => Document.Load(mainSource, resolver, sourceFile));

        Assert.Contains("missing.td", exception.Message);
        Assert.Contains("my_ops.td", exception.Message);
    }

    [Fact]
    public void CompositeResolverTriesResolversInOrder()
    {
        const string firstSource = "def FromFirst { int X = 1; };";
        const string secondSource = "def FromSecond { int X = 2; };";

        var first = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string> { ["shared.td"] = firstSource });
        var second = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string> { ["shared.td"] = secondSource });

        var composite = new TableGenCompositeIncludeResolver(first, second);
        var document = Document.Load("include \"shared.td\"", composite);

        // The first resolver wins.
        var record = document.Evaluate().Records.Single();
        Assert.Equal("FromFirst", record.Name);
    }

    [Fact]
    public void CompositeResolverFallsBackToSecondResolver()
    {
        const string secondSource = "def FromSecond { int X = 2; };";

        var first = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string>());
        var second = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string> { ["only-in-second.td"] = secondSource });

        var composite = new TableGenCompositeIncludeResolver(first, second);
        var document = Document.Load("include \"only-in-second.td\"", composite);

        var record = document.Evaluate().Records.Single();
        Assert.Equal("FromSecond", record.Name);
    }

    [Fact]
    public void LoadPassesIncludingFileContextToResolver()
    {
        var trackingResolver = new TrackingResolver(
            "dep.td",
            "def Dep { int X = 1; };");

        var mainFile = new TableGenSourceFile("main.td");
        Document.Load("include \"dep.td\"", trackingResolver, mainFile);

        Assert.NotNull(trackingResolver.CapturedIncludingFile);
        Assert.Equal("main.td", trackingResolver.CapturedIncludingFile!.LogicalPath);
    }

    private sealed class TrackingResolver : TableGenIncludeResolver
    {
        private readonly string expectedPath;
        private readonly string resolvedText;

        public TrackingResolver(string expectedPath, string resolvedText)
        {
            this.expectedPath = expectedPath;
            this.resolvedText = resolvedText;
        }

        public TableGenSourceFile? CapturedIncludingFile { get; private set; }

        public override bool TryResolveInclude(
            string includePath,
            TableGenSourceFile? includingFile,
            out TableGenResolvedInclude resolvedInclude)
        {
            if (includePath == expectedPath)
            {
                CapturedIncludingFile = includingFile;
                resolvedInclude = new TableGenResolvedInclude(includePath, resolvedText);
                return true;
            }

            resolvedInclude = null!;
            return false;
        }
    }
}
