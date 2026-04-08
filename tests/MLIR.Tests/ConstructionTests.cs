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
                new DelimitedSyntaxList<SyntaxToken>(SyntaxTokenFactory.LParen(), [], [], SyntaxTokenFactory.RParen()),
                new DelimitedSyntaxList<SyntaxToken>(null, [], [], null),
                [],
                attributes,
                colonToken,
                new RawTypeSyntax(typeSignature));
        }

        public RawSyntaxText Value { get; }
        public SyntaxToken ColonToken { get; }

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.WriteRaw(Value, " ");
            writer.WriteToken(ColonToken, " ");
            writer.WriteRaw(typeSignature, " ");
        }

        public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
        {
            return new PrefixConstantBodySyntax(
                rewriter.VisitRawText(Value),
                rewriter.VisitToken(ColonToken),
                rewriter.VisitRawText(typeSignature),
                rewriter.VisitDelimitedList(genericBody.Attributes));
        }
    }

    private static GenericOperationBodySyntax GetGenericBody(OperationSyntax operation)
    {
        if (operation.Body is GenericOperationBodySyntax genericBody)
        {
            return genericBody;
        }

        throw new InvalidOperationException("Expected a generic operation body syntax node.");
    }

    [Fact]
    public void ExposesPreservedTokenTriviaInTheCst()
    {
        const string source =
            "// leading comment\n" +
            "%0,  %1 = \"test.op\"(%lhs,  %rhs) [ ^bb1 ] : (i32, i32) -> i32";

        var module = Parser.ParseModule(source);
        var operation = module.Operations[0];

        Assert.Equal("// leading comment\n", operation.ResultList[0].LeadingTrivia);
        Assert.Equal(",", operation.ResultList.SeparatorTokens[0].Text);
        Assert.Equal("  ", operation.ResultList[1].LeadingTrivia);
        Assert.Equal(" ", GetGenericBody(operation).SuccessorList.OpenToken!.Value.LeadingTrivia);
        Assert.Equal(" ", GetGenericBody(operation).SuccessorList[0].LeadingTrivia);
        Assert.Equal(" ", GetGenericBody(operation).SuccessorList.CloseToken!.Value.LeadingTrivia);
        Assert.Equal("%0", operation.Results[0]);
        Assert.Equal("\"test.op\"", operation.Name);
        Assert.Equal("%lhs", GetGenericBody(operation).OperandList[0].Text);
        Assert.Equal("^bb1", GetGenericBody(operation).SuccessorList[0].Text);
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
        var token = SyntaxTokenFactory.Identifier("value", "  ");
        var raw = new RawSyntaxText([SyntaxTokenFactory.Identifier("i32", leadingTrivia: " ")]);

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
                type: Factory.Type("() -> i32")));

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
                type: Factory.Type("(i32, i32) -> i32")),
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
                                    type: Factory.Type("(i32) -> ()"))
                            ]))
                ],
                type: Factory.Type("(i1) -> ()")));

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
                    new SeparatedSyntaxList<SyntaxToken>([SyntaxTokenFactory.SsaName("%0")], []),
                    SyntaxTokenFactory.Equal(),
                    SyntaxTokenFactory.Identifier("arith.constant"),
                    new PrefixConstantBodySyntax(
                        new RawSyntaxText("0"),
                        SyntaxTokenFactory.Colon(),
                        new RawSyntaxText("i32"),
                        new DelimitedSyntaxList<NamedAttributeSyntax>(
                            SyntaxTokenFactory.LBrace(),
                            [new NamedAttributeSyntax("value", new RawSyntaxText("0"))],
                            [],
                            SyntaxTokenFactory.RBrace()))),
            ]);

        var text = Printer.Print(module);

        Assert.Equal("%0 = arith.constant 0 : i32", text);
        Assert.True(module.Operations[0].HasCustomAssemblyBody);
    }

    [Fact]
    public void DelimitedSyntaxListWriteToWritesAllTokensAndElements()
    {
        var list = new DelimitedSyntaxList<BlockArgumentSyntax>(
            SyntaxTokenFactory.LParen(),
            [
                new BlockArgumentSyntax(SyntaxTokenFactory.SsaName("%arg0"), SyntaxTokenFactory.Colon(), new RawTypeSyntax(new RawSyntaxText("i32"))),
                new BlockArgumentSyntax(SyntaxTokenFactory.SsaName("%arg1"), SyntaxTokenFactory.Colon(), new RawTypeSyntax(new RawSyntaxText("i64"))),
            ],
            [SyntaxTokenFactory.Comma()],
            SyntaxTokenFactory.RParen());

        var writer = new SyntaxWriter();
        writer.WriteDelimitedList(list);

        Assert.Equal("(%arg0: i32, %arg1: i64)", writer.ToString());
    }

    [Fact]
    public void DelimitedSyntaxListWriteToDoesNothingWhenNotPresent()
    {
        var list = new DelimitedSyntaxList<BlockArgumentSyntax>(
            null,
            [],
            [],
            null);

        Assert.False(list.IsPresent);

        var writer = new SyntaxWriter();
        writer.WriteDelimitedList(list);

        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void SeparatedSyntaxListWritesToWritesAllTokensAndSeparators()
    {
        var list = new SeparatedSyntaxList<SyntaxToken>(
            [SyntaxTokenFactory.SsaName("%a"), SyntaxTokenFactory.SsaName("%b"), SyntaxTokenFactory.SsaName("%c")],
            [SyntaxTokenFactory.Comma(), SyntaxTokenFactory.Comma()]);

        var writer = new SyntaxWriter();
        writer.WriteSeparatedList(list);

        Assert.Equal("%a, %b, %c", writer.ToString());
    }

    [Fact]
    public void SeparatedSyntaxListWriteToDoesNothingWhenEmpty()
    {
        var list = SeparatedSyntaxList<SyntaxToken>.Empty;

        var writer = new SyntaxWriter();
        writer.WriteSeparatedList(list);

        Assert.Equal(string.Empty, writer.ToString());
        Assert.Empty(list);
    }

    [Fact]
    public void SeparatedSyntaxListPreservesLeadingTriviaOnSeparators()
    {
        // Separator tokens with stored leading trivia override the default spacing.
        var list = new SeparatedSyntaxList<SyntaxToken>(
            [SyntaxTokenFactory.SsaName("%a", string.Empty), SyntaxTokenFactory.SsaName("%b", string.Empty)],
            [SyntaxTokenFactory.Comma(string.Empty)]);

        Assert.Equal(2, list.Count);
        Assert.Equal(",", list.SeparatorTokens[0].Text);
        Assert.Equal("%a", list[0].Text);
        Assert.Equal("%b", list[1].Text);
    }
}
