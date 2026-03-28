namespace MLIR.Generators.Tests;

using System.Linq;
using MLIR.Generators;
using Xunit;

public sealed class OdsDialectGeneratorTests
{
    [Fact]
    public void GeneratesDialectRegistrationTypedNodesAndCustomAssemblyStubs()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$value attr-dict\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let summary = \"integer addition\";\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new OdsDialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        Assert.Contains("namespace MLIR.Miniarith;", registrationSource);
        Assert.Contains("public static class MiniarithDialectRegistration", registrationSource);
        Assert.Contains("public sealed class MiniArith_ConstantOp : Operation", registrationSource);
        Assert.Contains("public sealed class MiniArith_AddIOp : Operation", registrationSource);
        Assert.Contains("public sealed class MiniArith_ConstantOpAssemblyFormat : IOperationAssemblyFormat", registrationSource);
        Assert.Contains("public sealed class MiniArith_AddIOpAssemblyFormat : IOperationAssemblyFormat", registrationSource);
        Assert.Contains("dialect.AddOperation(\"miniarith.constant\"", registrationSource);
        Assert.Contains("dialect.AddOperation(\"miniarith.addi\"", registrationSource);
        Assert.Contains(".WithFactory(static context => new MiniArith_AddIOp(context))", registrationSource);
        Assert.Contains(".WithAssemblyFormat(new MiniArith_AddIOpAssemblyFormat())", registrationSource);
    }
}
