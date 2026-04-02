namespace MLIR.Generators.Tests;

using System.Collections.Generic;
using System.Linq;
using MLIR.Generators;
using TableGen;
using Xunit;

/// <summary>
/// Tests for include resolution in the TableGen generator pipeline.
/// </summary>
public sealed class IncludeResolutionTests
{
    // -----------------------------------------------------------------------
    // Embedded prelude tests (via the generator pipeline)
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorResolvesEmbeddedPreludeIrOpBaseTd()
    {
        // A .td file that explicitly includes mlir/IR/OpBase.td from the embedded prelude.
        const string source =
            "include \"mlir/IR/OpBase.td\"\n" +
            "\n" +
            "class TestPrelude_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<TestPrelude_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def TestPrelude_Dialect : Dialect {\n" +
            "  let name = \"testprelude\";\n" +
            "  let cppNamespace = \"::mlir::testprelude\";\n" +
            "};\n" +
            "\n" +
            "def TestPrelude_AddIOp : TestPrelude_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("testprelude.td", source));

        var registration = Assert.Single(
            generatedSources.Where(static r => r.HintName == "TestpreludeDialectRegistration.g.cs"));
        var text = registration.SourceText.ToString();

        Assert.Contains("namespace MLIR.Testprelude;", text);
        Assert.Contains("public sealed class TestPrelude_AddIOp : Operation", text);
        Assert.Contains("dialect.AddOperation(TestPrelude_AddIOp.OperationDefinition);", text);
    }

    [Fact]
    public void GeneratorResolvesEmbeddedPreludeCommonTypeConstraintsTd()
    {
        const string source =
            "include \"mlir/IR/OpBase.td\"\n" +
            "\n" +
            "class TestTypes_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<TestTypes_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def TestTypes_Dialect : Dialect {\n" +
            "  let name = \"testtypes\";\n" +
            "  let cppNamespace = \"::mlir::testtypes\";\n" +
            "};\n" +
            "\n" +
            "def TestTypes_CastOp : TestTypes_Op<\"cast\", []> {\n" +
            "  let arguments = (ins I32:$input);\n" +
            "  let results = (outs I64:$result);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("testtypes.td", source));

        var registration = Assert.Single(
            generatedSources.Where(static r => r.HintName == "TesttypesDialectRegistration.g.cs"));
        var text = registration.SourceText.ToString();

        Assert.Contains("namespace MLIR.Testtypes;", text);
        Assert.Contains("public sealed class TestTypes_CastOp : Operation", text);
    }

    [Fact]
    public void GeneratorResolvesEmbeddedPreludeCommonAttrConstraintsTd()
    {
        const string source =
            "include \"mlir/IR/OpBase.td\"\n" +
            "\n" +
            "class TestAttrs_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<TestAttrs_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def TestAttrs_Dialect : Dialect {\n" +
            "  let name = \"testattrs\";\n" +
            "  let cppNamespace = \"::mlir::testattrs\";\n" +
            "};\n" +
            "\n" +
            "def TestAttrs_ConstantOp : TestAttrs_Op<\"constant\", [Pure]> {\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$value attr-dict\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("testattrs.td", source));

        var registration = Assert.Single(
            generatedSources.Where(static r => r.HintName == "TestattrsDialectRegistration.g.cs"));
        var text = registration.SourceText.ToString();

        Assert.Contains("namespace MLIR.Testattrs;", text);
        Assert.Contains("public sealed class TestAttrs_ConstantOp : Operation", text);
    }

    [Fact]
    public void GeneratorRequiresExplicitPreludeInclude()
    {
        const string withoutInclude =
            "class Arith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<Arith_Dialect, mnemonic, traits>;\n" +
            "def Arith_Dialect : Dialect {\n" +
            "  let name = \"arith\";\n" +
            "  let cppNamespace = \"::mlir::arith\";\n" +
            "};\n" +
            "def Arith_AddIOp : Arith_Op<\"addi\", [Pure]> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        const string withInclude =
            "include \"mlir/IR/OpBase.td\"\n" +
            withoutInclude;

        var withoutResult = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("arith_no_include.td", withoutInclude));

