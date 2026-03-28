namespace MLIR.Tests;

using System.Collections.Generic;
using MLIR.Construction;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed class ConstructionTests
{
    private sealed class PrefixConstantBodySyntax : OperationBodySyntax
    {
        private readonly GenericOperationBodySyntax genericBody;
        private readonly RawSyntaxText typeSignature;

        public PrefixConstantBodySyntax(
            RawSyntaxText value,
            SyntaxToken colonToken,
            RawSyntaxText typeSignature,
            DelimitedSyntaxList<NamedAttributeSyntax> attributes)
        {
            Value = value;
            ColonToken = colonToken;
            this.typeSignature = typeSignature;
            genericBody = new GenericOperationBodySyntax(
                new DelimitedSyntaxList<SyntaxToken>(new SyntaxToken("("), [], [], new SyntaxToken(")")),
                new DelimitedSyntaxList<SyntaxToken>(null, [], [], null),
                [],
                attributes,
                colonToken,
                new RawTypeSyntax(typeSignature));
        }

        public RawSyntaxText Value { get; }
        public SyntaxToken ColonToken { get; }

        public override bool TryGetGenericBody(out GenericOperationBodySyntax? genericBody)
        {
            genericBody = this.genericBody;
            return true;
        }

        public override void WriteTo(SyntaxWriter writer, int indentLevel, System.Action<SyntaxWriter, RegionSyntax, int> writeRegion)
        {
            writer.WriteRaw(Value, " ");
            writer.WriteToken(ColonToken, " ");
            writer.WriteRaw(typeSignature, " ");
        }
    }

    [Fact]
    public void ExposesPreservedTokenTriviaInTheCst()
    {
        const string source =
            "// leading comment\n" +
            "%0,  %1 = \"test.op\"(%lhs,  %rhs) [ ^bb1 ] : (i32, i32) -> i32";

        var module = Parser.ParseModule(source);
        var operation = module.Operations[0];

        Assert.Equal("// leading comment\n", operation.ResultTokens[0].LeadingTrivia);
        Assert.Equal(",", operation.ResultCommaTokens[0].Text);
        Assert.Equal("  ", operation.ResultTokens[1].LeadingTrivia);
        Assert.Equal(" ", operation.SuccessorList.OpenToken!.Value.LeadingTrivia);
        Assert.Equal(" ", operation.SuccessorList[0].LeadingTrivia);
        Assert.Equal(" ", operation.SuccessorList.CloseToken!.Value.LeadingTrivia);
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
        Assert.Equal("(", block.Arguments.OpenToken!.Value.Text);
        Assert.Equal(")", block.Arguments.CloseToken!.Value.Text);
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
        var module = Factory.Module(
            Factory.Op(
                name: "arith.constant",
                attributes: [Factory.Attr("value", "0 : i32")],
                type: "() -> i32"));

        var text = Printer.Print(module);

        Assert.Equal("\"arith.constant\"() {value = 0 : i32} : () -> i32", text);
    }

    [Fact]
    public void FactoryHelpersSupportRawAndDefaultEmptyLists()
    {
        var raw = Factory.Raw("tensor<2xi32>");
        var module = Factory.Module(Factory.Op(name: "\"test.op\""));

        Assert.Equal("tensor<2xi32>", raw.Text);
        Assert.Equal("\"test.op\"()", Printer.Print(module));
    }

    [Fact]
    public void FormatsProgrammaticallyGeneratedModuleWhenTriviaIsAbsent()
    {
        var module = Factory.Module(
            Factory.Op(
                name: "arith.addi",
                results: ["%sum"],
                operands: ["%lhs", "%rhs"],
                type: "(i32, i32) -> i32"),
            Factory.Op(
                name: "scf.if",
                operands: ["%cond"],
                regions:
                [
                    Factory.Region(
                        Factory.Block(
                            "^bb0",
                            args: [Factory.Arg("%arg0", "i32")],
                            ops:
                            [
                                Factory.Op(
                                    name: "func.return",
                                    operands: ["%arg0"],
                                    type: "(i32) -> ()")
                            ]))
                ],
                type: "(i1) -> ()"));

        var text = Printer.Print(module);

        Assert.Equal(
            "%sum = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32\n" +
            "\"scf.if\"(%cond) {\n" +
            "  ^bb0(%arg0: i32):\n" +
            "    \"func.return\"(%arg0) : (i32) -> ()\n" +
            "} : (i1) -> ()",
            text);
    }

    [Fact]
    public void FormatsProgrammaticallyGeneratedCustomBodiesWhenTriviaIsAbsent()
    {
        var module = new ModuleSyntax(
            [
                new OperationSyntax(
                    [new SyntaxToken("%0")],
                    [],
                    new SyntaxToken("="),
                    new SyntaxToken("arith.constant"),
                    new PrefixConstantBodySyntax(
                        new RawSyntaxText("0"),
                        new SyntaxToken(":"),
                        new RawSyntaxText("i32"),
                        new DelimitedSyntaxList<NamedAttributeSyntax>(
                            new SyntaxToken("{"),
                            [new NamedAttributeSyntax("value", new RawSyntaxText("0"))],
                            [],
                            new SyntaxToken("}")))),
            ]);

        var text = Printer.Print(module);

        Assert.Equal("%0 = arith.constant 0 : i32", text);
        Assert.True(module.Operations[0].HasCustomAssemblyBody);
        Assert.Equal("0", module.Operations[0].Attributes[0].RawValue.Text);
    }
}
