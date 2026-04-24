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
        var warmupDuration = TimeSpan.FromMilliseconds(options.WarmupDurationMilliseconds);
        var iterationDuration = TimeSpan.FromMilliseconds(options.IterationDurationMilliseconds);

        foreach (var @case in cases)
        {
            ForceFullGc();

            var warmupSamples = options.WarmupCount is not null
                ? BenchmarkSampling.RunForCount(() => RunIteration(@case.Action), options.WarmupCount.Value)
                : BenchmarkSampling.RunForDuration(() => RunIteration(@case.Action), warmupDuration);

            var iterationSamples = options.IterationCount is not null
                ? BenchmarkSampling.RunForCount(() => RunIteration(@case.Action), options.IterationCount.Value)
                : BenchmarkSampling.RunForDuration(() => RunIteration(@case.Action), iterationDuration);

            results.Add(BenchmarkResult.FromSamples(@case.Name, @case.Description, warmupSamples, iterationSamples));
        }

        var report = new BenchmarkReport(
            Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
            Environment.MachineName,
            DateTimeOffset.UtcNow,
            options.WarmupCount,
            options.IterationCount,
            options.WarmupCount is null ? options.WarmupDurationMilliseconds : null,
            options.IterationCount is null ? options.IterationDurationMilliseconds : null,
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
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static void ForceFullGc()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
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
        Console.Error.WriteLine("  dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- run --output <path> [--warmup N] [--iterations N] [--warmup-duration-ms N] [--duration-ms N]");
        Console.Error.WriteLine("  dotnet run --project tools/TableGen.Benchmarks/TableGen.Benchmarks.csproj -c Release -- compare --baseline <path> --candidate <path> [--output <path>]");
    }
}

internal sealed record RunOptions(
    string OutputPath,
    int? WarmupCount,
    int? IterationCount,
    int WarmupDurationMilliseconds,
    int IterationDurationMilliseconds)
{
    public static RunOptions Parse(string[] args)
    {
        string? output = null;
        int? warmup = null;
        int? iterations = null;
        var warmupDuration = 250;
        var iterationDuration = 1000;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                    output = args[++i];
                    break;
                case "--warmup":
                    warmup = ParsePositiveInt(args, ++i, "--warmup");
                    break;
                case "--iterations":
                    iterations = ParsePositiveInt(args, ++i, "--iterations");
                    break;
                case "--warmup-duration-ms":
                    warmupDuration = ParsePositiveInt(args, ++i, "--warmup-duration-ms");
                    break;
                case "--duration-ms":
                    iterationDuration = ParsePositiveInt(args, ++i, "--duration-ms");
                    break;
                default:
                    throw new InvalidOperationException($"Unknown run option '{args[i]}'.");
            }
        }

        if (string.IsNullOrEmpty(output))
        {
            throw new InvalidOperationException("Missing required '--output' argument.");
        }

        return new RunOptions(output, warmup, iterations, warmupDuration, iterationDuration);
    }

    private static int ParsePositiveInt(string[] args, int index, string optionName)
    {
        if (index >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for '{optionName}'.");
        }

        var value = int.Parse(args[index], CultureInfo.InvariantCulture);
        if (value <= 0)
        {
            throw new InvalidOperationException($"'{optionName}' must be greater than zero.");
        }

        return value;
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
        var preludeRoot = Path.Combine(RepositoryRoot, "src", "MLIR.Generators", "Prelude");
        var files = new Dictionary<string, string>(StringComparer.Ordinal);

        AddPreludeFiles(files, Path.Combine(preludeRoot, "Include"), static relativePath => relativePath);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Upstream"), static relativePath => relativePath, addOnlyIfMissing: true);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Upstream"), static relativePath => "mlir/Upstream/" + relativePath.Substring("mlir/".Length), addOnlyIfMissing: true);
        AddPreludeFiles(files, Path.Combine(preludeRoot, "Extensions"), static relativePath => "mlir/Extensions/" + relativePath.Substring("mlir/".Length), addOnlyIfMissing: true);

        return files;
    }

    private static void AddPreludeFiles(
        Dictionary<string, string> files,
        string directory,
        Func<string, string> logicalNameSelector,
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
            "mlir-prelude" => new DictionaryIncludeResolver(context.PreludeFiles),
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
        IncludeResolver? resolver)
    {
        var document = ParseDocumentForBenchmark(sourceText, resolver);
        return new BenchmarkCase(
            manifest.Name,
            manifest.Description,
            () => context.Consume(EvaluateDocumentForBenchmark(document)));
    }

    private static BenchmarkCase CreateParseAndEvaluateCase(
        BenchmarkContext context,
        BenchmarkManifest manifest,
        string sourceText,
        IncludeResolver? resolver)
    {
        return new BenchmarkCase(
            manifest.Name,
            manifest.Description,
            () =>
            {
                var document = ParseDocumentForBenchmark(sourceText, resolver);
                context.Consume(EvaluateDocumentForBenchmark(document));
            });
    }

    private static InterpretedDocument EvaluateDocumentForBenchmark(Document document)
    {
        var result = document.Evaluate();
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(result.Diagnostic!.ToString());
    }

    private static Document ParseDocumentForBenchmark(string sourceText, IncludeResolver? resolver)
    {
        var result = resolver == null
            ? Document.Parse(sourceText)
            : Document.Load(sourceText, resolver);

        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(result.Diagnostic!.ToString());
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
    int? WarmupCount,
    int? IterationCount,
    int? WarmupDurationMilliseconds,
    int? IterationDurationMilliseconds,
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
        if (string.Equals(baseline.Commit, candidate.Commit, StringComparison.Ordinal))
        {
            builder.AppendLine($"Commit: `{baseline.Commit}`");
        }
        else
        {
            builder.AppendLine($"Baseline commit: `{baseline.Commit}`");
            builder.AppendLine($"Candidate commit: `{candidate.Commit}`");
        }
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

internal static class BenchmarkSampling
{
    public static IReadOnlyList<double> RunForCount(Func<double> measureIteration, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "The sample count must be greater than zero.");
        }

        var samples = new List<double>(count);
        for (var i = 0; i < count; i++)
        {
            samples.Add(measureIteration());
        }

        return samples;
    }

    public static IReadOnlyList<double> RunForDuration(Func<double> measureIteration, TimeSpan targetDuration)
    {
        if (targetDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(targetDuration), "The target duration must be greater than zero.");
        }

        var samples = new List<double>();
        var elapsed = TimeSpan.Zero;

        while (elapsed < targetDuration || samples.Count == 0)
        {
            var sample = measureIteration();
            samples.Add(sample);
            elapsed += TimeSpan.FromMilliseconds(sample);
        }

        return samples;
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
