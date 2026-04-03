using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TableGen;
using TableGen.Evaluation;

var exitCode = await ProgramMain.RunAsync(args);
Environment.Exit(exitCode);

internal static class ProgramMain
{
    public static Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return Task.FromResult(1);
            }

            return args[0] switch
            {
                "run" => Task.FromResult(RunBenchmarks(args[1..])),
                "compare" => Task.FromResult(CompareBenchmarks(args[1..])),
                _ => Task.FromResult(UnknownCommand(args[0])),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Task.FromResult(1);
        }
    }

    private static int RunBenchmarks(string[] args)
    {
        var options = RunOptions.Parse(args);
        var repoRoot = FindRepositoryRoot();
        var benchmarkContext = new BenchmarkContext(repoRoot);
        var cases = BenchmarkCases.CreateAll(benchmarkContext);
        var results = new List<BenchmarkResult>(cases.Count);

        foreach (var @case in cases)
        {
            var warmupSamples = new List<double>(options.WarmupCount);
            for (var i = 0; i < options.WarmupCount; i++)
            {
                warmupSamples.Add(RunIteration(@case.Action));
            }

            var iterationSamples = new List<double>(options.IterationCount);
            for (var i = 0; i < options.IterationCount; i++)
            {
                iterationSamples.Add(RunIteration(@case.Action));
            }

            results.Add(BenchmarkResult.FromSamples(@case.Name, @case.Description, warmupSamples, iterationSamples));
        }

        var report = new BenchmarkReport(
            Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
            Environment.MachineName,
            DateTimeOffset.UtcNow,
            options.WarmupCount,
            options.IterationCount,
            results);

        WriteJson(options.OutputPath, report);
        WriteRunSummary(report);
        return 0;
    }

    private static int CompareBenchmarks(string[] args)
    {
        var options = CompareOptions.Parse(args);
        var baseline = ReadJson<BenchmarkReport>(options.BaselinePath);
        var candidate = ReadJson<BenchmarkReport>(options.CandidatePath);
        var markdown = BenchmarkComparison.CreateMarkdown(baseline, candidate);

        if (options.OutputPath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);
            File.WriteAllText(options.OutputPath, markdown);
        }

        Console.WriteLine(markdown);
        return 0;
    }

    private static double RunIteration(Action action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static void WriteRunSummary(BenchmarkReport report)
    {
        Console.WriteLine("TableGen benchmark summary");
        foreach (var result in report.Results)
        {
            Console.WriteLine(
                $"{result.Name}: mean {result.MeanMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms, " +
                $"median {result.MedianMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms");
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(value, JsonOptions.Instance);
        File.WriteAllText(path, json);
    }

    private static T ReadJson<T>(string path)
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions.Instance)
            ?? throw new InvalidOperationException($"Failed to deserialize JSON from '{path}'.");
    }

    private static string FindRepositoryRoot()
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

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- run --output <path> [--warmup N] [--iterations N]");
        Console.Error.WriteLine("  dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- compare --baseline <path> --candidate <path> [--output <path>]");
    }
}

internal sealed record RunOptions(string OutputPath, int WarmupCount, int IterationCount)
{
    public static RunOptions Parse(string[] args)
    {
        string? output = null;
        var warmup = 3;
        var iterations = 8;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                    output = args[++i];
                    break;
                case "--warmup":
                    warmup = int.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;
                case "--iterations":
                    iterations = int.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown run option '{args[i]}'.");
            }
        }

        if (string.IsNullOrEmpty(output))
        {
            throw new InvalidOperationException("Missing required '--output' argument.");
        }

        return new RunOptions(output, warmup, iterations);
    }
}

internal sealed record CompareOptions(string BaselinePath, string CandidatePath, string? OutputPath)
{
    public static CompareOptions Parse(string[] args)
    {
        string? baseline = null;
        string? candidate = null;
        string? output = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--baseline":
                    baseline = args[++i];
                    break;
                case "--candidate":
                    candidate = args[++i];
                    break;
                case "--output":
                    output = args[++i];
                    break;
                default:
                    throw new InvalidOperationException($"Unknown compare option '{args[i]}'.");
            }
        }

        if (string.IsNullOrEmpty(baseline) || string.IsNullOrEmpty(candidate))
        {
            throw new InvalidOperationException("Both '--baseline' and '--candidate' are required.");
        }

        return new CompareOptions(baseline, candidate, output);
    }
}

