namespace MLIR.Tests;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Dialects.Builtin;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Syntax.Types.Primitives;
using MLIR.Text;
using MLIR.Transforms;
using Xunit;

public sealed partial class SemanticTests
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

    private static ConcreteSyntaxBuilder.ConcreteSyntaxBuilderOptions ReplaceExistingSyntaxOptions()
    {
        return new ConcreteSyntaxBuilder.ConcreteSyntaxBuilderOptions(
            existingSyntaxHandling: ConcreteSyntaxBuilder.ExistingSyntaxHandling.ReplaceExistingSyntax);
    }

    private static DialectRegistry CreateFloatingPointConstantRegistry(AttributeDefinition valueDefinition)
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
                            .Result("result")
                            .WithFactory(static context => new GeneratedConstantOperation(context))
                            .WithAssemblyFormat(new ContextDirectedConstantAssemblyFormat(valueDefinition)));
                }));

        return registry;
    }

    private static GenericOperationBodySyntax GetGenericBody(OperationSyntax operation)
    {
        if (operation.Body is GenericOperationBodySyntax genericBody)
        {
            return genericBody;
        }

        throw new InvalidOperationException("Expected a generic operation body syntax node.");
    }

    private sealed class PrefixConstantBodySyntax : OperationBodySyntax
    {
        private readonly GenericOperationBodySyntax genericBody;

        public PrefixConstantBodySyntax(
            AttributeValueSyntax value,
            Token colonToken,
            TypeSyntax typeSignature,
            DelimitedSyntaxList<NamedAttributeSyntax> attributes)
        {
            Value = value;
            ColonToken = colonToken;
            TypeSignature = typeSignature;
            genericBody = new GenericOperationBodySyntax(
                new DelimitedSyntaxList<Token>(TokenFactory.LParen(), [], [], TokenFactory.RParen()),
                new DelimitedSyntaxList<Token>(null, [], [], null),
                [],
                attributes,
                colonToken,
                typeSignature);
        }

        public AttributeValueSyntax Value { get; }

        public Token ColonToken { get; }

        public TypeSyntax TypeSignature { get; }

        public DelimitedSyntaxList<NamedAttributeSyntax> Attributes => genericBody.Attributes;

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.SuggestTrivia(" ");
            Value.WriteTo(writer);
            writer.WriteToken(ColonToken, " ");
            writer.SuggestTrivia(" ");
            TypeSignature.WriteTo(writer);
        }

        public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
        {
            return new PrefixConstantBodySyntax(
                (AttributeValueSyntax)rewriter.Visit(Value),
                rewriter.VisitToken(ColonToken),
                (TypeSyntax)rewriter.Visit(TypeSignature),
                rewriter.VisitDelimitedList(Attributes));
        }
    }

    private sealed class ArithConstantView
    {
        private readonly Operation operation;

        public ArithConstantView(Operation operation)
        {
            if (operation.Name != "arith.constant")
            {
                throw new ArgumentException(
                    $"Expected operation 'arith.constant' but received '{operation.Name}'.",
                    nameof(operation));
            }

            this.operation = operation;
        }

        public IReadOnlyList<string> Results => operation.Results.Select(static result => result.Name).ToArray();

        public NamedAttribute ValueAttribute => operation.GetAttribute("value");

        public OperationResult ResultValue => operation.Results[0];
    }

    private sealed class GeneratedConstantOperation : Operation
    {
        private readonly OperationDefinition definition;

        public GeneratedConstantOperation(OperationSyntax syntax, OperationDefinition definition, OperationResult resultValue, AttributeValue value, TypeReference typeSignatureReference)
            : base(
                syntax,
                [],
                NamedAttributeCollection.Create(new NamedAttribute("value", value)),
                typeSignatureReference,
                [resultValue],
                [],
                [])
        {
            this.definition = definition;
        }

        public GeneratedConstantOperation(OperationConstructionContext context)
            : base(
                context.Syntax,
                context.Regions,
                context.Attributes,
                context.TypeSignatureReference,
                context.ResultValues,
                context.OperandValues,
                context.Successors)
        {
            definition = context.Definition;
        }

        public override string Name => definition.Name;

        public override OperationDefinition? Definition => definition;

        public NamedAttribute ValueAttribute => GetAttribute("value");

        public OperationResult ResultValue => Results.Single();
    }

    private sealed class GeneratedAddIOperation : Operation
    {
        private readonly OperationDefinition definition;

        public GeneratedAddIOperation(OperationConstructionContext context)
            : base(
                context.Syntax,
                context.Regions,
                context.Attributes,
                context.TypeSignatureReference,
                context.ResultValues,
                context.OperandValues,
                context.Successors)
        {
            definition = context.Definition;
        }

        public override string Name => definition.Name;

        public override OperationDefinition? Definition => definition;

        public Value LeftOperand => Operands[0].Value!;

        public Value RightOperand => Operands[1].Value!;

        public OperationResult ResultValue => Results[0];
    }

    private sealed class SyntheticOperation : Operation
    {
        private readonly string name;

        public SyntheticOperation(string name)
            : base(null)
        {
            this.name = name;
        }

        public override string Name => name;

        public override OperationDefinition? Definition => null;
    }

    private sealed class SyntheticAttributeValue : AttributeValue
    {
        public SyntheticAttributeValue(string name)
            : base(null, SourceLocation.Unknown)
        {
            Name = name;
        }

        public override string? Name { get; }

        public override AttributeConstraintDefinition? Definition => null;
    }

    private sealed class DenseAttributeValue : AttributeValue
    {
        public DenseAttributeValue(AttributeValueConstructionContext context)
            : base(context.Syntax, context.Location)
        {
            Name = context.Name;
            Definition = context.Definition;
        }

        public override string? Name { get; }

        public override AttributeConstraintDefinition? Definition { get; }

        public string? Kind { get; private set; }

        public void BindDense()
        {
            Kind = "dense";
        }
    }

    private sealed class DenseAttributeValueSyntax : AttributeValueSyntax
    {
        public DenseAttributeValueSyntax(
            Token hashToken,
            Token nameToken,
            Token lessThanToken,
            RawSyntaxText payload,
            Token greaterThanToken,
            Token? colonToken = null,
            TypeSyntax? typeSyntax = null)
        {
            HashToken = hashToken;
            NameToken = nameToken;
            LessThanToken = lessThanToken;
            Payload = payload;
            GreaterThanToken = greaterThanToken;
            ColonToken = colonToken;
            TypeSyntax = typeSyntax;
        }

        public Token HashToken { get; }

        public Token NameToken { get; }

        public Token LessThanToken { get; }

        public RawSyntaxText Payload { get; }

        public Token GreaterThanToken { get; }

        public Token? ColonToken { get; }

        public TypeSyntax? TypeSyntax { get; }

        public override SourceLocation Location => HashToken.Location;

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.WriteToken(HashToken);
            writer.WriteToken(NameToken);
            writer.WriteToken(LessThanToken);
            writer.WriteRaw(Payload);
            writer.WriteToken(GreaterThanToken);
            if (ColonToken.HasValue && TypeSyntax != null)
            {
                writer.WriteToken(ColonToken.Value, " ");
                TypeSyntax.WriteTo(writer);
            }
        }

        public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
        {
            return new DenseAttributeValueSyntax(
                rewriter.VisitToken(HashToken),
                rewriter.VisitToken(NameToken),
                rewriter.VisitToken(LessThanToken),
                rewriter.VisitRawText(Payload),
                rewriter.VisitToken(GreaterThanToken),
                rewriter.VisitToken(ColonToken),
                TypeSyntax != null ? (TypeSyntax)rewriter.Visit(TypeSyntax) : null);
        }
    }

    private sealed class I32AttributeValue : AttributeValue
    {
        public I32AttributeValue(AttributeValueConstructionContext context)
            : base(context.Syntax, context.Location)
        {
            Name = context.Name;
            Definition = context.Definition;
        }

        public override string? Name { get; }

        public override AttributeConstraintDefinition? Definition { get; }

        public int? Value { get; private set; }

        public void BindValue(int value)
        {
            Value = value;
        }
    }

    private sealed class TestF32AttributeValue : AttributeValue
    {
        private readonly MLIR.Numerics.ApFloat floatValue;

        public TestF32AttributeValue(AttributeValueConstructionContext context)
            : base(context.Syntax, context.Location)
        {
            floatValue = ((FloatingPointAttributeValueSyntax)context.Syntax).Value;
            Name = context.Name;
            Definition = context.Definition;
        }

        public TestF32AttributeValue(float value)
            : base(null, MLIR.Semantics.SourceLocation.Unknown)
        {
            floatValue = MLIR.Numerics.ApFloat.FromSingle(MLIR.Numerics.FloatSemantics.IEEESingle, value);
        }

        public override string? Name { get; }

        public override AttributeConstraintDefinition? Definition { get; }

        public float Value => floatValue.ToSingle();
    }

    private sealed class TestF64AttributeValue : AttributeValue
    {
        private readonly MLIR.Numerics.ApFloat floatValue;

        public TestF64AttributeValue(AttributeValueConstructionContext context)
            : base(context.Syntax, context.Location)
        {
            floatValue = ((FloatingPointAttributeValueSyntax)context.Syntax).Value;
            Name = context.Name;
            Definition = context.Definition;
        }

        public TestF64AttributeValue(double value)
            : base(null, MLIR.Semantics.SourceLocation.Unknown)
        {
            floatValue = MLIR.Numerics.ApFloat.FromDouble(MLIR.Numerics.FloatSemantics.IEEEDouble, value);
        }

        public override string? Name { get; }

        public override AttributeConstraintDefinition? Definition { get; }

        public double Value => floatValue.ToDouble();
    }

    private sealed class BuiltinIntegerTypeSyntax : TypeSyntax
    {
        public BuiltinIntegerTypeSyntax(Token nameToken)
        {
            NameToken = nameToken;
        }

        public Token NameToken { get; }

        public override SourceLocation Location => NameToken.Location;

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.WriteToken(NameToken);
        }

        public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
        {
            return new BuiltinIntegerTypeSyntax(rewriter.VisitToken(NameToken));
        }
    }

    private sealed class IntegerLiteralAttributeSyntax : AttributeValueSyntax
    {
        public IntegerLiteralAttributeSyntax(Token literalToken)
        {
            LiteralToken = literalToken;
        }

        public Token LiteralToken { get; }

        public override SourceLocation Location => LiteralToken.Location;

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.WriteToken(LiteralToken);
        }

        public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
        {
            return new IntegerLiteralAttributeSyntax(rewriter.VisitToken(LiteralToken));
        }
    }

    private sealed class PrefixConstantAssemblyFormat : IOperationAssemblyFormat
    {
        public ParseResult<OperationBodySyntax> TryParse(
            Token nameToken,
            SeparatedSyntaxList<Token> resultList,
            Token? equalsToken,
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
            var valueAttrSyntax = new RawAttributeValueSyntax(valueResult.Value);

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

            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(TokenFactory.Identifier("value"), TokenFactory.Equal(), valueAttrSyntax)]);

            return ParseResult<OperationBodySyntax>.Success(new PrefixConstantBodySyntax(valueAttrSyntax, colonTokenResult.Value, new RawTypeSyntax(typeResult.Value), attributes));
        }

        public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
        {
            var body = (PrefixConstantBodySyntax)syntax.Body;
            return new GeneratedConstantOperation(
                syntax,
                definition,
                new OperationResult(syntax.ResultList.Single()),
                binder.BindAttributeValue(body.Value),
                binder.BindTypeReference(body.TypeSignature));
        }

        public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
        {
            var genericBody = context.TransformGenericBody(operation);
            var valueAttr = operation.Attributes.FirstOrDefault(a => a.Name == "value");
            var body = new PrefixConstantBodySyntax(
                valueAttr != null ? context.BuildAttributeValueSyntax(valueAttr.Value) : new RawAttributeValueSyntax(new RawSyntaxText(string.Empty)),
                genericBody.TypeSignatureColonToken ?? TokenFactory.Colon(),
                genericBody.TypeSignatureSyntax ?? throw new InvalidOperationException("Expected a type signature in the generic body for rewriting."),
                genericBody.Attributes);
            var sourceNameToken = operation.Syntax!.NameToken;
            var rewrittenNameToken = sourceNameToken.WithText(operation.Name);
            return context.RewriteOperation(operation, body, rewrittenNameToken);
        }
    }

    private sealed class ContextDirectedConstantAssemblyFormat : IOperationAssemblyFormat
    {
        private readonly AttributeConstraintDefinition expectedAttributeDefinition;

        public ContextDirectedConstantAssemblyFormat(AttributeConstraintDefinition expectedAttributeDefinition)
        {
            this.expectedAttributeDefinition = expectedAttributeDefinition;
        }

        public ParseResult<OperationBodySyntax> TryParse(
            Token nameToken,
            SeparatedSyntaxList<Token> resultList,
            Token? equalsToken,
            OperationParsingContext context)
        {
            if (context.Is(TokenKind.LParen))
            {
                return ParseResult<OperationBodySyntax>.NoMatch();
            }

            var valueResult = context.TryParseAttributeValueSyntax(expectedAttributeDefinition, TokenKind.Colon);
            if (!valueResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(valueResult.Diagnostic!);
            }

            var colonTokenResult = context.Expect(TokenKind.Colon, "Expected ':' after the custom constant value.");
            if (!colonTokenResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(colonTokenResult.Diagnostic!);
            }

            var typeResult = context.TryParseTypeSyntax();
            if (!typeResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(typeResult.Diagnostic!);
            }

            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(TokenFactory.Identifier("value"), TokenFactory.Equal(), valueResult.Value)]);

            return ParseResult<OperationBodySyntax>.Success(new PrefixConstantBodySyntax(valueResult.Value, colonTokenResult.Value, typeResult.Value, attributes));
        }

        public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
        {
            var body = (PrefixConstantBodySyntax)syntax.Body;
            return new GeneratedConstantOperation(
                syntax,
                definition,
                new OperationResult(syntax.ResultList.Single()),
                binder.BindAttributeValue(body.Attributes[0].ValueSyntax, expectedAttributeDefinition),
                binder.BindTypeReference(body.TypeSignature));
        }

        public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
        {
            var genericBody = context.TransformGenericBody(operation);
            var valueAttr = operation.Attributes.FirstOrDefault(a => a.Name == "value");
            var attrSyntax = context.BuildAttributeValueSyntax(valueAttr?.Value ?? new UnknownAttributeValue(new RawAttributeValueSyntax(new RawSyntaxText(string.Empty)), null, null, SourceLocation.Unknown));
            var body = new PrefixConstantBodySyntax(
                attrSyntax,
                genericBody.TypeSignatureColonToken ?? TokenFactory.Colon(),
                genericBody.TypeSignatureSyntax ?? throw new InvalidOperationException("Expected a type signature in the generic body for rewriting."),
                genericBody.Attributes);
            var sourceNameToken = operation.Syntax!.NameToken;
            var rewrittenNameToken = sourceNameToken.WithText(operation.Name);
            return context.RewriteOperation(operation, body, rewrittenNameToken);
        }
    }

    private sealed class DenseAttributeAssemblyFormat : IAttributeAssemblyFormat
    {
        public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
        {
            if (!context.TryMatch(TokenKind.Hash, out var hashToken))
            {
                return ParseResult<AttributeValueSyntax>.NoMatch();
            }

            if (!(context.Is(TokenKind.Identifier) && context.TryMatch(TokenKind.Identifier, out var nameToken) && nameToken.Text == "dense"))
            {
                return ParseResult<AttributeValueSyntax>.NoMatch();
            }

            var lessThanTokenResult = context.Expect(TokenKind.LessThan, "Expected '<' after '#dense'.");
            if (!lessThanTokenResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(lessThanTokenResult.Diagnostic!);
            }

            var payloadResult = context.TryParseRawUntilDelimiter(TokenKind.GreaterThan);
            if (!payloadResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(payloadResult.Diagnostic!);
            }

            var greaterThanTokenResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the dense attribute.");
            if (!greaterThanTokenResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(greaterThanTokenResult.Diagnostic!);
            }

            Token? colonToken = null;
            TypeSyntax? typeSyntax = null;
            if (context.Is(TokenKind.Colon))
            {
                var colonTokenResult = context.Expect(TokenKind.Colon, "Expected ':' before the dense attribute type.");
                if (!colonTokenResult.IsSuccess)
                {
                    return ParseResult<AttributeValueSyntax>.Failure(colonTokenResult.Diagnostic!);
                }

                var typeSyntaxResult = context.TryParseRawUntilDelimiter(TokenKind.Comma, TokenKind.RBrace);
                if (!typeSyntaxResult.IsSuccess)
                {
                    return ParseResult<AttributeValueSyntax>.Failure(typeSyntaxResult.Diagnostic!);
                }

                colonToken = colonTokenResult.Value;
                typeSyntax = new RawTypeSyntax(typeSyntaxResult.Value);
            }

            return ParseResult<AttributeValueSyntax>.Success(new DenseAttributeValueSyntax(
                hashToken,
                nameToken,
                lessThanTokenResult.Value,
                payloadResult.Value,
                greaterThanTokenResult.Value,
                colonToken,
                typeSyntax));
        }

        public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
        {
            var denseAttribute = new DenseAttributeValue(new AttributeValueConstructionContext(syntax, "dense", definition, syntax.Location));
            denseAttribute.BindDense();
            if (!syntax.ToString().Contains("tensor<"))
            {
                binder.Report(new AssemblyDiagnostic(syntax.Location, "dense attribute literals should mention a tensor type."));
            }

            return denseAttribute;
        }

        public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
        {
            return attribute.Syntax ?? throw new InvalidOperationException("Dense attributes require syntax to rebuild their assembly form.");
        }
    }

    private sealed class BuiltinIntegerTypeAssemblyFormat : ITypeAssemblyFormat
    {
        public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
        {
            if (!context.TryMatch(TokenKind.Identifier, out var nameToken) ||
                !BuiltinIntegerTypeName.TryParse(nameToken.Text, out var signedness, out var width))
            {
                return ParseResult<TypeSyntax>.NoMatch();
            }

            return ParseResult<TypeSyntax>.Success(new global::MLIR.Syntax.Types.Primitives.BuiltinIntegerTypeSyntax(nameToken, signedness switch
            {
                BuiltinIntegerTypeName.Kind.Signed => IntegerTypeSignedness.Signed,
                BuiltinIntegerTypeName.Kind.Unsigned => IntegerTypeSignedness.Unsigned,
                _ => IntegerTypeSignedness.Signless,
            }, width));
        }

        public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)
        {
            if (syntax is not global::MLIR.Syntax.Types.Primitives.BuiltinIntegerTypeSyntax integerSyntax)
            {
                return new IntegerType(0, IntegerTypeSignedness.Signless, syntax);
            }

            return new IntegerType(integerSyntax.Width, integerSyntax.Signedness, syntax);
        }

        public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
        {
            if (type is IntegerType integerType)
            {
                return new global::MLIR.Syntax.Types.Primitives.BuiltinIntegerTypeSyntax(
                    TokenFactory.Identifier(BuiltinIntegerTypeName.Format(integerType.Width, integerType.Signedness switch
                    {
                        IntegerTypeSignedness.Signed => BuiltinIntegerTypeName.Kind.Signed,
                        IntegerTypeSignedness.Unsigned => BuiltinIntegerTypeName.Kind.Unsigned,
                        _ => BuiltinIntegerTypeName.Kind.Signless,
                    })),
                    integerType.Signedness,
                    integerType.Width);
            }

            return type.Syntax ?? throw new InvalidOperationException("Integer test types require syntax to rebuild their assembly form.");
        }
    }

    private sealed class I32AttributeAssemblyFormat : IAttributeAssemblyFormat
    {
        public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
        {
            if (!context.TryMatch(TokenKind.Integer, out var literalToken))
            {
                return ParseResult<AttributeValueSyntax>.NoMatch();
            }

            return ParseResult<AttributeValueSyntax>.Success(new IntegerLiteralAttributeSyntax(literalToken));
        }

        public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
        {
            var attribute = new I32AttributeValue(new AttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));
            attribute.BindValue(int.Parse(syntax.ToString()));
            return attribute;
        }

        public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
        {
            if (attribute is I32AttributeValue i32 && i32.Value.HasValue)
            {
                return new IntegerLiteralAttributeSyntax(TokenFactory.Integer(i32.Value.Value.ToString()));
            }

            return attribute.Syntax ?? throw new InvalidOperationException("i32 attributes require syntax to rebuild their assembly form.");
        }
    }

    private sealed class TestF32AttributeAssemblyFormat : IAttributeAssemblyFormat
    {
        private readonly FloatingPointLiteralAttributeAssemblyFormat singlePrecisionFormat = new(FloatSemantics.IEEESingle);

        public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
        {
            return singlePrecisionFormat.TryParse(context);
        }

        public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
        {
            return new TestF32AttributeValue(
                binder.CreateAttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));
        }

        public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
        {
            return singlePrecisionFormat.BuildCustomAssemblySyntax(attribute, context);
        }
    }

    private sealed class TestF64AttributeAssemblyFormat : IAttributeAssemblyFormat
    {
        private readonly FloatingPointLiteralAttributeAssemblyFormat doublePrecisionFormat = new(FloatSemantics.IEEEDouble);

        public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
        {
            return doublePrecisionFormat.TryParse(context);
        }

        public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
        {
            return new TestF64AttributeValue(
                binder.CreateAttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));
        }

        public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
        {
            return doublePrecisionFormat.BuildCustomAssemblySyntax(attribute, context);
        }
    }

    private sealed class CapturingIntegerAttributeAssemblyFormat : IAttributeAssemblyFormat
    {
        private readonly Action<AttributeValueConstructionContext> onContextBound;
        private readonly IntegerLiteralAttributeAssemblyFormat integerLiteralFormat = new();

        public CapturingIntegerAttributeAssemblyFormat(Action<AttributeValueConstructionContext> onContextBound)
        {
            this.onContextBound = onContextBound;
        }

        public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
        {
            return integerLiteralFormat.TryParse(context);
        }

        public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
        {
            var constructionContext = binder.CreateAttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location);
            onContextBound(constructionContext);
            return new UnknownAttributeValue(constructionContext.Syntax, constructionContext.Name, constructionContext.Definition, constructionContext.Location);
        }

        public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
        {
            return integerLiteralFormat.BuildCustomAssemblySyntax(attribute, context);
        }
    }
}
