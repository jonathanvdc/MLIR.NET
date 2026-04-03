namespace MLIR.Tests;

using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;
using Xunit;
using Xunit.Sdk;

public sealed class ParsingTests
{
    private sealed class PrefixConstantBodySyntax : OperationBodySyntax
    {
        private readonly GenericOperationBodySyntax genericBody;
        private readonly RawSyntaxText value;
        private readonly RawSyntaxText typeSignature;

        public PrefixConstantBodySyntax(
            RawSyntaxText value,
            SyntaxToken colonToken,
            RawSyntaxText typeSignature,
            DelimitedSyntaxList<NamedAttributeSyntax> attributes)
        {
            this.value = value;
            this.typeSignature = typeSignature;
            genericBody = new GenericOperationBodySyntax(
                new DelimitedSyntaxList<SyntaxToken>(new SyntaxToken("("), [], [], new SyntaxToken(")")),
                new DelimitedSyntaxList<SyntaxToken>(null, [], [], null),
                [],
                attributes,
                colonToken,
                new RawTypeSyntax(typeSignature));
        }

        public override void WriteTo(SyntaxWriter writer, int indentLevel)
        {
            writer.WriteRaw(value, " ");
            writer.WriteToken(this.genericBody.TypeSignatureColonToken ?? new SyntaxToken(":"), " ");
            writer.WriteRaw(typeSignature, " ");
        }
    }

    private sealed class PrefixConstantAssemblyFormat : IOperationAssemblyFormat
    {
        public bool TryParse(
            SyntaxToken nameToken,
            IReadOnlyList<SyntaxToken> resultTokens,
            IReadOnlyList<SyntaxToken> resultCommaTokens,
            SyntaxToken? equalsToken,
            OperationParsingContext context,
            out OperationBodySyntax? body)
        {
            if (context.Is(TokenKind.LParen))
            {
                body = null;
                return false;
            }

            var value = context.ParseRawUntilDelimiter(TokenKind.Colon);
            var colonToken = context.Expect(TokenKind.Colon, "Expected ':' after the custom constant value.");
            var type = context.ParseRawUntilOperationBoundary();
            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(new SyntaxToken("value"), new SyntaxToken("="), new RawAttributeValueSyntax(value))]);
            body = new PrefixConstantBodySyntax(value, colonToken, type, attributes);
            return true;
        }

        public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
        {
            throw new NotImplementedException("This assembly format is only intended for testing CSTs.");
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
        Assert.Single(GetGenericBody(module.Operations[0]).Regions);
        Assert.Equal("^bb2", GetGenericBody(GetGenericBody(module.Operations[0]).Regions[0].Blocks[0].Operations[0]).SuccessorList[0].Text);
    }

    [Fact]
    public void PreservesStructuredTypeSignatureText()
    {
        const string source = "\"memref.cast\"(%arg0) : (memref<2x?xf32, #map>) -> memref<*xf32>";

        var module = Parser.ParseModule(source);

        Assert.Equal("(memref<2x?xf32, #map>) -> memref<*xf32>", ((GenericOperationBodySyntax)module.Operations[0].Body).RawTypeSignature!.Text);
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
        Assert.Equal(3, GetGenericBody(module.Operations[1]).Regions[0].Blocks.Count);
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

        Assert.Empty(GetGenericBody(operation).Regions);
        Assert.Empty(GetGenericBody(operation).Attributes);
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
        var blocks = GetGenericBody(module.Operations[0]).Regions[0].Blocks;

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
        var attribute = GetGenericBody(module.Operations[0]).Attributes[0];

        Assert.Equal("dense<[[1, 2], [3, 4]]> : tensor<2x2xi32>", attribute.RawValue.Text);
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void ParsesBuiltinTypeSyntaxIntoStructuredNodes()
    {
        var syntax = Parser.ParseType("(tensor<2x?xf32>) -> tuple<vector<4xf32>, memref<*xf32, #map>>");

        var function = Assert.IsType<FunctionTypeSyntax>(syntax);
        var tensor = Assert.IsType<TensorTypeSyntax>(function.InputTypes[0]);
        var tuple = Assert.IsType<TupleTypeSyntax>(function.ResultType);
        var vector = Assert.IsType<VectorTypeSyntax>(tuple.Elements[0]);
        var memref = Assert.IsType<MemRefTypeSyntax>(tuple.Elements[1]);

        Assert.Collection(
            tensor.Dimensions,
            static dimension => Assert.Equal("2", dimension.GetRawText().Text),
            static dimension => Assert.Equal("?", dimension.GetRawText().Text));
        Assert.Equal("f32", tensor.ElementType.GetRawText().Text);
        Assert.Equal("4", vector.Dimensions[0].GetRawText().Text);
        Assert.True(memref.IsUnranked);
        Assert.Equal("#map", Assert.Single(memref.TrailingParameters).Text);
    }

    // [Fact]
    // public void CanRewriteCustomAssemblySyntaxToGenericSyntax()
    // {
    //     var registry = new DialectRegistry();
    //     registry.RegisterDialect(
    //         Dialect.Create(
    //             "arith",
    //             dialect =>
    //             {
    //                 dialect.AddOperation(
    //                     "arith.constant",
    //                     operation => operation.WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
    //             }));

    //     var module = Parser.ParseModule("%0 = arith.constant 0 : i32", registry);
    //     var genericModule = GenericSyntaxBuilder.BuildModule(module);

    //     Assert.True(module.Operations[0].HasCustomAssemblyBody);
    //     Assert.False(genericModule.Operations[0].HasCustomAssemblyBody);
    //     Assert.Equal("%0 = arith.constant() {value = 0} : i32", Printer.Print(genericModule));
    // }

    // [Fact]
    // public void RewritesNestedCustomAssemblySyntaxToGenericSyntaxRecursively()
    // {
    //     var registry = new DialectRegistry();
    //     registry.RegisterDialect(
    //         Dialect.Create(
    //             "arith",
    //             dialect =>
    //             {
    //                 dialect.AddOperation(
    //                     "arith.constant",
    //                     operation => operation.WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
    //             }));

    //     var module = Parser.ParseModule(
    //         "\"scf.if\"(%cond) {\n" +
    //         "  %0 = arith.constant 0 : i32\n" +
    //         "  \"func.return\"(%0) : (i32) -> ()\n" +
    //         "} : (i1) -> ()",
    //         registry);

    //     var genericModule = GenericSyntaxBuilder.BuildModule(module);

    //     Assert.True(module.Operations[0].Regions[0].Blocks[0].Operations[0].HasCustomAssemblyBody);
    //     Assert.False(genericModule.Operations[0].Regions[0].Blocks[0].Operations[0].HasCustomAssemblyBody);
    //     Assert.Equal(
    //         "\"scf.if\"(%cond) {\n" +
    //         "  %0 = arith.constant() {value = 0} : i32\n" +
    //         "  \"func.return\"(%0) : (i32) -> ()\n" +
    //         "} : (i1) -> ()",
    //         Printer.Print(genericModule));
    // }
}
