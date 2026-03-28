namespace MLIR.Tests;

using MLIR;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed class ParsingTests
{
    [Fact]
    public void ParsesAndPrintsSimpleGenericOperation()
    {
        const string source = "%0 = \"arith.addi\"(%lhs, %rhs) {fastmath = #arith.fastmath<none>} : (i32, i32) -> i32";

        var module = Parser.ParseModule(source);
        var text = Printer.Print(module);

        Assert.Equal(source, text);
    }

    [Fact]
    public void ParsesSuccessorsAndRegions()
    {
        const string source = "\"scf.if\"(%cond) {\n^bb1(%arg0: i32):\n  \"cf.br\"(%arg0) [^bb2] : (i32) -> ()\n^bb2:\n  \"func.return\"() : () -> ()\n}";

        var module = Parser.ParseModule(source);

        Assert.Single(module.Operations);
        Assert.Single(module.Operations[0].Regions);
        Assert.Equal("^bb2", module.Operations[0].Regions[0].Blocks[0].Operations[0].Successors[0]);
    }

    [Fact]
    public void PreservesStructuredTypeSignatureText()
    {
        const string source = "\"memref.cast\"(%arg0) : (memref<2x?xf32, #map>) -> memref<*xf32>";

        var module = Parser.ParseModule(source);

        Assert.Equal("(memref<2x?xf32, #map>) -> memref<*xf32>", module.Operations[0].TypeSignature!.Text);
    }

    [Fact]
    public void MlirDocumentParsesAndPrints()
    {
        const string source = "\"func.return\"() : () -> ()";

        var document = Document.Parse(source);

        Assert.Equal(source, document.ToText());
        Assert.Single(document.Module.Operations);
    }

    [Fact]
    public void RoundTripsLargerMultiOperationInput()
    {
        const string source =
            "%c0 = \"arith.constant\"() {value = 0 : index} : () -> index\n" +
            "%c1 = \"arith.constant\"() {value = 1 : index} : () -> index\n" +
            "%sum = \"arith.addi\"(%c0, %c1) : (index, index) -> index\n" +
            "\"scf.if\"(%sum) {\n" +
            "  %cast = \"memref.cast\"(%arg0) : (memref<4x?xf32>) -> memref<*xf32>\n" +
            "  \"func.return\"(%cast) : (memref<*xf32>) -> ()\n" +
            "} {predicate = #builtin.unit} : (index) -> ()";

        var module = Parser.ParseModule(source);
        var text = Printer.Print(module);

        Assert.Equal(4, module.Operations.Count);
        Assert.Equal(source, text);
    }

    [Fact]
    public void RoundTripsLargerInputWithMultipleBlocks()
    {
        const string source =
            "\"cf.cond_br\"(%cond) [^then, ^else] : (i1) -> ()\n" +
            "\"test.graph_region\"() {\n" +
            "  ^then(%arg0: i32):\n" +
            "    %0 = \"arith.addi\"(%arg0, %arg0) : (i32, i32) -> i32\n" +
            "    \"cf.br\"(%0) [^merge] : (i32) -> ()\n" +
            "  ^else(%arg1: i32):\n" +
            "    %1 = \"arith.subi\"(%arg1, %arg1) : (i32, i32) -> i32\n" +
            "    \"cf.br\"(%1) [^merge] : (i32) -> ()\n" +
            "  ^merge(%arg2: i32):\n" +
            "    \"func.return\"(%arg2) : (i32) -> ()\n" +
            "} : () -> ()";

        var module = Parser.ParseModule(source);
        var text = Printer.Print(module);

        Assert.Equal(2, module.Operations.Count);
        Assert.Equal(3, module.Operations[1].Regions[0].Blocks.Count);
        Assert.Equal(source, text);
    }

    [Fact]
    public void ParsesLargerInputWithCommentsAndBlankLines()
    {
        const string source =
            "// constants\n" +
            "%c0 = \"arith.constant\"() {value = 0 : i32} : () -> i32\n" +
            "\n" +
            "// computation\n" +
            "%c1 = \"arith.constant\"() {value = 1 : i32} : () -> i32\n" +
            "%sum = \"arith.addi\"(%c0, %c1) : (i32, i32) -> i32\n";

        var module = Parser.ParseModule(source);
        var text = Printer.Print(module);

        Assert.Equal(3, module.Operations.Count);
        Assert.Equal(source, text);
    }

    [Fact]
    public void PreservesCommentsAndSpacingInsideRegions()
    {
        const string source =
            "\"scf.if\"(%cond) {\n" +
            "  // then branch\n" +
            "  %0 = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32\n" +
            "\n" +
            "  // terminate\n" +
            "  \"func.return\"(%0) : (i32) -> ()\n" +
            "} : (i1) -> ()";

        var module = Parser.ParseModule(source);
        var text = Printer.Print(module);

        Assert.Equal(source, text);
    }

    [Fact]
    public void ParsesEmptyAttributeDictionary()
    {
        const string source = "\"test.empty_attr_dict\"() {} : () -> ()";

        var module = Parser.ParseModule(source);
        var operation = module.Operations[0];

        Assert.Empty(operation.Regions);
        Assert.Empty(operation.Attributes);
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void ParsesUnlabeledEntryBlockBeforeExplicitLabeledBlock()
    {
        const string source =
            "\"test.region\"() {\n" +
            "  \"cf.br\"() [^bb1] : () -> ()\n" +
            "^bb1:\n" +
            "  \"func.return\"() : () -> ()\n" +
            "} : () -> ()";

        var module = Parser.ParseModule(source);
        var blocks = module.Operations[0].Regions[0].Blocks;

        Assert.Equal(2, blocks.Count);
        Assert.Equal("^entry", blocks[0].Label);
        Assert.Equal("^bb1", blocks[1].Label);
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void ParsesIdentifierOperationNames()
    {
        const string source = "test.op(%arg0) : (i32) -> i32";

        var module = Parser.ParseModule(source);

        Assert.Equal("test.op", module.Operations[0].Name);
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void PreservesTrailingTriviaOnEndOfFileToken()
    {
        const string source =
            "\"func.return\"() : () -> ()\n" +
            "// trailing note";

        var module = Parser.ParseModule(source);

        Assert.Equal("\n// trailing note", module.EndOfFileToken.LeadingTrivia);
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void PreservesRawSyntaxWithNestedDelimiters()
    {
        const string source =
            "\"test.op\"(%arg0) {layout = dense<[[1, 2], [3, 4]]> : tensor<2x2xi32>} : (tensor<2x2xi32>) -> tensor<2x2xi32>";

        var module = Parser.ParseModule(source);
        var attribute = module.Operations[0].Attributes[0];

        Assert.Equal("dense<[[1, 2], [3, 4]]> : tensor<2x2xi32>", attribute.Value.Text);
        Assert.Equal(source, Printer.Print(module));
    }
}
