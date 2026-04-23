using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLIR.Generators;
using TableGen;

var parseResult = ParseArguments(args);
if (parseResult.ShowHelp)
{
    PrintUsage();
    return parseResult.HelpExitCode;
}

if (parseResult.ErrorMessage != null)
{
    Console.Error.WriteLine(parseResult.ErrorMessage);
    Console.Error.WriteLine();
    PrintUsage();
    return 1;
}

try
{
    var generatedSources = TableGenDialectCompiler.CompileSources(
        parseResult.InputPaths
            .Select(path => Path.GetFullPath(path))
            .Select(path => new TableGenInput(path, File.ReadAllText(path))),
        new CompositeIncludeResolver(
            new FileSystemIncludeResolver(),
            PreludeIncludeResolvers.CreateEmbeddedPreludeResolver()),
        includePrelude: parseResult.IncludePrelude,
        dialectNames: parseResult.DialectNames);

    if (generatedSources.Count == 0)
    {
        Console.Error.WriteLine("No dialect sources were generated for the requested inputs and filters.");
        return 1;
    }

    if (parseResult.WriteToStdout)
    {
        for (var i = 0; i < generatedSources.Count; i++)
        {
            if (i > 0)
            {
                Console.WriteLine();
            }

            Console.WriteLine("// === " + generatedSources[i].HintName + " ===");
            Console.Write(generatedSources[i].SourceText);
            if (!generatedSources[i].SourceText.EndsWith("\n", StringComparison.Ordinal))
            {
                Console.WriteLine();
            }
        }

        return 0;
    }

    var outputDirectory = parseResult.OutputDirectory != null
        ? Path.GetFullPath(parseResult.OutputDirectory)
        : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(parseResult.InputPaths[0])) ?? Environment.CurrentDirectory, "Generated");
    Directory.CreateDirectory(outputDirectory);

    foreach (var generatedSource in generatedSources)
    {
        var outputPath = Path.Combine(outputDirectory, generatedSource.HintName);
        File.WriteAllText(outputPath, generatedSource.SourceText);
        Console.WriteLine("Wrote " + outputPath);
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/TdToCSharp/TdToCSharp.csproj -- <file.td> [more.td ...] [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -o, --output <dir>         Output directory for generated .g.cs files.");
    Console.WriteLine("      --stdout               Print generated sources to stdout instead of writing files.");
    Console.WriteLine("      --dialect <name>       Emit only the named dialect. Can be repeated.");
    Console.WriteLine("      --include-prelude      Also emit PreludeDialectRegistration.g.cs.");
    Console.WriteLine("  -h, --help                 Show this help text.");
    Console.WriteLine();
    Console.WriteLine("Behavior:");
    Console.WriteLine("  Includes are resolved from the local filesystem first, then from the embedded MLIR prelude.");
    Console.WriteLine("  Multiple input files are merged by dialect name using the same merge logic as the source generator.");
    Console.WriteLine("  When --output is omitted, files are written to a Generated/ directory next to the first input file.");
}

static ParseResult ParseArguments(IReadOnlyList<string> args)
{
    if (args.Count == 0)
    {
        return ParseResult.Error("At least one input .td file is required.");
    }

    var inputPaths = new List<string>();
    var dialectNames = new List<string>();
    string? outputDirectory = null;
    var includePrelude = false;
    var writeToStdout = false;

    for (var i = 0; i < args.Count; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "-h":
            case "--help":
                return ParseResult.Help();
            case "--stdout":
                writeToStdout = true;
                break;
            case "--include-prelude":
                includePrelude = true;
                break;
            case "-o":
            case "--output":
                if (i + 1 >= args.Count)
                {
                    return ParseResult.Error("Missing value for --output.");
                }

                outputDirectory = args[++i];
                break;
            case "--dialect":
                if (i + 1 >= args.Count)
                {
                    return ParseResult.Error("Missing value for --dialect.");
                }

                dialectNames.Add(args[++i]);
                break;
            default:
                if (arg.StartsWith("-", StringComparison.Ordinal))
                {
                    return ParseResult.Error("Unknown option: " + arg);
                }

                inputPaths.Add(arg);
                break;
        }
    }

    if (inputPaths.Count == 0)
    {
        return ParseResult.Error("At least one input .td file is required.");
    }

    if (writeToStdout && outputDirectory != null)
    {
        return ParseResult.Error("Use either --stdout or --output, not both.");
    }

    return new ParseResult(
        showHelp: false,
        helpExitCode: 0,
        errorMessage: null,
        inputPaths: inputPaths.ToArray(),
        outputDirectory: outputDirectory,
        includePrelude: includePrelude,
        writeToStdout: writeToStdout,
        dialectNames: dialectNames.ToArray());
}

internal sealed class ParseResult
{
    public ParseResult(
        bool showHelp,
        int helpExitCode,
        string? errorMessage,
        string[] inputPaths,
        string? outputDirectory,
        bool includePrelude,
        bool writeToStdout,
        string[] dialectNames)
    {
        ShowHelp = showHelp;
        HelpExitCode = helpExitCode;
        ErrorMessage = errorMessage;
        InputPaths = inputPaths;
        OutputDirectory = outputDirectory;
        IncludePrelude = includePrelude;
        WriteToStdout = writeToStdout;
        DialectNames = dialectNames;
    }

    public bool ShowHelp { get; }

    public int HelpExitCode { get; }

    public string? ErrorMessage { get; }

    public string[] InputPaths { get; }

    public string? OutputDirectory { get; }

    public bool IncludePrelude { get; }

    public bool WriteToStdout { get; }

    public string[] DialectNames { get; }

    public static ParseResult Help()
    {
        return new ParseResult(true, 0, null, Array.Empty<string>(), null, false, false, Array.Empty<string>());
    }

    public static ParseResult Error(string message)
    {
        return new ParseResult(false, 1, message, Array.Empty<string>(), null, false, false, Array.Empty<string>());
    }
}

internal sealed class FileSystemIncludeResolver : IncludeResolver
{
    public override bool TryResolveInclude(
        string includePath,
        SourceFile? includingFile,
        out ResolvedInclude resolvedInclude)
    {
        if (TryResolveExistingPath(includePath, out resolvedInclude))
        {
            return true;
        }

        if (includingFile != null)
        {
            var directory = Path.GetDirectoryName(includingFile.LogicalPath);
            if (!string.IsNullOrEmpty(directory))
            {
                var candidate = Path.Combine(directory, includePath);
                if (TryResolveExistingPath(candidate, out resolvedInclude))
                {
                    return true;
                }
            }
        }

        resolvedInclude = null!;
        return false;
    }

    private static bool TryResolveExistingPath(string path, out ResolvedInclude resolvedInclude)
    {
        if (!File.Exists(path))
        {
            resolvedInclude = null!;
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        resolvedInclude = new ResolvedInclude(fullPath, File.ReadAllText(fullPath));
        return true;
    }
}
