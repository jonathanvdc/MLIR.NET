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
            "def ArithDialect {\n" +
            "  string DialectName = \"arith\";\n" +
            "  string DialectClassName = \"ArithDialectRegistration\";\n" +
            "};\n" +
            "def AddI {\n" +
            "  string DialectName = \"arith\";\n" +
            "  string OperationName = \"arith.addi\";\n" +
            "  string ClassName = \"AddIOperation\";\n" +
            "  list<string> Operands = [\"lhs\", \"rhs\"];\n" +
            "  list<string> Results = [\"result\"];\n" +
            "  bit HasCustomAssemblyFormat = 1;\n" +
            "};\n" +
            "def FastMathAttr {\n" +
            "  string DialectName = \"arith\";\n" +
            "  string AttributeName = \"fastmath\";\n" +
            "  string ClassName = \"FastMathAttributeValue\";\n" +
            "};\n" +
            "def I32Type {\n" +
            "  string DialectName = \"arith\";\n" +
            "  string TypeName = \"i32\";\n" +
            "  string ClassName = \"I32TypeReference\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new OdsDialectGenerator(),
            ("arith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "ArithDialectRegistration.g.cs")).SourceText.ToString();

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
