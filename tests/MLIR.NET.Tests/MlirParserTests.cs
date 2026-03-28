namespace MLIR.Tests;

using System.Collections.Generic;
using MLIR.Syntax;
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

        var module = MlirParser.ParseModule(source);
        var text = MlirPrinter.Print(module);

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

        var module = MlirParser.ParseModule(source);
        var text = MlirPrinter.Print(module);

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

        var module = MlirParser.ParseModule(source);
        var text = MlirPrinter.Print(module);

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

        var module = MlirParser.ParseModule(source);
        var text = MlirPrinter.Print(module);

        Assert.Equal(source, text);
    }

    [Fact]
    public void ExposesPreservedTokenTriviaInTheCst()
    {
        const string source =
            "// leading comment\n" +
            "%0,  %1 = \"test.op\"(%lhs,  %rhs) [ ^bb1 ] : (i32, i32) -> i32";

        var module = MlirParser.ParseModule(source);
        var operation = module.Operations[0];

        Assert.Equal("// leading comment\n", operation.ResultTokens[0].LeadingTrivia);
        Assert.Equal(",", operation.ResultCommaTokens[0].Text);
        Assert.Equal("  ", operation.ResultTokens[1].LeadingTrivia);
        Assert.Equal(" ", operation.SuccessorList.OpenToken!.LeadingTrivia);
        Assert.Equal(" ", operation.SuccessorList[0].LeadingTrivia);
        Assert.Equal(" ", operation.SuccessorList.CloseToken!.LeadingTrivia);
    }

    [Fact]
    public void DelimitedListsEnumerateItemsByDefault()
    {
        var block = new BlockSyntax(
            "^bb0",
            new List<BlockArgumentSyntax>
            {
                new("%arg0", new RawSyntaxText("i32")),
                new("%arg1", new RawSyntaxText("i64")),
            },
            new List<OperationSyntax>());

        var names = new List<string>();
        foreach (var argument in block.Arguments)
        {
            names.Add(argument.Name);
        }

        Assert.Equal(new[] { "%arg0", "%arg1" }, names);
        Assert.Equal("(", block.Arguments.OpenToken!.Text);
        Assert.Equal(")", block.Arguments.CloseToken!.Text);
        Assert.Single(block.Arguments.SeparatorTokens);
    }

    [Fact]
    public void PreservesTrailingTriviaOnEndOfFileToken()
    {
        const string source =
            "\"func.return\"() : () -> ()\n" +
            "// trailing note";

        var module = MlirParser.ParseModule(source);

        Assert.Equal("\n// trailing note", module.EndOfFileToken.LeadingTrivia);
        Assert.Equal(source, MlirPrinter.Print(module));
    }

    [Fact]
    public void FormatsProgrammaticallyGeneratedModuleWhenTriviaIsAbsent()
    {
        var module = new MlirModuleSyntax(
            new List<OperationSyntax>
            {
                new(
                    new List<string> { "%sum" },
                    "\"arith.addi\"",
                    new List<string> { "%lhs", "%rhs" },
                    new List<string>(),
                    new List<RegionSyntax>(),
                    new List<NamedAttributeSyntax>(),
                    new RawSyntaxText("(i32, i32) -> i32")),
                new(
                    new List<string>(),
                    "\"scf.if\"",
                    new List<string> { "%cond" },
                    new List<string>(),
                    new List<RegionSyntax>
                    {
                        new(
                            new List<BlockSyntax>
                            {
                                new(
                                    "^bb0",
                                    new List<BlockArgumentSyntax> { new("%arg0", new RawSyntaxText("i32")) },
                                    new List<OperationSyntax>
                                    {
                                        new(
                                            new List<string>(),
                                            "\"func.return\"",
                                            new List<string> { "%arg0" },
                                            new List<string>(),
                                            new List<RegionSyntax>(),
                                            new List<NamedAttributeSyntax>(),
                                            new RawSyntaxText("(i32) -> ()"))
                                    })
                            })
                    },
                    new List<NamedAttributeSyntax>(),
                    new RawSyntaxText("(i1) -> ()"))
            });

        var text = MlirPrinter.Print(module);

        Assert.Equal(
            "%sum = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32\n" +
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32):\n" +
            "    \"func.return\"(%arg0) : (i32) -> ()\n" +
            "} : (i1) -> ()",
            text);
    }

    [Fact]
    public void ThrowsHelpfulExceptionForInvalidInput()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"arith.addi\"(%lhs, %rhs"));

        Assert.Contains("operand list", exception.Message);
    }

    [Fact]
    public void ReportsLexerErrorForUnexpectedCharacter()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"arith.addi\"(%lhs) !"));

        Assert.Equal("Unexpected character '!'.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(20, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsLexerErrorForUnterminatedStringLiteral()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"arith.addi"));

        Assert.Equal("Unterminated string literal.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingOperationName()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("%0 = (%lhs)"));

        Assert.Equal("Expected an operation name.", exception.Diagnostic.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(6, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsParserErrorForMissingRegionTerminator()
    {
        var exception = Assert.Throws<MlirParseException>(() => MlirParser.ParseModule("\"scf.if\"(%cond) {\n  \"func.return\"() : () -> ()"));

        Assert.Equal("Expected an operation name.", exception.Diagnostic.Message);
        Assert.Equal(2, exception.Diagnostic.Line);
        Assert.Equal(29, exception.Diagnostic.Column);
    }
}
