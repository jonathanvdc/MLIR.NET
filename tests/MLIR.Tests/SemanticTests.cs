namespace MLIR.Tests;

using System.Collections.Generic;
using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;
using Xunit;

public sealed class SemanticTests
{
    private static Dialect CreateArithConstantDialect()
    {
        return Dialect.Create(
            "arith",
            dialect =>
            {
                dialect.AddOperation(
                    "arith.constant",
                    operation => operation
                        .WithFactory(static context => new GeneratedConstantOperation(context))
                        .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
            });
    }

    private sealed class PrefixConstantBodySyntax : OperationBodySyntax
    {
        private readonly GenericOperationBodySyntax genericBody;

        public PrefixConstantBodySyntax(
            RawSyntaxText value,
            SyntaxToken colonToken,
            TypeSyntax typeSignature,
            DelimitedSyntaxList<NamedAttributeSyntax> attributes)
        {
            Value = value;
            ColonToken = colonToken;
            TypeSignature = typeSignature;
            genericBody = new GenericOperationBodySyntax(
                new DelimitedSyntaxList<SyntaxToken>(new SyntaxToken("("), [], [], new SyntaxToken(")")),
                new DelimitedSyntaxList<SyntaxToken>(null, [], [], null),
                [],
                attributes,
                colonToken,
                typeSignature);
        }

        public RawSyntaxText Value { get; }
        public SyntaxToken ColonToken { get; }
        public TypeSyntax TypeSignature { get; }

        public override void WriteTo(SyntaxWriter writer, int indentLevel)
        {
            writer.WriteRaw(Value, " ");
            writer.WriteToken(ColonToken, " ");
            writer.WriteType(TypeSignature, " ");
        }
    }

    private sealed class ArithConstantView
    {
        private readonly Operation operation;
        public ArithConstantView(Operation operation)
        {
            if (operation.Name != "arith.constant")
            {
                throw new System.ArgumentException(
                    $"Expected operation 'arith.constant' but received '{operation.Name}'.",
                    nameof(operation));
            }

            this.operation = operation;
            typedOperation = operation as GeneratedConstantOperation;
        }

        private readonly GeneratedConstantOperation? typedOperation;
        public IReadOnlyList<string> Results => operation.Results;
        public NamedAttribute ValueAttribute => operation.GetAttribute("value");
        public ValueReference ResultValue => operation.ResultValues[0];
    }

    private sealed class GeneratedConstantOperation : Operation
    {
        public readonly NamedAttribute ValueAttribute;
        public readonly ValueReference ResultValue;

        public GeneratedConstantOperation(OperationSyntax syntax, OperationDefinition definition, ValueReference resultValue, AttributeValue value, TypeReference typeSignatureReference)
            : base(
                syntax,
                definition.Name,
                definition)
        {
            ValueAttribute = new NamedAttribute("value", value);
            ResultValue = resultValue;
            TypeSignatureReference = typeSignatureReference;
        }

        public GeneratedConstantOperation(OperationConstructionContext context)
            : base(
                context.Syntax,
                context.Name,
                context.Definition)
        {
            ValueAttribute = context.GetAttribute("value");
            ResultValue = context.ResultValues.Single();
            TypeSignatureReference = context.TypeSignatureReference;
        }

        public override IReadOnlyList<Region> Regions => [];
        public override IReadOnlyList<NamedAttribute> Attributes => [ValueAttribute];
        public override TypeReference? TypeSignatureReference { get; }
        public override IReadOnlyList<ValueReference> ResultValues => [ResultValue];
        public override IReadOnlyList<ValueReference> OperandValues => [];
        public override IReadOnlyList<BlockReference> SuccessorReferences => [];
    }

    private sealed class DenseAttributeValue : AttributeValue
    {
        public DenseAttributeValue(AttributeValueConstructionContext context)
            : base(context.Syntax, context.Name, context.Definition, context.Location)
        {
        }

        public string? Kind { get; private set; }

        public void BindDense()
        {
            Kind = "dense";
        }
    }

    private sealed class BuiltinIntegerTypeReference : TypeReference
    {
        public BuiltinIntegerTypeReference(TypeReferenceConstructionContext context)
            : base(context.Syntax, context.Name, context.Definition, context.Location)
        {
        }

        public int? Width { get; private set; }

        public void BindWidth(int width)
        {
            Width = width;
        }
    }

    private sealed class GeneratedAddIOperation : Operation
    {
        private static readonly IReadOnlyList<Region> EmptyRegions = [];
        private static readonly IReadOnlyList<NamedAttribute> EmptyAttributes = [];
        private static readonly IReadOnlyList<BlockReference> EmptySuccessors = [];

        public GeneratedAddIOperation(OperationConstructionContext context)
            : base(
                context.Syntax,
                context.Name,
                context.Definition)
        {
            LeftOperand = context.OperandValues[0];
            RightOperand = context.OperandValues[1];
            ResultValue = context.ResultValues[0];
        }

        public override IReadOnlyList<Region> Regions => EmptyRegions;
        public override IReadOnlyList<NamedAttribute> Attributes => EmptyAttributes;
        public override TypeReference? TypeSignatureReference => null;
        public override IReadOnlyList<ValueReference> ResultValues => [ResultValue];
        public override IReadOnlyList<ValueReference> OperandValues => [LeftOperand, RightOperand];
        public override IReadOnlyList<BlockReference> SuccessorReferences => EmptySuccessors;

        public ValueReference LeftOperand { get; }
        public ValueReference RightOperand { get; }
        public ValueReference ResultValue { get; }
    }

    private sealed class SyntheticOperation : Operation
    {
        public SyntheticOperation(string name)
            : base(null, name, null)
        {
        }

        public override IReadOnlyList<Region> Regions => [];
        public override IReadOnlyList<NamedAttribute> Attributes => [];
        public override TypeReference? TypeSignatureReference => null;
        public override IReadOnlyList<ValueReference> ResultValues => [];
        public override IReadOnlyList<ValueReference> OperandValues => [];
        public override IReadOnlyList<BlockReference> SuccessorReferences => [];
    }

    private sealed class SyntheticAttributeValue : AttributeValue
    {
        public SyntheticAttributeValue(string name)
            : base(null, name, null, SourceLocation.Unknown)
        {
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
            var type = new RawTypeSyntax(context.ParseRawUntilOperationBoundary());
            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(new SyntaxToken("value"), new SyntaxToken("="), new RawAttributeValueSyntax(value))]);

            body = new PrefixConstantBodySyntax(value, colonToken, type, attributes);
            return true;
        }