internal sealed class BenchmarkContext
{
    private int sink;
    private readonly Lazy<IReadOnlyDictionary<string, string>> preludeFiles;

    public BenchmarkContext(string repositoryRoot)
    {
        RepositoryRoot = repositoryRoot;
        preludeFiles = new Lazy<IReadOnlyDictionary<string, string>>(LoadPreludeFiles);
    }

    public string RepositoryRoot { get; }

    public string BenchmarkRoot => Path.Combine(RepositoryRoot, "tools", "TableGen.Benchmarks");

    public IReadOnlyDictionary<string, string> PreludeFiles => preludeFiles.Value;

    public void Consume(InterpretedDocument document)
    {
        var value = document.Records.Count;
        foreach (var record in document.Records)
        {
            value += record.Name.Length;
            value += record.Fields.Count;
        }

        sink ^= value;
    }

    private IReadOnlyDictionary<string, string> LoadPreludeFiles()
    {
        var root = Path.Combine(RepositoryRoot, "src", "MLIR.Generators", "Prelude");
        return Directory
            .GetFiles(Path.Combine(root, "mlir"), "*.td", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace('\\', '/'),
                static path => File.ReadAllText(path));
    }
}

internal sealed record BenchmarkCase(string Name, string Description, Action Action);

internal static class BenchmarkCases
{
    public static IReadOnlyList<BenchmarkCase> CreateAll(BenchmarkContext context)
    {
        var manifests = Directory
            .GetFiles(Path.Combine(context.BenchmarkRoot, "Cases"), "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(path => LoadManifest(context, path))
            .ToArray();
        return manifests.Select(manifest => CreateCase(context, manifest)).ToArray();
    }

    private static BenchmarkCase CreateCase(BenchmarkContext context, BenchmarkManifest manifest)
    {
        var sourcePath = Path.Combine(context.BenchmarkRoot, manifest.Source.Replace('/', Path.DirectorySeparatorChar));
        var sourceText = File.ReadAllText(sourcePath);
        var resolver = manifest.IncludeResolver switch
        {
            "mlir-prelude" => new TableGenDictionaryIncludeResolver(context.PreludeFiles),
            "none" => null,
            _ => throw new InvalidOperationException($"Unknown include resolver '{manifest.IncludeResolver}' in benchmark '{manifest.Name}'."),
        };

        return manifest.Mode switch
        {
            "evaluate" => CreateEvaluateCase(context, manifest, sourceText, resolver),
            "parse-and-evaluate" => CreateParseAndEvaluateCase(context, manifest, sourceText, resolver),
            _ => throw new InvalidOperationException($"Unknown benchmark mode '{manifest.Mode}' for '{manifest.Name}'."),
        };
    }

    private static BenchmarkCase CreateEvaluateCase(
        BenchmarkContext context,
        BenchmarkManifest manifest,
        string sourceText,
        TableGenIncludeResolver? resolver)
    {
        var document = resolver == null
            ? Document.Parse(sourceText)
            : Document.Load(sourceText, resolver);
        return new BenchmarkCase(
            manifest.Name,
            manifest.Description,
            () => context.Consume(document.Evaluate()));
    }

    private static BenchmarkCase CreateParseAndEvaluateCase(
        BenchmarkContext context,
        BenchmarkManifest manifest,
        string sourceText,
        TableGenIncludeResolver? resolver)
    {
        return new BenchmarkCase(
            manifest.Name,
            manifest.Description,
            () =>
            {
                var document = resolver == null
                    ? Document.Parse(sourceText)
                    : Document.Load(sourceText, resolver);
                context.Consume(document.Evaluate());
            });
    }

    private static BenchmarkManifest LoadManifest(BenchmarkContext context, string manifestPath)
    {
        var manifest = JsonSerializer.Deserialize<BenchmarkManifest>(File.ReadAllText(manifestPath), JsonOptions.Instance)
            ?? throw new InvalidOperationException($"Failed to deserialize benchmark manifest '{manifestPath}'.");

        if (string.IsNullOrWhiteSpace(manifest.Name) ||
            string.IsNullOrWhiteSpace(manifest.Description) ||
            string.IsNullOrWhiteSpace(manifest.Mode) ||
            string.IsNullOrWhiteSpace(manifest.Source) ||
            string.IsNullOrWhiteSpace(manifest.IncludeResolver))
        {
            throw new InvalidOperationException($"Benchmark manifest '{manifestPath}' is missing one or more required properties.");
        }

        return manifest;
    }
}

internal sealed record BenchmarkManifest(
    string Name,
    string Description,
    string Mode,
    string Source,
    string IncludeResolver);

internal sealed record BenchmarkReport(
    string Commit,
    string MachineName,
    DateTimeOffset CreatedAtUtc,
    int WarmupCount,
    int IterationCount,
    IReadOnlyList<BenchmarkResult> Results);

internal sealed record BenchmarkResult(
    string Name,
    string Description,
    IReadOnlyList<double> WarmupSamples,
    IReadOnlyList<double> IterationSamples,
    double MeanMilliseconds,
    double MedianMilliseconds,
    double MinMilliseconds,
    double MaxMilliseconds)
{
    public static BenchmarkResult FromSamples(
        string name,
        string description,
        IReadOnlyList<double> warmupSamples,
        IReadOnlyList<double> iterationSamples)
    {
        var ordered = iterationSamples.OrderBy(static sample => sample).ToArray();
        var mean = iterationSamples.Average();
        var median = ordered.Length % 2 == 0
            ? (ordered[(ordered.Length / 2) - 1] + ordered[ordered.Length / 2]) / 2.0
            : ordered[ordered.Length / 2];
        return new BenchmarkResult(
            name,
            description,
            warmupSamples.ToArray(),
            iterationSamples.ToArray(),
            mean,
            median,
            ordered.First(),
            ordered.Last());
    }
}

internal static class BenchmarkComparison
{
    public static string CreateMarkdown(BenchmarkReport baseline, BenchmarkReport candidate)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## TableGen Interpreter Benchmarks");
        builder.AppendLine();
        builder.AppendLine($"Baseline commit: `{baseline.Commit}`");
        builder.AppendLine($"Candidate commit: `{candidate.Commit}`");
        builder.AppendLine();
        builder.AppendLine("| Benchmark | Baseline (ms) | Candidate (ms) | Delta | Status |");
        builder.AppendLine("| --- | ---: | ---: | ---: | --- |");

