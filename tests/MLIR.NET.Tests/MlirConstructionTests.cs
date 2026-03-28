namespace MLIR.Tests;

using System.Collections.Generic;
using MLIR.Construction;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed class MlirConstructionTests
{
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
        Assert.Equal("%0", operation.Results[0]);
        Assert.Equal("\"test.op\"", operation.Name);
        Assert.Equal("%lhs", operation.Operands[0]);
        Assert.Equal("^bb1", operation.Successors[0]);
    }

    [Fact]
    public void DelimitedListsEnumerateItemsByDefault()
    {
        var block = new BlockSyntax(
            "^bb0",
            [
                new("%arg0", new RawSyntaxText("i32")),
                new("%arg1", new RawSyntaxText("i64")),
            ],
            []);

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
    public void SyntaxTokenAndRawSyntaxTextExposeFullText()
    {
        var token = new SyntaxToken("value", "  ");
        var raw = new RawSyntaxText("i32", " ");

        Assert.Equal("  value", token.FullText);
        Assert.Equal("  value", token.ToString());
        Assert.Equal(" i32", raw.FullText);
        Assert.Equal(" i32", raw.ToString());
        Assert.True(raw.HasLeadingTrivia);
    }

    [Fact]
    public void FactoryHelpersQuoteOperationNamesAndBuildAttributes()
    {
        var module = MlirFactory.Module(
            MlirFactory.Op(
                name: "arith.constant",
                attributes: [MlirFactory.Attr("value", "0 : i32")],
                type: "() -> i32"));

        var text = MlirPrinter.Print(module);

        Assert.Equal("\"arith.constant\"() {value = 0 : i32} : () -> i32", text);
    }

    [Fact]
    public void FactoryHelpersSupportRawAndDefaultEmptyLists()
    {
        var raw = MlirFactory.Raw("tensor<2xi32>");
        var module = MlirFactory.Module(MlirFactory.Op(name: "\"test.op\""));

        Assert.Equal("tensor<2xi32>", raw.Text);
        Assert.Equal("\"test.op\"()", MlirPrinter.Print(module));
    }

    [Fact]
    public void FormatsProgrammaticallyGeneratedModuleWhenTriviaIsAbsent()
    {
        var module = MlirFactory.Module(
            MlirFactory.Op(
                name: "arith.addi",
                results: ["%sum"],
                operands: ["%lhs", "%rhs"],
                type: "(i32, i32) -> i32"),
            MlirFactory.Op(
                name: "scf.if",
                operands: ["%cond"],
                regions:
                [
                    MlirFactory.Region(
                        MlirFactory.Block(
                            "^bb0",
                            args: [MlirFactory.Arg("%arg0", "i32")],
                            ops:
                            [
                                MlirFactory.Op(
                                    name: "func.return",
                                    operands: ["%arg0"],
                                    type: "(i32) -> ()")
                            ]))
                ],
                type: "(i1) -> ()"));

        var text = MlirPrinter.Print(module);

        Assert.Equal(
            "%sum = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32\n" +
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32):\n" +
            "    \"func.return\"(%arg0) : (i32) -> ()\n" +
            "} : (i1) -> ()",
            text);
    }
}
