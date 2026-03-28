namespace MLIR.Generators.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

internal static class GeneratorTestHelpers
{
    public static ImmutableArray<GeneratedSourceResult> RunGenerator(IIncrementalGenerator generator, params (string path, string text)[] additionalTexts)
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
            additionalTexts: additionalTexts.Select(static text => new InMemoryAdditionalText(text.path, text.text)).ToImmutableArray());

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
}