        public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
        {
            var body = (PrefixConstantBodySyntax)syntax.Body;
            return new GeneratedConstantOperation(
                syntax,
                definition,
                binder.BindValueReference(syntax.ResultTokens.Single()),
                binder.BindAttributeValue(body.Value),
                binder.BindTypeReference(body.TypeSignature));
        }

        public OperationSyntax Rewrite(Operation operation, OperationSyntaxTransformContext context)
        {
            var genericBody = context.TransformGenericBody(operation);
            var body = new PrefixConstantBodySyntax(
                operation.HasAttribute("value") ? operation.GetAttribute("value").Value.Syntax! : new RawSyntaxText(string.Empty),
                genericBody.TypeSignatureColonToken ?? new SyntaxToken(":"),
                genericBody.TypeSignatureSyntax ?? throw new InvalidOperationException("Expected a type signature in the generic body for rewriting."),
                genericBody.Attributes);
            var sourceNameToken = operation.Syntax!.NameToken;
            var rewrittenNameToken = new SyntaxToken(operation.Name, sourceNameToken.LeadingTrivia, sourceNameToken.Line, sourceNameToken.Column);
            return context.RewriteOperation(operation, body, rewrittenNameToken);
        }
    }

    private sealed class DenseAttributeAssemblyFormat : IAttributeAssemblyFormat
    {
        public void Bind(AttributeValue attribute, AttributeAssemblyBindingContext context)
        {
            if (attribute is DenseAttributeValue denseAttribute)
            {
                denseAttribute.BindDense();
            }

            if (!attribute.Syntax!.Text.Contains("tensor<"))
            {
                context.Report("dense attribute literals should mention a tensor type.");
            }
        }
    }

    private sealed class BuiltinIntegerTypeAssemblyFormat : ITypeAssemblyFormat
    {
        public void Bind(TypeReference type, TypeAssemblyBindingContext context)
        {
            if (type is BuiltinIntegerTypeReference integerType)
            {
                integerType.BindWidth(int.Parse(type.Name![1..]));
            }
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
    public void BindsRegisteredOperationsToDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("arith", [new OperationDefinition("arith.addi")]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32"),
            registry);

        var operation = module.Operations[0];
        Assert.True(operation.IsKnown);
        Assert.IsType<UnknownOperation>(operation);
        Assert.Equal("arith.addi", operation.Name);
        Assert.Equal("\"arith.addi\"", operation.SyntaxName);
        Assert.Equal("arith", operation.DialectName);
        Assert.NotNull(operation.Definition);
        Assert.Equal("%0", operation.ResultValues[0].Name);
        Assert.Equal("%lhs", operation.OperandValues[0].Name);
    }

    [Fact]
    public void LeavesUnknownOperationsUnbound()
    {
        var module = Binder.BindModule(Parser.ParseModule("\"test.unknown\"() : () -> ()"));

        var operation = module.Operations[0];
        Assert.False(operation.IsKnown);
        Assert.IsType<UnknownOperation>(operation);
        Assert.Null(operation.Definition);
        Assert.Equal("test.unknown", operation.Name);
    }

    [Fact]
    public void BinderCanConstructGeneratedTypedOperations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.addi",
                        operation => operation
                            .Operand("lhs")
                            .Operand("rhs")
                            .Result("result")
                            .WithFactory(static context => new GeneratedAddIOperation(context)));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%sum = \"arith.addi\"(%lhs, %rhs) : (i32, i32) -> i32"),
            registry);

        var operation = Assert.IsType<GeneratedAddIOperation>(module.Operations[0]);
        Assert.Equal("%lhs", operation.LeftOperand.Name);
        Assert.Equal("%rhs", operation.RightOperand.Name);
        Assert.Equal("%sum", operation.ResultValue.Name);
    }

    [Fact]
    public void BindsNestedRegionsBlocksArgumentsAndAttributes()
    {
        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  ^bb0(%arg0: i32):\n" +
                "    \"func.return\"(%arg0) {value = 1 : i32} : (i32) -> ()\n" +
                "} : (i1) -> ()"));

        var region = module.Operations[0].Regions[0];
        var block = region.Blocks[0];
        var nestedOperation = block.Operations[0];

        Assert.Single(module.Operations);
        Assert.Equal("^bb0", block.Label);
        Assert.Single(block.Arguments);
        Assert.Equal("%arg0", block.Arguments[0].Name);
        Assert.Equal("i32", block.Arguments[0].Type.Text);
        Assert.Single(nestedOperation.Attributes);
        Assert.Equal("value", nestedOperation.Attributes[0].Name);
        Assert.Equal("1 : i32", nestedOperation.Attributes[0].Value.Syntax!.Text);
        Assert.Equal("%arg0", block.Arguments[0].Value.Name);
        Assert.Equal("i32", block.Arguments[0].TypeReference.Name);
    }

    [Fact]
    public void BindsAttributeAndTypeDefinitionsFromTheRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [new AttributeDefinition("dense", new DenseAttributeAssemblyFormat(), static context => new DenseAttributeValue(context))],
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat(), static context => new BuiltinIntegerTypeReference(context))]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.op\"() {value = #dense<[1, 2]> : tensor<2xi32>} : i32"),
            registry);

        var operation = module.Operations[0];

        Assert.True(operation.Attributes[0].Value.IsKnown);
        Assert.Equal("dense", operation.Attributes[0].Value.Name);
        Assert.Equal("dense", Assert.IsType<DenseAttributeValue>(operation.Attributes[0].Value).Kind);
        Assert.NotNull(operation.TypeSignatureReference);
        Assert.True(operation.TypeSignatureReference!.IsKnown);
        Assert.Equal("i32", operation.TypeSignatureReference.Name);
        Assert.Equal(32, Assert.IsType<BuiltinIntegerTypeReference>(operation.TypeSignatureReference).Width);
    }

    [Fact]
    public void BindsTypedSuccessorReferences()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"cf.cond_br\"(%cond) [^then, ^else] : (i1) -> ()"));

        var operation = module.Operations[0];

        Assert.Equal("^then", operation.SuccessorReferences[0].Label);
        Assert.Equal("^else", operation.SuccessorReferences[1].Label);
    }

    [Fact]
    public void DocumentBindUsesTheDialectRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("func", [new OperationDefinition("func.return")]));

        var module = Document.Parse("\"func.return\"() : () -> ()").Bind(registry);

        Assert.True(module.Operations[0].IsKnown);
        Assert.Equal("func.return", module.Operations[0].Name);
    }

    [Fact]
    public void VerifierReportsDiagnosticsFromRegisteredDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "arith",
                [
                    new OperationDefinition(
                        "arith.addi",
                        operandDefinitions:
                        [
                            new OperationSegmentDefinition("lhs"),
                            new OperationSegmentDefinition("rhs"),
                        ],
                        verifier: new DelegateOperationVerifier(
                            static (operation, context) =>
                            {
                                if (operation.Operands.Count != 2)
                                {
                                    context.Report("arith.addi expects exactly two operands.");
                                }
                            })),
                ]));

        var module = Binder.BindModule(Parser.ParseModule("\"arith.addi\"(%lhs) : (i32) -> i32"), registry);
        var result = Verifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Diagnostics.Count);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "'arith.addi' expects exactly 2 operands but found 1.");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "arith.addi expects exactly two operands.");
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("arith.addi", diagnostic.Operation.Name));
    }

    [Fact]
    public void VerifierReportsStructuralOperandAndResultConstraints()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "arith",
                [
                    new OperationDefinition(
                        "arith.constant",
                        resultDefinitions: [new OperationSegmentDefinition("result")]),
                    new OperationDefinition(
                        "arith.addi",
                        operandDefinitions:
                        [
                            new OperationSegmentDefinition("lhs"),
                            new OperationSegmentDefinition("rhs"),
                        ],
                        resultDefinitions: [new OperationSegmentDefinition("result")]),
                ]));

        var module = Binder.BindModule(
            Parser.ParseModule("\"arith.addi\"(%lhs) : (i32) -> i32"),
            registry);

        var result = Verifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "'arith.addi' expects exactly 2 operands but found 1.");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "'arith.addi' expects exactly 1 result but found 0.");
    }

    [Fact]
    public void VerifierReportsMissingRequiredAttributes()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "arith",
                [
                    new OperationDefinition(
                        "arith.constant",
                        resultDefinitions: [new OperationSegmentDefinition("result")],
                        requiredAttributes: ["value"]),
                ]));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() : () -> i32"),
            registry);

        var result = Verifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Diagnostics);
        Assert.Equal("'arith.constant' requires the 'value' attribute.", result.Diagnostics[0].Message);
    }

    [Fact]
    public void VerifierSupportsVariadicOperandDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "test",
                [
                    new OperationDefinition(
                        "test.concat",
                        operandDefinitions:
                        [
                            new OperationSegmentDefinition("head"),
                            new OperationSegmentDefinition("tail", isVariadic: true),
                        ],
                        resultDefinitions: [new OperationSegmentDefinition("result")]),
                ]));

        var validModule = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.concat\"(%a, %b, %c) : (i32, i32, i32) -> i32"),
            registry);
        var invalidModule = Binder.BindModule(
            Parser.ParseModule("%0 = \"test.concat\"() : () -> i32"),
            registry);

        Assert.True(Verifier.Verify(validModule).IsSuccess);

        var invalidResult = Verifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'test.concat' expects at least 1 operand but found 0.");
    }

    [Fact]
    public void VerifierSupportsRegionAndSuccessorDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "test",
                [
                    new OperationDefinition(
                        "test.branching",
                        regionDefinitions: [new OperationSegmentDefinition("body")],
                        successorDefinitions:
                        [
                            new OperationSegmentDefinition("primary"),
                            new OperationSegmentDefinition("extra", isVariadic: true),
                        ]),
                ]));

        var missingRegionModule = Binder.BindModule(
            Parser.ParseModule("\"test.branching\"() [^bb0] : () -> ()"),
            registry);
        var missingSuccessorModule = Binder.BindModule(
            Parser.ParseModule("\"test.branching\"() {} : () -> ()"),
            registry);

        var missingRegionResult = Verifier.Verify(missingRegionModule);
        var missingSuccessorResult = Verifier.Verify(missingSuccessorModule);

        Assert.Contains(missingRegionResult.Diagnostics, diagnostic => diagnostic.Message == "'test.branching' expects exactly 1 region but found 0.");
        Assert.Contains(missingSuccessorResult.Diagnostics, diagnostic => diagnostic.Message == "'test.branching' expects at least 1 successor but found 0.");
    }

    [Fact]
    public void VerifierSupportsOptionalAndRequiredAttributeDefinitions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "test",
                [
                    new OperationDefinition(
                        "test.attrs",
                        attributeDefinitions:
                        [
                            new OperationAttributeDefinition("required"),
                            new OperationAttributeDefinition("optional", isRequired: false),
                        ]),
                ]));

        var validModule = Binder.BindModule(
            Parser.ParseModule("\"test.attrs\"() {required = 1 : i32} : () -> ()"),
            registry);
        var invalidModule = Binder.BindModule(
            Parser.ParseModule("\"test.attrs\"() {optional = 1 : i32} : () -> ()"),
            registry);

        Assert.True(Verifier.Verify(validModule).IsSuccess);

        var invalidResult = Verifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Single(invalidResult.Diagnostics);
        Assert.Equal("'test.attrs' requires the 'required' attribute.", invalidResult.Diagnostics[0].Message);
    }

    [Fact]
    public void OperationCanCheckForAttributesByName()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var operation = module.Operations[0];

        Assert.True(operation.HasAttribute("value"));
        Assert.False(operation.HasAttribute("fastmath"));
    }

    [Fact]
    public void OperationCanRetrieveAttributesByName()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"));

        var attribute = module.Operations[0].GetAttribute("value");

        Assert.Equal("value", attribute.Name);
        Assert.Equal("0 : i32", attribute.Value.Syntax!.Text);
    }

    [Fact]
    public void OperationViewProvidesTypedWrapperOverSemanticOperation()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(CreateArithConstantDialect());

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"),
            registry);

        var view = new ArithConstantView(Assert.IsType<GeneratedConstantOperation>(module.Operations[0]));

        Assert.Equal("%0", view.Results[0]);
        Assert.Equal("%0", view.ResultValue.Name);
        Assert.Equal("0 : i32", view.ValueAttribute.Value.Syntax!.Text);
    }

    [Fact]
    public void OperationViewRejectsUnexpectedOperationNames()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"func.return\"() : () -> ()"));

        var exception = Assert.Throws<ArgumentException>(() => new ArithConstantView(module.Operations[0]));

        Assert.Contains("arith.constant", exception.Message);
        Assert.Contains("func.return", exception.Message);
    }

    [Fact]
    public void DialectBuilderCreatesDefinitionsFluently()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation =>
                        {
                            operation.Result("result")
                                .RequiredAttribute("value")
                                .WithFactory(static context => new GeneratedConstantOperation(context))
                                .WithVerifier(static (semanticOperation, context) =>
                                {
                                    if (semanticOperation.Results.Count != 1)
                                    {
                                        context.Report("arith.constant should define a single result.");
                                    }
                                });
                        });
                }));

        var validModule = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0 : i32} : () -> i32"),
            registry);
        var invalidModule = Binder.BindModule(
            Parser.ParseModule("\"arith.constant\"() : () -> i32"),
            registry);

        Assert.True(validModule.Operations[0].IsKnown);

        var invalidResult = Verifier.Verify(invalidModule);
        Assert.False(invalidResult.IsSuccess);
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'arith.constant' expects exactly 1 result but found 0.");
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "'arith.constant' requires the 'value' attribute.");
        Assert.Contains(invalidResult.Diagnostics, diagnostic => diagnostic.Message == "arith.constant should define a single result.");
    }

    [Fact]
    public void SemanticPrinterUsesCustomAssemblyFormatsWhenAvailable()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation =>
                        {
                            operation.Result("result")
                                .RequiredAttribute("value")
                                .WithFactory(static context => new GeneratedConstantOperation(context))
                                .WithAssemblyFormat(new PrefixConstantAssemblyFormat());
                        });
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() {value = 0} : () -> i32"),
            registry);

        Assert.Equal("%0 = arith.constant 0 : () -> i32", module.ToText());
    }

    [Fact]
    public void ParserCanUseRegisteredCustomAssemblyFormats()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = Parser.ParseModule("%0 = arith.constant 0 : i32", registry);

        Assert.Single(module.Operations);
        Assert.Equal("arith.constant", module.Operations[0].Name);
        Assert.True(module.Operations[0].HasCustomAssemblyBody);

        var body = Assert.IsType<PrefixConstantBodySyntax>(module.Operations[0].Body);
        Assert.Equal("0", body.Value.Text);
        Assert.Equal("i32", body.TypeSignature.GetRawText().Text);
    }

    [Fact]
    public void DocumentCanParseRegisteredCustomAssemblyFormats()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var document = Document.Parse("%0 = arith.constant 0 : i32", registry);
        var module = Binder.BindModule(document.Module, registry);

        Assert.Equal("%0 = arith.constant 0 : i32", module.ToText());
        Assert.Equal("0", module.Operations[0].GetAttribute("value").Value.Syntax!.Text);
    }

    [Fact]
    public void CustomAssemblyBodiesRoundTripExactlyThroughTheConcreteSyntaxTree()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        const string source = "%0 = arith.constant  0  :  i32\n";

        var module = Parser.ParseModule(source, registry);

        Assert.True(module.Operations[0].HasCustomAssemblyBody);
        Assert.Equal(source, Printer.Print(module));
    }

    [Fact]
    public void AssemblyBindingCanReportDiagnostics()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .RequiredAttribute("value")
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.constant\"() : () -> i32"),
            registry);

        Assert.Single(module.AssemblyDiagnostics);
        Assert.Equal("arith.constant expects a 'value' required attribute.", module.AssemblyDiagnostics[0].Message);
        Assert.True(module.AssemblyDiagnostics[0].Location.IsKnown);
        Assert.Equal(1, module.AssemblyDiagnostics[0].Location.Line);
        Assert.Equal(6, module.AssemblyDiagnostics[0].Location.Column);
    }

    [Fact]
    public void AttributeAndTypeBindingCanReportDiagnostics()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "builtin",
                [],
                [new AttributeDefinition("dense", new DenseAttributeAssemblyFormat(), static context => new DenseAttributeValue(context))],
                [new TypeDefinition("i32", new BuiltinIntegerTypeAssemblyFormat(), static context => new BuiltinIntegerTypeReference(context))]));

        var module = Binder.BindModule(
            Parser.ParseModule("\"test.op\"() {value = #dense<[1, 2]>} : () -> i32"),
            registry);

        Assert.Single(module.AssemblyDiagnostics);
        Assert.Equal("dense attribute literals should mention a tensor type.", module.AssemblyDiagnostics[0].Message);
        Assert.True(module.AssemblyDiagnostics[0].Location.IsKnown);
    }

    [Fact]
    public void SemanticPrinterFallsBackToGenericAssemblyForUnknownOperations()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("\"test.unknown\"(%arg0) : (i32) -> i32"));

        Assert.Equal("\"test.unknown\"(%arg0) : (i32) -> i32", module.ToText());
    }

    [Fact]
    public void SemanticPrinterCanMixCustomAndGenericAssemblyWithinRegions()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            Dialect.Create(
                "arith",
                dialect =>
                {
                    dialect.AddOperation(
                        "arith.constant",
                        operation => operation
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new PrefixConstantAssemblyFormat()));
                }));

        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  %0 = \"arith.constant\"() {value = 0} : () -> i32\n" +
                "  \"func.return\"(%0) : (i32) -> ()\n" +
                "} : (i1) -> ()"),
            registry);

        Assert.Equal(
            "\"scf.if\"(%cond) {\n" +
            "  %0 = arith.constant 0 : () -> i32\n" +
            "  \"func.return\"(%0) : (i32) -> ()\n" +
            "} : (i1) -> ()",
            module.ToText());
    }

    [Fact]
    public void VerifierWalksNestedOperations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(
            new Dialect(
                "func",
                [
                    new OperationDefinition(
                        "func.return",
                        verifier: new DelegateOperationVerifier(
                            static (operation, context) =>
                            {
                                if (operation.Operands.Count == 0)
                                {
                                    context.Report("func.return expects a value in this test dialect.");
                                }
                            })),
                ]));

        var module = Binder.BindModule(
            Parser.ParseModule(
                "\"scf.if\"(%cond) {\n" +
                "  \"func.return\"() : () -> ()\n" +
                "} : (i1) -> ()"),
            registry);

        var result = Verifier.Verify(module);

        Assert.False(result.IsSuccess);
        Assert.Single(result.Diagnostics);
        Assert.Equal("func.return", result.Diagnostics[0].Operation.Name);
        Assert.True(result.Diagnostics[0].Location.IsKnown);
        Assert.Equal(2, result.Diagnostics[0].Location.Line);
        Assert.Equal(3, result.Diagnostics[0].Location.Column);
    }

    [Fact]
    public void SemanticReferencesExposeSourceLocations()
    {
        var module = Binder.BindModule(
            Parser.ParseModule("%0 = \"arith.addi\"(%lhs, %rhs) [^bb1] : (i32, i32) -> i32"));

        var operation = module.Operations[0];

        Assert.Equal(1, operation.Location.Line);
        Assert.Equal(6, operation.Location.Column);
        Assert.Equal(1, operation.ResultValues[0].Location.Line);
        Assert.Equal(1, operation.ResultValues[0].Location.Column);
        Assert.Equal(1, operation.OperandValues[0].Location.Line);
        Assert.Equal(19, operation.OperandValues[0].Location.Column);
        Assert.Equal(1, operation.SuccessorReferences[0].Location.Line);
        Assert.Equal(32, operation.SuccessorReferences[0].Location.Column);
    }

    [Fact]
    public void RegistryRejectsDuplicateOperationRegistrations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("arith", [new OperationDefinition("arith.addi")]));

        var exception = Assert.Throws<ArgumentException>(
            () => registry.RegisterDialect(new Dialect("arithx", [new OperationDefinition("arith.addi")])));

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void RegistryRejectsDuplicateAttributeAndTypeRegistrations()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(new Dialect("builtin", [], [new AttributeDefinition("dense")], [new TypeDefinition("i32")]));

        var attributeException = Assert.Throws<ArgumentException>(
            () => registry.RegisterDialect(new Dialect("builtin_attr", [], [new AttributeDefinition("dense")], [])));
        var typeException = Assert.Throws<ArgumentException>(
            () => registry.RegisterDialect(new Dialect("builtin_type", [], [], [new TypeDefinition("i32")])));

        Assert.Contains("already registered", attributeException.Message);
        Assert.Contains("already registered", typeException.Message);
    }

    [Fact]
    public void SyntheticBlockHasNullSyntaxAndUnknownLocation()
    {
        var syntheticBlock = new Block(null, [], []);

        Assert.Null(syntheticBlock.Syntax);
        Assert.Null(syntheticBlock.Label);
        Assert.Null(syntheticBlock.LabelReference);
        Assert.False(syntheticBlock.Location.IsKnown);
    }

    [Fact]
    public void SyntheticRegionHasNullSyntax()
    {
        var syntheticRegion = new Region(null, []);

        Assert.Null(syntheticRegion.Syntax);
    }

    [Fact]
    public void SyntheticOperationHasNullSyntaxAndUnknownLocation()
    {
        var syntheticOperation = new SyntheticOperation("test.synthetic");

        Assert.Null(syntheticOperation.Syntax);
        Assert.Null(syntheticOperation.SyntaxName);
        Assert.False(syntheticOperation.Location.IsKnown);
        Assert.Equal("test.synthetic", syntheticOperation.Name);
        Assert.Equal("test", syntheticOperation.DialectName);
    }

    [Fact]
    public void SyntheticAttributeValueHasNullSyntax()
    {
        var syntheticAttribute = new SyntheticAttributeValue("test");

        Assert.Null(syntheticAttribute.Syntax);
        Assert.Equal("test", syntheticAttribute.Name);
        Assert.False(syntheticAttribute.Location.IsKnown);
    }
}
