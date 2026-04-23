using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using MLIR.Generators;
using Pixie;
using Pixie.Markup;
using Pixie.Options;
using Pixie.Terminal;
using TableGen;
using RoslynDiagnostic = Microsoft.CodeAnalysis.Diagnostic;

var stdoutLog = TerminalLog.AcquireStandardOutput();
var stderrLog = TerminalLog.AcquireStandardError();

var helpFlag = FlagOption.CreateFlagOption(
        OptionForm.Short("h"),
        OptionForm.Long("help"))
    .WithCategory("General")
    .WithDescription(new Text("Show this help text."));
var stdoutFlag = FlagOption.CreateFlagOption(OptionForm.Long("stdout"))
    .WithCategory("Output")
    .WithDescription(new Text("Print generated sources to stdout instead of writing files."));
var includePreludeFlag = FlagOption.CreateFlagOption(OptionForm.Long("include-prelude"))
    .WithCategory("Output")
    .WithDescription(new Text("Also emit PreludeDialectRegistration.g.cs."));
var outputOption = ValueOption.CreateStringOption(
        new[] { OptionForm.Short("o"), OptionForm.Long("output") },
        string.Empty)
    .WithCategory("Output")
    .WithDescription(new Text("Output directory for generated .g.cs files."));
var dialectOption = SequenceOption.CreateStringOption(OptionForm.Long("dialect"))
    .WithCategory("Filtering")
    .WithDescription(new Text("Emit only the named dialect. Can be repeated."))
    .WithParameters(new SymbolicOptionParameter("name"));
var inputOption = SequenceOption.CreateStringOption(OptionForm.Long("input"))
    .WithCategory("Input")
    .WithDescription(new Text("One or more TableGen input files to compile."))
    .WithParameters(new SymbolicOptionParameter("file", true));
var options = new Option[] { helpFlag, stdoutFlag, includePreludeFlag, outputOption, dialectOption };
var parser = new GnuOptionSetParser(options, inputOption);

var parsedOptions = parser.Parse(args, stderrLog);
if (parsedOptions.GetValue<bool>(helpFlag))
{
    stdoutLog.Log(CreateHelpMessage(options));
    return 0;
}

var inputPaths = parsedOptions.GetValue<string[]>(inputOption);
if (inputPaths.Length == 0)
{
    LogError(stderrLog, "At least one input .td file is required.");
    stderrLog.Log(CreateHelpMessage(options));
    return 1;
}

var writeToStdout = parsedOptions.GetValue<bool>(stdoutFlag);
var includePrelude = parsedOptions.GetValue<bool>(includePreludeFlag);
var outputDirectory = parsedOptions.GetValue<string>(outputOption);
var dialectNames = parsedOptions.GetValue<string[]>(dialectOption);

if (writeToStdout && !string.IsNullOrWhiteSpace(outputDirectory))
{
    LogError(stderrLog, "Use either --stdout or --output, not both.");
    return 1;
}

try
{
    var compilationResult = TableGenDialectCompiler.CompileSourcesDetailed(
        inputPaths
            .Select(path => Path.GetFullPath(path))
            .Select(path => new TableGenInput(path, File.ReadAllText(path))),
        new CompositeIncludeResolver(
            new FileSystemIncludeResolver(),
            PreludeIncludeResolvers.CreateEmbeddedPreludeResolver()),
        includePrelude: includePrelude,
        dialectNames: dialectNames);
    var generatedSources = compilationResult.GeneratedSources;

    foreach (var diagnostic in compilationResult.Diagnostics)
    {
        LogDiagnostic(stderrLog, diagnostic);
    }

    if (compilationResult.Diagnostics.Count > 0)
    {
        return 1;
    }

    if (generatedSources.Count == 0)
    {
        LogError(stderrLog, "No dialect sources were generated for the requested inputs and filters.");
        return 1;
    }

    if (writeToStdout)
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

    var resolvedOutputDirectory = !string.IsNullOrWhiteSpace(outputDirectory)
        ? Path.GetFullPath(outputDirectory)
        : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(inputPaths[0])) ?? Environment.CurrentDirectory, "Generated");
    Directory.CreateDirectory(resolvedOutputDirectory);

    foreach (var generatedSource in generatedSources)
    {
        var outputPath = Path.Combine(resolvedOutputDirectory, generatedSource.HintName);
        File.WriteAllText(outputPath, generatedSource.SourceText);
        LogInfo(stdoutLog, "Wrote " + outputPath);
    }

    return 0;
}
catch (Exception exception)
{
    LogError(stderrLog, FormatExceptionMessage(exception));
    return 1;
}

static HelpMessage CreateHelpMessage(IReadOnlyList<Option> options)
{
    return new HelpMessage(
        new Text("Compile one or more TableGen dialect inputs into generated C# sources."),
        new Text("tdtocsharp <file.td> [more.td ...] [options]"),
        options);
}

static void LogDiagnostic(ILog log, RoslynDiagnostic diagnostic)
{
    log.Log(
        new LogEntry(
            diagnostic.Severity switch
            {
                DiagnosticSeverity.Hidden => Severity.Message,
                DiagnosticSeverity.Info => Severity.Info,
                DiagnosticSeverity.Warning => Severity.Warning,
                DiagnosticSeverity.Error => Severity.Error,
                _ => Severity.Error,
            },
            new Text(FormatDiagnostic(diagnostic))));
}

static void LogError(ILog log, string message)
{
    log.Log(new LogEntry(Severity.Error, new Text("error: " + message)));
}

static void LogInfo(ILog log, string message)
{
    log.Log(new LogEntry(Severity.Message, new Text(message)));
}

static string FormatDiagnostic(RoslynDiagnostic diagnostic)
{
    var locationPrefix = diagnostic.Location == Location.None || !diagnostic.Location.IsInSource
        ? string.Empty
        : diagnostic.Location.ToString() + ": ";
    return locationPrefix
        + diagnostic.Severity.ToString().ToLowerInvariant()
        + " "
        + diagnostic.Id
        + ": "
        + diagnostic.GetMessage();
}

static string FormatExceptionMessage(Exception exception)
{
    var parts = new List<string>();
    for (var current = exception; current != null; current = current.InnerException)
    {
        if (!string.IsNullOrWhiteSpace(current.Message))
        {
            parts.Add(current.Message);
        }
    }

    return parts.Count == 0
        ? exception.GetType().FullName ?? exception.GetType().Name
        : string.Join(" --> ", parts);
}
