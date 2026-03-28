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
            "class Dialect {\n" +
            "  string name;\n" +
            "  string cppNamespace;\n" +
            "};\n" +
            "class Op<string dialectName, string mnemonic> {\n" +
            "  string dialectName = dialectName;\n" +
            "  string mnemonic = mnemonic;\n" +
            "  string cppClassName;\n" +
            "  list<string> operands;\n" +
            "  list<string> results;\n" +
            "  bit hasCustomAssemblyFormat = 0;\n" +
            "};\n" +
            "class AttrDef<string dialectName, string attrName> {\n" +
            "  string dialectName = dialectName;\n" +
            "  string attrName = attrName;\n" +
            "  string cppClassName;\n" +
            "};\n" +
            "class TypeDef<string dialectName, string typeName> {\n" +
            "  string dialectName = dialectName;\n" +
            "  string typeName = typeName;\n" +
            "  string cppClassName;\n" +
            "};\n" +
            "def Arith_Dialect : Dialect {\n" +
            "  let name = \"arith\";\n" +
            "  let cppNamespace = \"::mlir::arith\";\n" +
            "};\n" +
            "def AddIOp : Op<\"arith\", \"addi\"> {\n" +
            "  let cppClassName = \"AddIOperation\";\n" +
            "  let operands = [\"lhs\", \"rhs\"];\n" +
            "  let results = [\"result\"];\n" +
            "  let hasCustomAssemblyFormat = 1;\n" +
            "};\n" +
            "def FastMathAttr : AttrDef<\"arith\", \"fastmath\"> {\n" +
            "  let cppClassName = \"FastMathAttributeValue\";\n" +
            "};\n" +
            "def I32Type : TypeDef<\"arith\", \"i32\"> {\n" +
            "  let cppClassName = \"I32TypeReference\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new OdsDialectGenerator(),
            ("arith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "ArithDialectRegistration.g.cs")).SourceText.ToString();

        Assert.Contains("namespace MLIR.Arith;", registrationSource);
        Assert.Contains("public static class ArithDialectRegistration", registrationSource);
        Assert.Contains("public sealed class AddIOperation : Operation", registrationSource);
        Assert.Contains("public sealed class FastMathAttributeValue : AttributeValue", registrationSource);
        Assert.Contains("public sealed class I32TypeReference : TypeReference", registrationSource);
        Assert.Contains("public sealed class AddIOperationAssemblyFormat : IOperationAssemblyFormat", registrationSource);
        Assert.Contains("dialect.AddOperation(\"arith.addi\"", registrationSource);
        Assert.Contains(".WithFactory(static context => new AddIOperation(context))", registrationSource);
        Assert.Contains(".WithAssemblyFormat(new AddIOperationAssemblyFormat())", registrationSource);
    }
}
