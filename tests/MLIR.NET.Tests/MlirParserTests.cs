namespace MLIR.Tests;

using MLIR.Text;
using Xunit;

public sealed class MlirParserTests
{
    [Fact]
    public void ParsesAndPrintsSimpleGenericOperation()
    {
        const string source = "%0 = \"arith.addi\"(%lhs, %rhs) {fastmath = #arith.fastmath<none>} : (i32, i32) -> i32";

        var module = MlirParser.ParseModule(source);
        var text = MlirPrinter.Print(module);

        Assert.Equal(source, text);
    }

    [Fact]
    public void ParsesSuccessorsAndRegions()
    {
        const string source = "\"scf.if\"(%cond) {\n^bb1(%arg0: i32):\n  \"cf.br\"(%arg0) [^bb2] : (i32) -> ()\n^bb2:\n  \"func.return\"() : () -> ()\n}";

        var module = MlirParser.ParseModule(source);

        Assert.Single(module.Operations);
        Assert.Single(module.Operations[0].Regions);
        Assert.Equal("^bb2", module.Operations[0].Regions[0].Blocks[0].Operations[0].Successors[0]);
    }

    [Fact]
    public void PreservesStructuredTypeSignatureText()
    {
        const string source = "\"memref.cast\"(%arg0) : (memref<2x?xf32, #map>) -> memref<*xf32>";

        var module = MlirParser.ParseModule(source);

        Assert.Equal("(memref<2x?xf32, #map>) -> memref<*xf32>", module.Operations[0].TypeSignature!.Text);
    }

    [Fact]
    public void ThrowsHelpfulExceptionForInvalidInput()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"arith.addi\"(%lhs, %rhs"));

        Assert.Contains("operand list", exception.Message);
    }
}
