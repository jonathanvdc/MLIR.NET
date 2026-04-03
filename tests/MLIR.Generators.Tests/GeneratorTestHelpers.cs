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
        return RunGeneratorDetailed(generator, ensureUpstreamPrelude: true, additionalTexts)
            .Results
            .Single()
            .GeneratedSources;
    }

    public static ImmutableArray<GeneratedSourceResult> RunGeneratorRaw(IIncrementalGenerator generator, params (string path, string text)[] additionalTexts)
    {
        return RunGeneratorDetailed(generator, ensureUpstreamPrelude: false, additionalTexts)
            .Results
            .Single()
            .GeneratedSources;
    }

    public static GeneratorDriverRunResult RunGeneratorDetailed(
        IIncrementalGenerator generator,
        bool ensureUpstreamPrelude,
        params (string path, string text)[] additionalTexts)
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
        return driver.GetRunResult();
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
        return Document.Load(EnsureUpstreamPrelude(source), new DictionaryIncludeResolver(PreludeFiles));
    }

    public static Document LoadTableGenFromPrelude(string source)
    {
        return Document.Load(source, new DictionaryIncludeResolver(PreludeFiles));
    }

    private static string EnsureUpstreamPrelude(string source)
    {
        return source.Contains("include \"mlir/IR/OpBase.td\"", System.StringComparison.Ordinal)
            ? source
            : UpstreamOpBaseInclude + source;
    }

    private static readonly IReadOnlyDictionary<string, string> PreludeFiles = BuildPreludeFiles();

    private static IReadOnlyDictionary<string, string> BuildPreludeFiles()
    {
        var preludeRoot = Path.Combine(GetRepositoryRoot(), "src", "MLIR.Generators", "Prelude");
        var files = new Dictionary<string, string>(System.StringComparer.Ordinal);

        AddPreludeFiles(files, Path.Combine(preludeRoot, "Include"), static relativePath => relativePath);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Upstream"), static relativePath => relativePath, addOnlyIfMissing: true);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Upstream"), static relativePath => "mlir/Upstream/" + relativePath.Substring("mlir/".Length), addOnlyIfMissing: true);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Extensions"), static relativePath => "mlir/Extensions/" + relativePath.Substring("mlir/".Length), addOnlyIfMissing: true);

        return files;
    }

    private static void AddPreludeFiles(
        Dictionary<string, string> files,
        string directory,
        System.Func<string, string> logicalNameSelector,
        bool addOnlyIfMissing = false)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(directory, "*.td", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directory, path).Replace('\\', '/');
            var logicalName = logicalNameSelector(relativePath);
            if (addOnlyIfMissing && files.ContainsKey(logicalName))
            {
                continue;
            }

            files[logicalName] = File.ReadAllText(path);
        }
    }

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
