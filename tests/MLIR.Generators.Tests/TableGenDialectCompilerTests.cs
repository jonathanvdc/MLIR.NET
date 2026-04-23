namespace MLIR.Generators.Tests;

using System;
using System.IO;
using System.Linq;
using MLIR.Generators;
using TableGen;
using Xunit;

public sealed class TableGenDialectCompilerTests
{
    [Fact]
    public void CompileFilesGeneratesDialectSourceWithoutPreludeByDefault()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var inputPath = Path.Combine(tempDirectory, "mydialect.td");
            File.WriteAllText(inputPath, string.Join("\n", new[]
            {
                "def MyDialect_Dialect : Dialect {",
                "  let name = \"mydialect\";",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "include \"mlir/IR/Interfaces.td\"",
                "include \"mlir/IR/AttrTypeBase.td\"",
                string.Empty,
                "def MyDialect_MarkerIface : TypeInterface<\"MyMarkerIface\"> {",
                "  let cppNamespace = \"::mlir::mydialect\";",
                "};",
                string.Empty,
                "class MyDialect_Type<string name, list<Trait> traits = []> : TypeDef<MyDialect_Dialect, name, traits> {",
                "  let typeName = \"myp.\" # name;",
                "};",
                string.Empty,
                "def MyDialect_FooType : MyDialect_Type<\"foo\", [MyDialect_MarkerIface]>;",
            }));

            var generatedSources = TableGenDialectCompiler.CompileSources(
                [new TableGenInput(inputPath, File.ReadAllText(inputPath))],
                PreludeIncludeResolvers.CreateEmbeddedPreludeResolver());

            var dialectSource = Assert.Single(generatedSources);
            Assert.Equal("mydialect", dialectSource.DialectName);
            Assert.Equal("MydialectDialectRegistration.g.cs", dialectSource.HintName);
            Assert.False(dialectSource.IsPrelude);
            Assert.Contains("public partial interface IMyMarkerIface", dialectSource.SourceText);
            Assert.Contains("public sealed partial class fooType : TypeReference, MLIR.Dialects.Mydialect.IMyMarkerIface", dialectSource.SourceText);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CompileFilesCanIncludePreludeAndFilterByDialectName()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var inputPath = Path.Combine(tempDirectory, "builtin.td");
            File.WriteAllText(inputPath, "include \"mlir/IR/BuiltinTypes.td\"\n");

            var generatedSources = TableGenDialectCompiler.CompileSources(
                [new TableGenInput(inputPath, File.ReadAllText(inputPath))],
                PreludeIncludeResolvers.CreateEmbeddedPreludeResolver(),
                includePrelude: true,
                dialectNames: ["builtin"]);

            var dialectSource = Assert.Single(generatedSources);
            Assert.Equal("builtin", dialectSource.DialectName);
            Assert.DoesNotContain(generatedSources, static source => source.IsPrelude);
            Assert.Contains("BuiltinDialectRegistration", dialectSource.SourceText);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mlir-net-td2cs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
