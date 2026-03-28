namespace MLIR.Generators.Tests;

using System.Linq;
using MLIR.ODS;
using TableGen;
using Xunit;

public sealed class OdsDialectImporterTests
{
    [Fact]
    public void ImportsActualOdsStyleDialectAndOperationRecords()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "  let summary = \"Arithmetic dialect\";\n" +
            "  let description = [{This dialect defines basic integer and floating point arithmetic ops.}];\n" +
            "  let hasConstantMaterializer = 1;\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let description = [{Produces a constant integer value.}];\n" +
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

        var dialects = OdsDialectImporter.Import(TableGenDocument.Parse(source).Evaluate());

        var dialect = Assert.Single(dialects);
        var constantOp = Assert.Single(dialect.Operations, static op => op.Name == "miniarith.constant");
        var addiOp = Assert.Single(dialect.Operations, static op => op.Name == "miniarith.addi");

        Assert.Equal("miniarith", dialect.Name);
        Assert.Equal("::mlir::miniarith", dialect.CppNamespace);
        Assert.Equal("Arithmetic dialect", dialect.Summary);
        Assert.Contains("basic integer and floating point arithmetic ops", dialect.Description);
        Assert.True(dialect.HasConstantMaterializer);

        Assert.Equal("MiniArith_ConstantOp", constantOp.ClassName);
        Assert.Equal(["result"], constantOp.Results);
        Assert.Equal(["value"], constantOp.Attributes);
        Assert.Empty(constantOp.Operands);
        Assert.Equal(["Pure"], constantOp.Traits);
        Assert.Equal("$value attr-dict", constantOp.AssemblyFormat);
        Assert.True(constantOp.HasCustomAssemblyFormat);
        Assert.Equal("integer constant", constantOp.Summary);
        Assert.Contains("Produces a constant integer value", constantOp.Description);

        Assert.Equal("MiniArith_AddIOp", addiOp.ClassName);
        Assert.Equal(["lhs", "rhs"], addiOp.Operands);
        Assert.Equal(["result"], addiOp.Results);
        Assert.Empty(addiOp.Attributes);
        Assert.Equal(["Pure", "Commutative"], addiOp.Traits);
        Assert.Equal("$lhs `,` $rhs attr-dict `:` type($result)", addiOp.AssemblyFormat);
        Assert.True(addiOp.HasCustomAssemblyFormat);
    }
}
