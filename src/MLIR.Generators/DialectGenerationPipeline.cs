namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MLIR.ODS;
using MLIR.ODS.Model;
using TableGen;

internal static class DialectGenerationPipeline
{
    public static IReadOnlyList<DialectModel> ParseAndMerge(
        ImmutableArray<AdditionalText> additionalTexts,
        System.Threading.CancellationToken cancellationToken,
        SourceProductionContext productionContext)
    {
        var includeResolver = BuildIncludeResolver(additionalTexts, cancellationToken);
        var results = ImmutableArray.CreateRange(
            additionalTexts,
            static (file, state) => DialectGeneratorInput.ParseFile(file, state.Resolver, state.Token),
            (Resolver: includeResolver, Token: cancellationToken));

        return MergeDialects(
            results,
            result => productionContext.ReportDiagnostic(
                Diagnostic.Create(
                    DialectGeneratorDiagnostics.InvalidTableGenInput,
                    CreateLocation(result),
                    result.Path,
                    result.ErrorMessage))).ToArray();
    }

    internal static IReadOnlyList<DialectModel> ParseAndMerge(
        IReadOnlyList<TableGenInput> inputs,
        IncludeResolver includeResolver)
    {
        return ParseAndMergeDetailed(inputs, includeResolver).Dialects;
    }

    internal static DialectMergeResult ParseAndMergeDetailed(
        IReadOnlyList<TableGenInput> inputs,
        IncludeResolver includeResolver)
    {
        var results = inputs
            .Select(input => DialectGeneratorInput.ParseFile(input, includeResolver))
            .ToArray();
        var diagnostics = new List<Diagnostic>();
        var dialects = MergeDialects(
            results,
            result => diagnostics.Add(
                Diagnostic.Create(
                    DialectGeneratorDiagnostics.InvalidTableGenInput,
                    CreateLocation(result),
                    result.Path,
                    result.ErrorMessage!))).ToArray();
        return new DialectMergeResult(dialects, diagnostics);
    }

    private static IncludeResolver BuildIncludeResolver(
        ImmutableArray<AdditionalText> additionalTexts,
        System.Threading.CancellationToken cancellationToken)
    {
        return new CompositeIncludeResolver(
            new ConsumerFileResolver(additionalTexts, cancellationToken),
            new EmbeddedPreludeResolver());
    }

    private static IEnumerable<DialectModel> MergeDialects(
        IReadOnlyList<ParsedDialectFile> results,
        Action<ParsedDialectFile>? reportError = null)
    {
        var dialects = new List<DialectModel>();
        foreach (var result in results)
        {
            if (result.ErrorMessage != null)
            {
                reportError?.Invoke(result);
                continue;
            }

            dialects.AddRange(result.Dialects);
        }

        return dialects
            .GroupBy(static dialect => dialect.Name, StringComparer.Ordinal)
            .Select(DialectModelMerger.MergeDialectGroup)
            .OrderBy(static dialect => dialect.Name, StringComparer.Ordinal);
    }

    private static Location CreateLocation(ParsedDialectFile result)
    {
        var diagnostic = result.TableGenDiagnostic;
        if (diagnostic == null || !diagnostic.Location.IsKnown)
        {
            return Location.None;
        }

        var resolved = diagnostic.Location.Resolve();
        if (resolved == null)
        {
            return Location.None;
        }

        var primary = resolved.PrimarySpan;
        var start = primary.Document.GetPosition(primary.Start);
        var end = primary.Document.GetPosition(primary.End);
        var fileName = start.Identifier;
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = result.Path;
        }

        return Location.Create(
            fileName!,
            new TextSpan(primary.Start, primary.Length),
            new LinePositionSpan(
                new LinePosition(Math.Max(0, start.Line - 1), Math.Max(0, start.Column - 1)),
                new LinePosition(Math.Max(0, end.Line - 1), Math.Max(0, end.Column - 1))));
    }
}

internal sealed class DialectMergeResult
{
    public DialectMergeResult(IReadOnlyList<DialectModel> dialects, IReadOnlyList<Diagnostic> diagnostics)
    {
        Dialects = dialects;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<DialectModel> Dialects { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
