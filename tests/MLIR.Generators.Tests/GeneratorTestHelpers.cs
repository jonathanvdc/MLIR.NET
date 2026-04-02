namespace MLIR.Generators.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using TableGen;

internal static class GeneratorTestHelpers
{
    private const string UpstreamOpBaseInclude = "include \"mlir/IR/OpBase.td\"\n\n";

    public static ImmutableArray<GeneratedSourceResult> RunGenerator(IIncrementalGenerator generator, params (string path, string text)[] additionalTexts)
    {
        return RunGeneratorCore(generator, additionalTexts, ensureUpstreamPrelude: true);
    }

    public static ImmutableArray<GeneratedSourceResult> RunGeneratorRaw(IIncrementalGenerator generator, params (string path, string text)[] additionalTexts)
    {
        return RunGeneratorCore(generator, additionalTexts, ensureUpstreamPrelude: false);
    }

    private static ImmutableArray<GeneratedSourceResult> RunGeneratorCore(
        IIncrementalGenerator generator,
        (string path, string text)[] additionalTexts,
        bool ensureUpstreamPrelude)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText("namespace GeneratorHost; public sealed class Placeholder {}")],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            ]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalTexts
                .Select(text => new InMemoryAdditionalText(text.path, ensureUpstreamPrelude ? EnsureUpstreamPrelude(text.text) : text.text))
                .ToImmutableArray());

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Results.Single().GeneratedSources;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText sourceText;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            sourceText = SourceText.From(text);
        }

        public override string Path { get; }

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            return sourceText;
        }
    }

    public static string WithUpstreamOpBasePrelude(string source)
    {
        return EnsureUpstreamPrelude(source);
    }

    public static Document LoadTableGenWithUpstreamPrelude(string source)
    {
        return Document.Load(EnsureUpstreamPrelude(source), new TableGenDictionaryIncludeResolver(PreludeFiles));
    }

    private static string EnsureUpstreamPrelude(string source)
    {
        return source.Contains("include \"mlir/IR/OpBase.td\"", System.StringComparison.Ordinal)
            ? source
            : UpstreamOpBaseInclude + source;
    }

    private static readonly IReadOnlyDictionary<string, string> PreludeFiles = Directory
        .GetFiles(Path.Combine(GetRepositoryRoot(), "src", "MLIR.Generators", "Prelude", "mlir", "IR"), "*.td")
        .ToDictionary(
            static path => "mlir/IR/" + Path.GetFileName(path),
            static path => File.ReadAllText(path));

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MLIR.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            throw new DirectoryNotFoundException("Could not locate the MLIR.NET repository root.");
        }

        return directory.FullName;
    }
}
