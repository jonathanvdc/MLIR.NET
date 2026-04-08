namespace MLIR.Tests;

using MLIR;
using MLIR.Dialects;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Syntax.Types.Collections;
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
                new DelimitedSyntaxList<SyntaxToken>(SyntaxTokenFactory.LParen(), [], [], SyntaxTokenFactory.RParen()),
                new DelimitedSyntaxList<SyntaxToken>(null, [], [], null),
                [],
                attributes,
                colonToken,
                new RawTypeSyntax(typeSignature));
        }

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.WriteRaw(value, " ");
            writer.WriteToken(this.genericBody.TypeSignatureColonToken ?? SyntaxTokenFactory.Colon(), " ");
            writer.WriteRaw(typeSignature, " ");
        }

        public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
        {
            return new PrefixConstantBodySyntax(
                rewriter.VisitRawText(value),
                rewriter.VisitToken(genericBody.TypeSignatureColonToken!.Value),
                rewriter.VisitRawText(typeSignature),
                rewriter.VisitDelimitedList(genericBody.Attributes));
        }
    }

    private sealed class PrefixConstantAssemblyFormat : IOperationAssemblyFormat
    {
        public ParseResult<OperationBodySyntax> TryParse(
            SyntaxToken nameToken,
            SeparatedSyntaxList<SyntaxToken> resultList,
            SyntaxToken? equalsToken,
            OperationParsingContext context)
        {
            if (context.Is(TokenKind.LParen))
            {
                return ParseResult<OperationBodySyntax>.NoMatch();
            }

            var valueResult = context.TryParseRawUntilDelimiter(TokenKind.Colon);
            if (!valueResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(valueResult.Diagnostic!);
            }

            var colonTokenResult = context.Expect(TokenKind.Colon, "Expected ':' after the custom constant value.");
            if (!colonTokenResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(colonTokenResult.Diagnostic!);
            }

            var typeResult = context.TryParseRawUntilOperationBoundary();
            if (!typeResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(typeResult.Diagnostic!);
            }

            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(SyntaxTokenFactory.Identifier("value"), SyntaxTokenFactory.Equal(), new RawAttributeValueSyntax(valueResult.Value))]);
            return ParseResult<OperationBodySyntax>.Success(new PrefixConstantBodySyntax(valueResult.Value, colonTokenResult.Value, typeResult.Value, attributes));
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

    /// <summary>
    /// An operation body that captures the SSA token list parsed by <see cref="SsaListCapturingAssemblyFormat"/>.
    /// </summary>
    private sealed class SsaListCapturingBodySyntax : OperationBodySyntax
    {
        public SsaListCapturingBodySyntax(SeparatedSyntaxList<SyntaxToken> inputs)
        {
            Inputs = inputs;
        }

        /// <summary>Gets the SSA tokens that were parsed by <see cref="OperationParsingContext.TryParseSsaTokenList"/>.</summary>
        public SeparatedSyntaxList<SyntaxToken> Inputs { get; }

        public override void WriteTo(SyntaxWriter writer)
        {
            for (var i = 0; i < Inputs.Count; i++)
            {
                if (i > 0)
                {
                    writer.WriteToken(SyntaxTokenFactory.Comma());
                }

                writer.WriteToken(Inputs[i], " ");
            }
        }

        public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
        {
            return new SsaListCapturingBodySyntax(rewriter.VisitSeparatedTokenList(Inputs));
        }
    }

    /// <summary>
    /// A custom assembly format that uses <see cref="OperationParsingContext.TryParseSsaTokenList"/>
    /// to parse a variadic SSA operand list and stores the result in a
    /// <see cref="SsaListCapturingBodySyntax"/>.
    /// </summary>
    private sealed class SsaListCapturingAssemblyFormat : IOperationAssemblyFormat
    {
        public ParseResult<OperationBodySyntax> TryParse(
            SyntaxToken nameToken,
            SeparatedSyntaxList<SyntaxToken> resultList,
            SyntaxToken? equalsToken,
            OperationParsingContext context)
        {
            var listResult = context.TryParseSsaTokenList();
            if (!listResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(listResult.Diagnostic!);
            }

            return ParseResult<OperationBodySyntax>.Success(new SsaListCapturingBodySyntax(listResult.Value));
        }

        public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
        {
            throw new NotImplementedException("This assembly format is only intended for testing SSA list parsing.");
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

        Assert.Equal("(memref<2x?xf32, #map>) -> memref<*xf32>", ((GenericOperationBodySyntax)module.Operations[0].Body).TypeSignatureSyntax!.ToString());
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

    [Theory]
    [InlineData("42", 42)]
    [InlineData("-42", -42)]
    [InlineData("+42", 42)]
    public void ParsesStandaloneIntegerAttributeValues(string source, int expectedValue)
    {
        var syntax = Parser.ParseAttributeValue(source);

        var integerSyntax = Assert.IsType<IntegerAttributeValueSyntax>(syntax);
        Assert.Equal(expectedValue, (int)integerSyntax.Value.ToBigIntegerSigned());
        var expectedDigits = source.StartsWith('+') || source.StartsWith('-') ? source[1..] : source;
        Assert.Equal(expectedDigits, integerSyntax.IntegerToken.Text);
        Assert.Equal(source.StartsWith('+') || source.StartsWith('-') ? source[..1] : null, integerSyntax.SignToken?.Text);
        Assert.Equal(source, syntax.ToString());
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("+1.5")]
    [InlineData("1e3")]
    [InlineData("0x3f800000")]
    public void ParsesStandaloneFloatingPointAttributeValues(string source)
    {
        var syntax = Parser.ParseAttributeValue(source);

        var floatingPointSyntax = Assert.IsType<FloatingPointAttributeValueSyntax>(syntax);
        Assert.Equal(source, syntax.ToString());
        Assert.Equal(source, floatingPointSyntax.LiteralText);
        Assert.Equal(FloatSemantics.IEEEDouble, floatingPointSyntax.Value.Semantics);
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

        Assert.Equal("dense<[[1, 2], [3, 4]]> : tensor<2x2xi32>", attribute.ValueSyntax.ToString());
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void PreservesDenseArrayTriviaWhenItemsHaveNoSpaces()
    {
        const string source = "\"test.op\"() {value = #dense<[1,2,3]>} : () -> i32";

        var module = Parser.ParseModule(source);

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
            static dimension => Assert.Equal("2", dimension.ToString()),
            static dimension => Assert.Equal("?", dimension.ToString()));
        Assert.Equal("f32", tensor.ElementType.ToString());
        Assert.Equal("4", vector.Dimensions[0].ToString());
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

    private static DialectRegistry CreateSsaListCapturingRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "test",
                dialect =>
                {
                    dialect.AddOperation(
                        "test.variadic",
                        operation => operation.WithAssemblyFormat(new SsaListCapturingAssemblyFormat()));
                }));
        return registry;
    }

    [Fact]
    public void TryParseSsaTokenListReturnsEmptyListWhenCurrentTokenIsNotSsaName()
    {
        // The operation body contains no SSA name token, so TryParseSsaTokenList should
        // return a successful empty list rather than failing or throwing.
        var module = Parser.ParseModule("test.variadic", CreateSsaListCapturingRegistry());

        var body = Assert.IsType<SsaListCapturingBodySyntax>(module.Operations[0].Body);
        Assert.Empty(body.Inputs);
    }

    [Fact]
    public void TryParseSsaTokenListReturnsSingleToken()
    {
        var module = Parser.ParseModule("test.variadic %a", CreateSsaListCapturingRegistry());

        var body = Assert.IsType<SsaListCapturingBodySyntax>(module.Operations[0].Body);
        Assert.Single(body.Inputs);
        Assert.Equal("%a", body.Inputs[0].Text);
    }

    [Fact]
    public void TryParseSsaTokenListStopsAtFirstNonSsaCommaToken()
    {
        // The list should stop at the second operand after which there is no comma,
        // and must not consume any token that follows a complete comma-separated sequence.
        var module = Parser.ParseModule("test.variadic %a, %b", CreateSsaListCapturingRegistry());

        var body = Assert.IsType<SsaListCapturingBodySyntax>(module.Operations[0].Body);
        Assert.Equal(2, body.Inputs.Count);
        Assert.Equal("%a", body.Inputs[0].Text);
        Assert.Equal("%b", body.Inputs[1].Text);
    }
}
