namespace TableGen.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Text;
using TableGen.Evaluation;
using TableGen.Syntax;
using Xunit;

public sealed class IncludeTests
{
    [Fact]
    public void LexerRecognizesIncludeKeyword()
    {
        // include is parsed as an IncludeDirectiveSyntax, not as an identifier.
        var document = Document.Parse("include \"foo.td\"").Value;

        Assert.Single(document.Syntax.Declarations);
        var include = Assert.IsType<IncludeDirectiveSyntax>(document.Syntax.Declarations[0]);
        Assert.Equal("foo.td", include.Path);
    }

    [Fact]
    public void ParserProducesIncludeDirectiveSyntaxNode()
    {
        const string source = "include \"mlir/IR/OpBase.td\"";

        var document = Document.Parse(source).Value;

        Assert.Single(document.Syntax.Declarations);
        var include = Assert.IsType<IncludeDirectiveSyntax>(document.Syntax.Declarations[0]);
        Assert.Equal("mlir/IR/OpBase.td", include.Path);
    }

    [Fact]
    public void ParserMixesIncludeDirectivesAndOtherDeclarations()
    {
        const string source =
            "include \"base.td\"\n" +
            "def Example { int Width = 4; };";

        var document = Document.Parse(source).Value;

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

        var resolver = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["base.td"] = baseSource });

        var document = Document.Load(mainSource, resolver).Value;

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

        var resolver = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["base.td"] = baseSource });

        var document = Document.Load(mainSource, resolver).Value;
        var record = document.Evaluate().Value.Records.Single();

        Assert.Equal("Example", record.Name);
        Assert.Equal("hello", Assert.IsType<StringValue>(record.GetField("Tag")).Value);
    }

    [Fact]
    public void LoadDeduplicatesViaIncludeGuards()
    {
        // When a file has proper #ifndef include guards, including it twice should
        // result in exactly one expansion.
        const string sharedSource =
            "#ifndef SHARED_TD\n" +
            "#define SHARED_TD\n" +
            "def Shared { int X = 1; };\n" +
            "#endif\n";
        const string mainSource =
            "include \"shared.td\"\n" +
            "include \"shared.td\"\n";

        var resolver = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["shared.td"] = sharedSource });

        var document = Document.Load(mainSource, resolver).Value;

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

        var resolver = new DictionaryIncludeResolver(new Dictionary<string, string>
        {
            ["level1.td"] = level1,
            ["level2.td"] = level2,
        });

        var document = Document.Load(main, resolver).Value;

        // Declarations appear in include-expansion order: L2, L1, L0.
        var names = document.Evaluate().Value.Records.Select(static r => r.Name).ToArray();
        Assert.Equal(["L2", "L1", "L0"], names);
    }

    [Fact]
    public void LoadThrowsOnUnresolvedInclude()
    {
        const string mainSource = "include \"missing.td\"";

        var resolver = new DictionaryIncludeResolver(
            new Dictionary<string, string>());

        var diagnostic = TestHelpers.LoadFailure(mainSource, resolver);

        Assert.Contains("missing.td", diagnostic.Message);
    }

    [Fact]
    public void LoadDiagnosticIncludesIncludingFileWhenKnown()
    {
        const string mainSource = "include \"missing.td\"";

        var resolver = new DictionaryIncludeResolver(
            new Dictionary<string, string>());

        var sourceDocument = new OriginalSourceDocument(mainSource, "my_ops.td");

        var diagnostic = TestHelpers.LoadFailure(sourceDocument, resolver);

        Assert.Contains("missing.td", diagnostic.Message);
        Assert.Equal("my_ops.td", diagnostic.FileName);
    }

    [Fact]
    public void LoadParseDiagnosticIncludesIncludedFileNameWhenKnown()
    {
        const string mainSource = "include \"dep.td\"";
        const string dependencySource = "def Broken : Base<1;";

        var resolver = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["dep.td"] = dependencySource });

        var diagnostic = TestHelpers.LoadFailure(new OriginalSourceDocument(mainSource, "main.td"), resolver);

        Assert.Contains("Expected '>' to close the argument list.", diagnostic.Message);
        Assert.Equal("dep.td", diagnostic.FileName);
    }

    [Fact]
    public void LoadParseDiagnosticAfterPreprocessingResolvesToOriginalSource()
    {
        const string source =
            "#define FEATURE\n" +
            "    def Broken : Base<1;\n";

        var diagnostic = TestHelpers.LoadFailure(new OriginalSourceDocument(source, "main.td"), new DictionaryIncludeResolver(new Dictionary<string, string>()));
        var resolved = diagnostic.Location.Resolve();

        Assert.Contains("Expected '>' to close the argument list.", diagnostic.Message);
        Assert.Equal("main.td", diagnostic.FileName);
        Assert.Equal(2, diagnostic.Line);
        Assert.IsType<PreprocessedSourceDocument>(diagnostic.Location.Document);
        Assert.NotNull(resolved);
        Assert.Equal("main.td", resolved!.PrimarySpan.Document.FileName);
        Assert.True(resolved.PrimarySpan.Start > diagnostic.Location.Start);
    }

    [Fact]
    public void LoadParseDiagnosticInPreprocessedIncludeResolvesToIncludedOriginalSource()
    {
        const string mainSource = "include \"dep.td\"";
        const string dependencySource =
            "#define FEATURE\n" +
            "    def Broken : Base<1;\n";

        var resolver = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["dep.td"] = dependencySource });

        var diagnostic = TestHelpers.LoadFailure(new OriginalSourceDocument(mainSource, "main.td"), resolver);
        var resolved = diagnostic.Location.Resolve();

        Assert.Contains("Expected '>' to close the argument list.", diagnostic.Message);
        Assert.Equal("dep.td", diagnostic.FileName);
        Assert.Equal(2, diagnostic.Line);
        Assert.IsType<PreprocessedSourceDocument>(diagnostic.Location.Document);
        Assert.NotNull(resolved);
        Assert.Equal("dep.td", resolved!.PrimarySpan.Document.FileName);
    }

    [Fact]
    public void LoadParseDiagnosticAfterInactiveMultiLineConditionalResolvesToOriginalSource()
    {
        const string source =
            "#ifdef DISABLED\n" +
            "def SkippedA { int X = 1; };\n" +
            "def SkippedB { int X = 2; };\n" +
            "#endif\n" +
            "    def Broken : Base<1;\n";

        var diagnostic = TestHelpers.LoadFailure(new OriginalSourceDocument(source, "conditional.td"), new DictionaryIncludeResolver(new Dictionary<string, string>()));
        var resolved = diagnostic.Location.Resolve();

        Assert.Contains("Expected '>' to close the argument list.", diagnostic.Message);
        Assert.Equal("conditional.td", diagnostic.FileName);
        Assert.Equal(5, diagnostic.Line);
        Assert.IsType<PreprocessedSourceDocument>(diagnostic.Location.Document);
        Assert.NotNull(resolved);
        Assert.Equal("conditional.td", resolved!.PrimarySpan.Document.FileName);
        Assert.True(resolved.PrimarySpan.Start > diagnostic.Location.Start);
    }

    [Fact]
    public void CompositeResolverTriesResolversInOrder()
    {
        const string firstSource = "def FromFirst { int X = 1; };";
        const string secondSource = "def FromSecond { int X = 2; };";

        var first = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["shared.td"] = firstSource });
        var second = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["shared.td"] = secondSource });

        var composite = new CompositeIncludeResolver(first, second);
        var document = Document.Load("include \"shared.td\"", composite).Value;

        // The first resolver wins.
        var record = document.Evaluate().Value.Records.Single();
        Assert.Equal("FromFirst", record.Name);
        Assert.Equal(1L, Assert.IsType<IntegerValue>(record.GetField("X")).Value);
    }

    [Fact]
    public void CompositeResolverFallsBackToSecondResolver()
    {
        const string secondSource = "def FromSecond { int X = 2; };";

        var first = new DictionaryIncludeResolver(
            new Dictionary<string, string>());
        var second = new DictionaryIncludeResolver(
            new Dictionary<string, string> { ["only-in-second.td"] = secondSource });

        var composite = new CompositeIncludeResolver(first, second);
        var document = Document.Load("include \"only-in-second.td\"", composite).Value;

        var record = document.Evaluate().Value.Records.Single();
        Assert.Equal("FromSecond", record.Name);
    }

    [Fact]
    public void LoadPassesIncludingFileContextToResolver()
    {
        var trackingResolver = new TrackingResolver(
            "dep.td",
            "def Dep { int X = 1; };");

        var mainSource = "include \"dep.td\"";
        var mainDocument = new OriginalSourceDocument(mainSource, "main.td");
        var result = Document.Load(mainDocument, trackingResolver);

        Assert.True(result.IsSuccess);
        Assert.NotNull(trackingResolver.CapturedIncludingFile);
        Assert.Equal("main.td", trackingResolver.CapturedIncludingFile!.FileName);
    }

    // -----------------------------------------------------------------------
    // TableGenPreprocessor tests
    // -----------------------------------------------------------------------

    [Fact]
    public void PreprocessorPassesThroughSourceWithNoDirectives()
    {
        const string source = "def Foo { int X = 1; };";
        var defines = new System.Collections.Generic.HashSet<string>();
        var result = Preprocessor.Process(source, defines);
        Assert.Contains("def Foo", result);
    }

    [Fact]
    public void PreprocessorHandlesDefineDirective()
    {
        const string source = "#define MY_SYMBOL\ndef Foo { int X = 1; };";
        var defines = new System.Collections.Generic.HashSet<string>();
        Preprocessor.Process(source, defines);
        Assert.Contains("MY_SYMBOL", defines);
    }

    [Fact]
    public void PreprocessorIfndefSkipsBlockWhenSymbolDefined()
    {
        const string source =
            "#ifndef MY_SYMBOL\n" +
            "def ShouldBeSkipped { int X = 1; };\n" +
            "#endif\n";
        var defines = new System.Collections.Generic.HashSet<string> { "MY_SYMBOL" };
        var result = Preprocessor.Process(source, defines);
        Assert.DoesNotContain("ShouldBeSkipped", result);
    }

    [Fact]
    public void PreprocessorIfndefIncludesBlockWhenSymbolNotDefined()
    {
        const string source =
            "#ifndef MY_SYMBOL\n" +
            "def ShouldBeIncluded { int X = 1; };\n" +
            "#endif\n";
        var defines = new System.Collections.Generic.HashSet<string>();
        var result = Preprocessor.Process(source, defines);
        Assert.Contains("ShouldBeIncluded", result);
    }

    [Fact]
    public void PreprocessorElseBranchIsActiveWhenIfBranchIsNot()
    {
        const string source =
            "#ifndef MY_SYMBOL\n" +
            "def IfBranch { int X = 1; };\n" +
            "#else\n" +
            "def ElseBranch { int X = 2; };\n" +
            "#endif\n";
        var defines = new System.Collections.Generic.HashSet<string> { "MY_SYMBOL" };
        var result = Preprocessor.Process(source, defines);
        Assert.DoesNotContain("IfBranch", result);
        Assert.Contains("ElseBranch", result);
    }

    [Fact]
    public void PreprocessorHandlesNestedConditionals()
    {
        const string source =
            "#ifndef OUTER\n" +
            "  #ifndef INNER\n" +
            "  def BothAbsent { int X = 1; };\n" +
            "  #endif\n" +
            "#endif\n";
        var defines = new System.Collections.Generic.HashSet<string>();
        var result = Preprocessor.Process(source, defines);
        Assert.Contains("BothAbsent", result);
    }

    [Fact]
    public void PreprocessorIncludeGuardPreventsDoubleExpansionWhenIncludedTwice()
    {
        const string guardedSource =
            "#ifndef GUARDED_TD\n" +
            "#define GUARDED_TD\n" +
            "def GuardedDef { int X = 1; };\n" +
            "#endif\n";
        const string mainSource =
            "include \"guarded.td\"\n" +
            "include \"guarded.td\"\n";

        var resolver = new DictionaryIncludeResolver(
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["guarded.td"] = guardedSource,
            });

        var doc = Document.Load(mainSource, resolver).Value;

        // GuardedDef must appear exactly once.
        Assert.Single(doc.Syntax.Declarations);
    }

    [Fact]
    public void PreprocessorPreservesLineCountForDiagnostics()
    {
        // Inactive lines are replaced with blank lines; line numbers are preserved.
        const string source =
            "#ifndef MY_SYMBOL\n" +
            "def Line2 { int X = 1; };\n" +
            "#endif\n" +
            "def Line4 { int X = 2; };\n";
        var defines = new System.Collections.Generic.HashSet<string> { "MY_SYMBOL" };
        var result = Preprocessor.Process(source, defines);
        var resultLines = result.Split('\n');
        // Line 4 (index 3) of the output should still contain the def.
        Assert.Contains("Line4", resultLines[3]);
    }

    [Fact]
    public void PreprocessorDocumentMapsActiveTextToOriginalSource()
    {
        const string source =
            "#define FEATURE\n" +
            "  def Foo { int X = 1; };\n";
        var sourceDocument = new OriginalSourceDocument(source, "active.td");
        var defines = new HashSet<string>();

        var result = Preprocessor.Process(sourceDocument, defines);
        var outputStart = result.Text.IndexOf("Foo", StringComparison.Ordinal);
        var originalStart = source.IndexOf("Foo", StringComparison.Ordinal);
        var location = new SourceLocation(result, outputStart, "Foo".Length);
        var resolved = location.Resolve();

        Assert.True(outputStart >= 0);
        Assert.True(originalStart >= 0);
        Assert.NotEqual(originalStart, outputStart);
        Assert.Equal("active.td", location.FileName);
        Assert.Equal(2, location.Line);
        Assert.NotNull(resolved);
        Assert.Equal(originalStart, resolved!.PrimarySpan.Start);
        Assert.Equal("Foo".Length, resolved.PrimarySpan.Length);
        Assert.Equal("active.td", resolved.PrimarySpan.Document.FileName);
    }

    [Fact]
    public void PreprocessorDocumentMapsSyntheticDirectiveLineToOriginalLineStart()
    {
        const string source =
            "#define FEATURE\n" +
            "def Foo { int X = 1; };\n";
        var sourceDocument = new OriginalSourceDocument(source, "directive.td");
        var defines = new HashSet<string>();

        var result = Preprocessor.Process(sourceDocument, defines);
        var location = new SourceLocation(result, 0, 1);
        var resolved = location.Resolve();

        Assert.Equal('\n', result.Text[0]);
        Assert.Equal("directive.td", location.FileName);
        Assert.Equal(1, location.Line);
        Assert.Equal(1, location.Column);
        Assert.NotNull(resolved);
        Assert.Equal(0, resolved!.PrimarySpan.Start);
        Assert.Equal(0, resolved.PrimarySpan.Length);
    }

    private sealed class TrackingResolver : IncludeResolver
    {
        private readonly string expectedPath;
        private readonly string resolvedText;

        public TrackingResolver(string expectedPath, string resolvedText)
        {
            this.expectedPath = expectedPath;
            this.resolvedText = resolvedText;
        }

        public SourceDocument? CapturedIncludingFile { get; private set; }

        public override bool TryResolveInclude(
            string includePath,
            SourceDocument? includingFile,
            out SourceDocument resolvedDocument)
        {
            if (includePath == expectedPath)
            {
                CapturedIncludingFile = includingFile;
                resolvedDocument = new OriginalSourceDocument(resolvedText, includePath);
                return true;
            }

            resolvedDocument = null!;
            return false;
        }
    }
}
