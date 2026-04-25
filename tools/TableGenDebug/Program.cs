using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MLIR.Generators;
using MLIR.Text;
using Pixie;
using Pixie.Markup;
using Pixie.Options;
using Pixie.Terminal;
using TableGen;
using TableGen.Evaluation;
using TableGenDebug;

var stdoutLog = TerminalLog.AcquireStandardOutput();
var stderrLog = TerminalLog.AcquireStandardError();

var helpFlag = FlagOption.CreateFlagOption(
        OptionForm.Short("h"),
        OptionForm.Long("help"))
    .WithCategory("General")
    .WithDescription(new Text("Show this help text."));
var positionalOption = SequenceOption.CreateStringOption(OptionForm.Long("argument"))
    .WithCategory("Input")
    .WithDescription(new Text("The input TableGen file, followed by an optional record-name glob pattern."))
    .WithParameters(
        new SymbolicOptionParameter("file"),
        new SymbolicOptionParameter("record-pattern"));
var options = new Option[] { helpFlag };
var parser = new GnuOptionSetParser(options, positionalOption);

var parsedOptions = parser.Parse(args, stderrLog);
if (parsedOptions.GetValue<bool>(helpFlag))
{
    stdoutLog.Log(CreateHelpMessage(options));
    return 0;
}

var positionalArguments = parsedOptions.GetValue<string[]>(positionalOption);
if (positionalArguments.Length == 0 || positionalArguments.Length > 2)
{
    LogError(stderrLog, positionalArguments.Length == 0
        ? "An input .td file is required."
        : "Expected at most two positional arguments: <file.td> [record-pattern].");
    stderrLog.Log(CreateHelpMessage(options));
    return 1;
}

var inputPath = positionalArguments[0];
if (!File.Exists(inputPath))
{
    LogError(stderrLog, "Input file not found: " + inputPath);
    return 1;
}

var recordPattern = positionalArguments.Length >= 2 ? positionalArguments[1] : "*";
try
{
    var sourceText = File.ReadAllText(inputPath);
    var sourceDocument = new OriginalSourceDocument(sourceText, Path.GetFullPath(inputPath));
    var resolver = new CompositeIncludeResolver(
        new FileSystemIncludeResolver(),
        PreludeIncludeResolvers.CreateEmbeddedPreludeResolver());

    var documentResult = Document.Load(sourceDocument, resolver);
    if (!documentResult.IsSuccess)
    {
        LogError(stderrLog, documentResult.Diagnostic!.ToString());
        return 1;
    }

    var evaluated = documentResult.Value.Evaluate();
    if (!evaluated.IsSuccess)
    {
        LogError(stderrLog, evaluated.Diagnostic!.ToString());
        return 1;
    }

    var records = evaluated.Value.Records
        .Where(record => MatchesPattern(record.Name, recordPattern))
        .ToArray();

    if (records.Length == 0)
    {
        LogInfo(stdoutLog, "No records matched pattern '" + recordPattern + "'.");
        return 0;
    }

    for (var i = 0; i < records.Length; i++)
    {
        if (i > 0)
        {
            Console.WriteLine();
        }

        PrintRecord(records[i]);
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
        new Text("Load a TableGen file, evaluate it with the embedded MLIR prelude, and print matching records."),
        new Text("tablegendebug <file.td> [record-pattern]"),
        options);
}

static void LogError(ILog log, string message)
{
    log.Log(new LogEntry(Severity.Error, new Text("error: " + message)));
}

static void LogInfo(ILog log, string message)
{
    log.Log(new LogEntry(Severity.Message, new Text(message)));
}

static string FormatExceptionMessage(Exception exception)
{
    var parts = new System.Collections.Generic.List<string>();
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

static bool MatchesPattern(string value, string pattern)
{
    if (pattern == "*")
    {
        return true;
    }

    var regex = "^" + Regex.Escape(pattern)
        .Replace(@"\*", ".*")
        .Replace(@"\?", ".") + "$";
    return Regex.IsMatch(value, regex, RegexOptions.CultureInvariant);
}

static void PrintRecord(Record record)
{
    var builder = new StringBuilder();
    builder.Append("def ").Append(record.Name);
    if (record.BaseClasses.Count > 0)
    {
        builder.Append(" : ").Append(string.Join(", ", record.BaseClassNames));
    }

    builder.AppendLine(" {");
    foreach (var field in record.Fields.OrderBy(static field => field.Key, StringComparer.Ordinal))
    {
        builder.Append("  ").Append(field.Key).Append(" = ").Append(FormatValue(field.Value)).AppendLine(";");
    }

    builder.AppendLine("}");
    Console.Write(builder.ToString());
}

static string FormatValue(Value value)
{
    return value switch
    {
        IntegerValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
        StringValue str => $"\"{EscapeString(str.Value)}\"",
        BitValue bit => bit.Value ? "1" : "0",
        ListValue list => $"[{string.Join(", ", list.Items.Select(FormatValue))}]",
        RecordReferenceValue record => $"@{record.RecordName}",
        SymbolReferenceValue symbol => $"${symbol.SymbolName}",
        UnsetValue => "?",
        DagValue dag => $"({dag.OperatorName}{FormatDagArguments(dag)})",
        AnonymousRecordValue anonymous => FormatAnonymousRecordValue(anonymous),
        _ => value.GetType().Name,
    };
}

static string FormatDagArguments(DagValue dag)
{
    if (dag.Arguments.Count == 0)
    {
        return string.Empty;
    }

    return " " + string.Join(", ", dag.Arguments.Select(argument =>
        argument.Name == null
            ? FormatValue(argument.Value)
            : $"{FormatValue(argument.Value)}:${argument.Name}"));
}

static string FormatAnonymousRecordValue(AnonymousRecordValue anonymous)
{
    var fields = anonymous.Fields
        .OrderBy(static field => field.Key, StringComparer.Ordinal)
        .Select(field => $"{field.Key} = {FormatValue(field.Value)}");
    return $"anon {anonymous.ClassName} {{ {string.Join(", ", fields)} }}";
}

static string EscapeString(string value)
{
    return value
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r", "\\r")
        .Replace("\n", "\\n")
        .Replace("\t", "\\t");
}
