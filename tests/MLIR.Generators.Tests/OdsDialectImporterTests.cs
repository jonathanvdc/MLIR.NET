namespace MLIR.Generators.Tests;

using System.Linq;
using MLIR.ODS;
using TableGen;
using Xunit;

public sealed class OdsDialectImporterTests
{
    [Fact]
    public void ImportsConventionBasedDialectOperationAttributeAndTypeRecords()
    {
        const string source =
            "def ArithDialect {\n" +
            "  string DialectName = \"arith\";\n" +
            "  string DialectClassName = \"ArithDialectRegistration\";\n" +
            "};\n" +
            "def ArithDialectOp {\n" +
            "  string DialectName = \"arith\";\n" +
            "  string OperationName = \"arith.addi\";\n" +
            "  string ClassName = \"AddIOperation\";\n" +
            "  list<string> Operands = [\"lhs\", \"rhs\"];\n" +
            "  list<string> Results = [\"result\"];\n" +
            "  list<string> Attributes = [\"fastmath\"];\n" +
            "  bit HasCustomAssemblyFormat = 1;\n" +
            "};\n" +
            "def ArithAttribute {\n" +
            "  string DialectName = \"arith\";\n" +
            "  string AttributeName = \"fastmath\";\n" +
            "  string ClassName = \"FastMathAttributeValue\";\n" +
            "};\n" +
            "def BuiltinType {\n" +
            "  string DialectName = \"builtin\";\n" +
            "  string TypeName = \"i32\";\n" +
            "  string ClassName = \"I32TypeReference\";\n" +
            "};";

        var dialects = OdsDialectImporter.Import(TableGenDocument.Parse(source).Evaluate());

        var arith = Assert.Single(dialects, static dialect => dialect.Name == "arith");
        var op = Assert.Single(arith.Operations);
        var attribute = Assert.Single(arith.Attributes);

        Assert.Equal("ArithDialectRegistration", arith.ClassName);
        Assert.Equal("arith.addi", op.Name);
        Assert.Equal("AddIOperation", op.ClassName);
        Assert.Equal(["lhs", "rhs"], op.Operands);
        Assert.Equal(["result"], op.Results);
        Assert.Equal(["fastmath"], op.Attributes);
        Assert.True(op.HasCustomAssemblyFormat);
        Assert.Equal("fastmath", attribute.Name);
        Assert.Equal("FastMathAttributeValue", attribute.ClassName);

        var builtin = Assert.Single(dialects, static dialect => dialect.Name == "builtin");
        Assert.Null(builtin.ClassName);
        var type = Assert.Single(builtin.Types);
        Assert.Equal("i32", type.Name);
        Assert.Equal("I32TypeReference", type.ClassName);
    }
}