        var baselineByName = baseline.Results.ToDictionary(static result => result.Name);
        foreach (var candidateResult in candidate.Results)
        {
            if (!baselineByName.TryGetValue(candidateResult.Name, out var baselineResult))
            {
                continue;
            }

            var deltaRatio = (candidateResult.MeanMilliseconds - baselineResult.MeanMilliseconds) / baselineResult.MeanMilliseconds;
            var deltaText = deltaRatio.ToString("+0.0%;-0.0%;0.0%", CultureInfo.InvariantCulture);
            var status = deltaRatio switch
            {
                > 0.05 => "Regression",
                < -0.05 => "Improvement",
                _ => "Flat",
            };

            builder.AppendLine(
                $"| {candidateResult.Name} | " +
                $"{baselineResult.MeanMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} | " +
                $"{candidateResult.MeanMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} | " +
                $"{deltaText} | {status} |");
        }

        builder.AppendLine();
        builder.AppendLine("Interpretation:");
        builder.AppendLine("- `Regression` means candidate mean time is more than 5% slower than baseline.");
        builder.AppendLine("- `Improvement` means candidate mean time is more than 5% faster than baseline.");
        builder.AppendLine("- `Flat` means the change stayed within a 5% noise band.");
        return builder.ToString();
    }
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}
