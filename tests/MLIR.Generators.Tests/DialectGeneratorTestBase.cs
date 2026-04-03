namespace MLIR.Generators.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Generators;
using Xunit;

public abstract class DialectGeneratorTestBase
{
    private static readonly string[] MiniArithPreamble =
    [
        "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :",
        "    Op<MiniArith_Dialect, mnemonic, traits>;",
        string.Empty,
        "def MiniArith_Dialect : Dialect {",
        "  let name = \"miniarith\";",
        "  let cppNamespace = \"::mlir::miniarith\";",
        "};",
    ];

    private static readonly string[] MyDialectPreamble =
    [
        "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :",
        "    Op<MyDialect_Dialect, mnemonic, traits>;",
        string.Empty,
        "def MyDialect_Dialect : Dialect {",
        "  let name = \"mydialect\";",
        "  let cppNamespace = \"::mlir::mydialect\";",
        "};",
    ];

    protected static string GenerateMiniArithRegistrationSource(IEnumerable<string> lines)
    {
        return GenerateRegistrationSource(
            "miniarith.td",
            "MiniarithDialectRegistration.g.cs",
            ComposeMiniArithSource(lines));
    }

    protected static string GenerateMyDialectRegistrationSource(IEnumerable<string> lines)
    {
        return GenerateRegistrationSource(
            "mydialect.td",
            "MydialectDialectRegistration.g.cs",
            ComposeMyDialectSource(lines));
    }

    protected static string GenerateRegistrationSource(string path, string hintName, string source)
    {
        var generatedSources = GeneratorTestHelpers.RunGenerator(new DialectGenerator(), (path, source));
        return Assert.Single(generatedSources.Where(result => result.HintName == hintName)).SourceText.ToString();
    }

    protected static string ComposeMiniArithSource(IEnumerable<string> lines)
    {
        return ComposeSource(MiniArithPreamble.Concat(new[] { string.Empty }).Concat(lines));
    }

    protected static string ComposeMyDialectSource(IEnumerable<string> lines)
    {
        return ComposeSource(MyDialectPreamble.Concat(new[] { string.Empty }).Concat(lines));
    }

    protected static string ComposeSource(IEnumerable<string> lines)
    {
        return string.Join("\n", lines);
    }

    protected static void AssertContainsAll(string text, params string[] snippets)
    {
        foreach (var snippet in snippets)
        {
            Assert.Contains(snippet, text);
        }
    }

    protected static void AssertDoesNotContainAny(string text, params string[] snippets)
    {
        foreach (var snippet in snippets)
        {
            Assert.DoesNotContain(snippet, text);
        }
    }

    protected static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