        var withoutPreludeResult = GeneratorTestHelpers.RunGeneratorRaw(
            new DialectGenerator(),
            ("arith_no_include.td", withoutInclude));

        var withResult = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("arith_with_include.td", withInclude));

        Assert.Single(withoutResult.Where(static r => r.HintName.Contains("Arith")));
        Assert.Empty(withoutPreludeResult.Where(static r => r.HintName.Contains("Arith")));
        Assert.Single(withResult.Where(static r => r.HintName.Contains("Arith")));
        var withText = withResult.First(static r => r.HintName.Contains("Arith")).SourceText.ToString();

        Assert.Contains("public sealed class Arith_AddIOp : Operation", withText);
    }

    // -----------------------------------------------------------------------
    // Consumer file resolver tests (via the generator pipeline)
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorResolvesConsumerFileIncludes()
    {
        // base.td defines a shared op class and dialect.
        const string baseTd =
            "class Shared_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<Shared_Dialect, mnemonic, traits>;\n" +
            "def Shared_Dialect : Dialect {\n" +
            "  let name = \"shared\";\n" +
            "  let cppNamespace = \"::mlir::shared\";\n" +
            "};";

        // ops.td includes base.td.
        const string opsTd =
            "include \"base.td\"\n" +
            "def Shared_MulIOp : Shared_Op<\"muli\", [Pure, Commutative]> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "};";

        // Both files are passed as additional texts.
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("base.td", baseTd),
            ("ops.td", opsTd));

        // The ops.td results should include the MulIOp.
        var allText = string.Join("\n", generatedSources.Select(static r => r.SourceText.ToString()));
        Assert.Contains("Shared_MulIOp", allText);
    }

    [Fact]
    public void GeneratorPreludeIncludeGuardPreventsDoubleInclusion()
    {
        // Two .td files both include mlir/IR/OpBase.td – the prelude should only be expanded once.
        const string fileA =
            "include \"mlir/IR/OpBase.td\"\n" +
            "def A_Dialect : Dialect { let name = \"aaa\"; let cppNamespace = \"::mlir::aaa\"; };";

        const string fileB =
            "include \"mlir/IR/OpBase.td\"\n" +
            "def B_Dialect : Dialect { let name = \"bbb\"; let cppNamespace = \"::mlir::bbb\"; };";

        // Each file is processed independently, so double inclusion within a single file is the concern.
        // This test verifies that processing doesn't throw a duplicate-key exception.
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("a.td", fileA),
            ("b.td", fileB));

        var names = generatedSources.Select(static r => r.HintName).ToArray();
        Assert.Contains("AaaDialectRegistration.g.cs", names);
        Assert.Contains("BbbDialectRegistration.g.cs", names);
    }

    [Fact]
    public void GeneratorReportsDiagnosticForUnresolvableInclude()
    {
        const string source = "include \"does_not_exist.td\"";

        // RunGenerator swallows the exception and emits it as a Roslyn diagnostic
        // (wrapped in ParsedDialectFile.ErrorMessage). No sources should be generated.
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("broken.td", source));

        Assert.Empty(generatedSources);
    }

    // -----------------------------------------------------------------------
    // Direct TableGen API tests for embedded resolver via composite
    // -----------------------------------------------------------------------

    [Fact]
    public void DictionaryResolverCanBeUsedAsTestResolver()
    {
        const string baseSource =
            "class Base<string tag> { string Tag = tag; };";
        const string mainSource =
            "include \"base.td\"\n" +
            "def Example : Base<\"hello\">;";

        var resolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string> { ["base.td"] = baseSource });

        var doc = Document.Load(mainSource, resolver);
        var record = doc.Evaluate().Records.Single(static r => r.Name == "Example");

        Assert.Equal("hello",
            Assert.IsType<TableGen.Evaluation.StringValue>(record.GetField("Tag")).Value);
    }

    [Fact]
    public void CompositeResolverUsesEmbeddedPreludeAsFallback()
    {
        // Consumer resolver is empty; prelude resolver is the fallback.
        var consumerResolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string>());
        var preludeResolver = new TableGenDictionaryIncludeResolver(
            new Dictionary<string, string>
            {
                ["mlir/IR/OpBase.td"] =
                    "class Trait;\n" +
                    "def Pure : Trait;\n",
            });

        var composite = new TableGenCompositeIncludeResolver(consumerResolver, preludeResolver);
        var doc = Document.Load("include \"mlir/IR/OpBase.td\"", composite);

        // Pure should be available as a record.
        Assert.NotNull(doc.Evaluate().Records.Single(static r => r.Name == "Pure"));
    }

    // -----------------------------------------------------------------------
    // New mlir/IR/ prelude files
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorResolvesEmbeddedDialectBaseTd()
    {
        const string source =
            "include \"mlir/IR/DialectBase.td\"\n" +
            "\n" +
            "class DB_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<DB_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def DB_Dialect : Dialect {\n" +
            "  let name = \"db\";\n" +
            "  let cppNamespace = \"::mlir::db\";\n" +
            "};\n" +
            "\n" +
            "def DB_NoopOp : DB_Op<\"noop\", [Pure]> {\n" +
            "  let arguments = (ins);\n" +
            "  let results  = (outs);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("db.td", source));

        var registration = Assert.Single(
            generatedSources.Where(static r => r.HintName == "DbDialectRegistration.g.cs"));
        Assert.Contains("namespace MLIR.Db;", registration.SourceText.ToString());
    }

    [Fact]
    public void GeneratorResolvesEmbeddedCommonTypeConstraintsTd()
    {
        const string source =
            "include \"mlir/IR/CommonTypeConstraints.td\"\n" +
            "\n" +
            "class TCTest_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<TCTest_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def TCTest_Dialect : Dialect {\n" +
            "  let name = \"tctest\";\n" +
            "  let cppNamespace = \"::mlir::tctest\";\n" +
            "};\n" +
            "\n" +
            "def TCTest_AddOp : TCTest_Op<\"add\", [Pure]> {\n" +
            "  let arguments = (ins I32:$lhs, I64:$rhs);\n" +
            "  let results  = (outs I64:$result);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("tctest.td", source));

        var registration = Assert.Single(
            generatedSources.Where(static r => r.HintName == "TctestDialectRegistration.g.cs"));
        Assert.Contains("namespace MLIR.Tctest;", registration.SourceText.ToString());
    }

    [Fact]
    public void GeneratorResolvesEmbeddedCommonAttrConstraintsTd()
    {
        const string source =
            "include \"mlir/IR/CommonAttrConstraints.td\"\n" +
            "\n" +
            "class ACTest_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<ACTest_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def ACTest_Dialect : Dialect {\n" +
            "  let name = \"actest\";\n" +
            "  let cppNamespace = \"::mlir::actest\";\n" +
            "};\n" +
            "\n" +
            "def ACTest_ConstOp : ACTest_Op<\"const\", [Pure]> {\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results  = (outs I32:$result);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("actest.td", source));

        var registration = Assert.Single(
            generatedSources.Where(static r => r.HintName == "ActestDialectRegistration.g.cs"));
        Assert.Contains("namespace MLIR.Actest;", registration.SourceText.ToString());
    }

    [Fact]
    public void GeneratorResolvesEmbeddedUtilsTd()
    {
        const string source =
            "include \"mlir/IR/Utils.td\"\n" +
            "\n" +
            "class Utils_Op<string mnemonic> :\n" +
            "    Op<Utils_Dialect, mnemonic, []>;\n" +
            "\n" +
            "def Utils_Dialect : Dialect {\n" +
            "  let name = \"utils\";\n" +
            "  let cppNamespace = \"::mlir::utils\";\n" +
            "};\n" +
            "\n" +
            "def Utils_NoopOp : Utils_Op<\"noop\"> {\n" +
            "  let arguments = (ins);\n" +
            "  let results  = (outs);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("utils.td", source));

        var registration = Assert.Single(
            generatedSources.Where(static r => r.HintName == "UtilsDialectRegistration.g.cs"));
        Assert.Contains("namespace MLIR.Utils;", registration.SourceText.ToString());
    }
}
