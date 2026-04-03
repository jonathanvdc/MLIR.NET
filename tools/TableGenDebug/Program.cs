using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MLIR.Generators;
using TableGen;
using TableGen.Evaluation;
using TableGenDebug;

if (args.Length is 0 or > 2 || args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
{
    PrintUsage();
    return args.Length == 0 ? 1 : 0;
}

var inputPath = args[0];
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 1;
}

var recordPattern = args.Length >= 2 ? args[1] : "*";
try
{
    var sourceText = File.ReadAllText(inputPath);
    var sourceFile = new SourceFile(Path.GetFullPath(inputPath));
    var resolver = new CompositeIncludeResolver(
        new FileSystemIncludeResolver(),
        PreludeIncludeResolvers.CreateEmbeddedPreludeResolver());

    var document = Document.Load(sourceText, resolver, sourceFile);
    var records = document.Evaluate().Records
        .Where(record => MatchesPattern(record.Name, recordPattern))
        .ToArray();

    if (records.Length == 0)
    {
        Console.WriteLine($"No records matched pattern '{recordPattern}'.");
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
    Console.Error.WriteLine(exception);
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/TableGenDebug/TableGenDebug.csproj -- <file.td> [record-pattern]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <file.td>         TableGen file to load and evaluate.");
    Console.WriteLine("  [record-pattern]   Optional glob-style filter for record names.");
    Console.WriteLine("                    Use * to match any sequence of characters and ? for one character.");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run --project tools/TableGenDebug/TableGenDebug.csproj -- samples/GeneratedDialectConsumer/Dialects/arith.td");
    Console.WriteLine("  dotnet run --project tools/TableGenDebug/TableGenDebug.csproj -- samples/GeneratedDialectConsumer/Dialects/arith.td 'Arith_*'");
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
        builder.Append(" : ").Append(string.Join(", ", record.BaseClasses));
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
