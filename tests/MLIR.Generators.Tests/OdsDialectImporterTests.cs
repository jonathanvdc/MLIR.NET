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
            "class Dialect {\n" +
            "  string name;\n" +
            "  string cppNamespace;\n" +
            "  string summary;\n" +
            "  string description;\n" +
            "  bit hasConstantMaterializer = 0;\n" +
            "};\n" +
            "class Op<string dialectName, string mnemonic> {\n" +
            "  string dialectName = dialectName;\n" +
            "  string mnemonic = mnemonic;\n" +
            "  string cppClassName;\n" +
            "  list<string> operands;\n" +
            "  list<string> results;\n" +
            "  list<string> attributes;\n" +
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
            "  let summary = \"Arithmetic dialect\";\n" +
            "  let description = [{This dialect defines basic integer and floating point arithmetic ops.}];\n" +
            "  let hasConstantMaterializer = 1;\n" +
            "};\n" +
            "def AddIOp : Op<\"arith\", \"addi\"> {\n" +
            "  let cppClassName = \"AddIOperation\";\n" +
            "  let operands = [\"lhs\", \"rhs\"];\n" +
            "  let results = [\"result\"];\n" +
            "  let attributes = [\"fastmath\"];\n" +
            "  let hasCustomAssemblyFormat = 1;\n" +
            "};\n" +
            "def FastMathAttr : AttrDef<\"arith\", \"fastmath\"> {\n" +
            "  let cppClassName = \"FastMathAttributeValue\";\n" +
            "};\n" +
            "def I32Type : TypeDef<\"builtin\", \"i32\"> {\n" +
            "  let cppClassName = \"I32TypeReference\";\n" +
            "};";

        var dialects = OdsDialectImporter.Import(TableGenDocument.Parse(source).Evaluate());

        var arith = Assert.Single(dialects, static dialect => dialect.Name == "arith");
        var op = Assert.Single(arith.Operations);
        var attribute = Assert.Single(arith.Attributes);

        Assert.Equal("::mlir::arith", arith.CppNamespace);
        Assert.Equal("Arithmetic dialect", arith.Summary);
        Assert.Contains("basic integer and floating point arithmetic ops", arith.Description);
        Assert.True(arith.HasConstantMaterializer);
        Assert.Equal("arith.addi", op.Name);
        Assert.Equal("AddIOperation", op.ClassName);
        Assert.Equal(["lhs", "rhs"], op.Operands);
        Assert.Equal(["result"], op.Results);
        Assert.Equal(["fastmath"], op.Attributes);
        Assert.True(op.HasCustomAssemblyFormat);
        Assert.Equal("fastmath", attribute.Name);
        Assert.Equal("FastMathAttributeValue", attribute.ClassName);

        var builtin = Assert.Single(dialects, static dialect => dialect.Name == "builtin");
        Assert.Null(builtin.CppNamespace);
        var type = Assert.Single(builtin.Types);
        Assert.Equal("i32", type.Name);
        Assert.Equal("I32TypeReference", type.ClassName);
    }
}
