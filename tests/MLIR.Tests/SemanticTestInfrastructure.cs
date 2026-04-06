namespace MLIR.Tests;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
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

        public AttributeValueSyntax Value { get; }

        public SyntaxToken ColonToken { get; }

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
            SyntaxToken hashToken,
            SyntaxToken nameToken,
            SyntaxToken lessThanToken,
            RawSyntaxText payload,
            SyntaxToken greaterThanToken,
            SyntaxToken? colonToken = null,
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

        public SyntaxToken HashToken { get; }

        public SyntaxToken NameToken { get; }

        public SyntaxToken LessThanToken { get; }

        public RawSyntaxText Payload { get; }

        public SyntaxToken GreaterThanToken { get; }

        public SyntaxToken? ColonToken { get; }

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
    }

    private sealed class IntegerTypeReference : global::MLIR.Semantics.Types.Primitives.IntegerTypeReference
    {
        public IntegerTypeReference(TypeReferenceConstructionContext context)
            : base(
                global::MLIR.Syntax.Types.Primitives.IntegerTypeSignedness.Signless,
                int.Parse(context.Name![1..]),
                context.Syntax,
                context.Location)
        {
            Definition = context.Definition;
        }

        public override TypeDefinition? Definition { get; }
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

    private sealed class TestF32AttributeValue : MLIR.Semantics.Attributes.Primitives.F32AttributeValue
    {
        public TestF32AttributeValue(AttributeValueConstructionContext context)
            : base(context, MLIR.Semantics.Attributes.Primitives.FloatingPointLiteralParser.ParseSingle(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText))
        {
            Name = context.Name;
            Definition = context.Definition;
        }

        public TestF32AttributeValue(float value)
            : base(value)
        {
        }

        public override string? Name { get; }

        public override AttributeConstraintDefinition? Definition { get; }
    }

    private sealed class TestF64AttributeValue : MLIR.Semantics.Attributes.Primitives.F64AttributeValue
    {
        public TestF64AttributeValue(AttributeValueConstructionContext context)
            : base(context, MLIR.Semantics.Attributes.Primitives.FloatingPointLiteralParser.ParseDouble(((FloatingPointAttributeValueSyntax)context.Syntax).LiteralText))
        {
            Name = context.Name;
            Definition = context.Definition;
        }

        public TestF64AttributeValue(double value)
            : base(value)
        {
        }

        public override string? Name { get; }

        public override AttributeConstraintDefinition? Definition { get; }
    }

    private sealed class BuiltinIntegerTypeSyntax : TypeSyntax
    {
        private readonly RawSyntaxText rawText;

        public BuiltinIntegerTypeSyntax(SyntaxToken nameToken)
        {
            NameToken = nameToken;
            rawText = new RawSyntaxText([nameToken]);
        }

        public SyntaxToken NameToken { get; }

        public override SourceLocation Location => NameToken.Location;

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.WriteToken(NameToken);
        }
    }

    private sealed class IntegerLiteralAttributeSyntax : AttributeValueSyntax
    {
        private readonly RawSyntaxText rawText;

        public IntegerLiteralAttributeSyntax(SyntaxToken literalToken)
        {
            LiteralToken = literalToken;
            rawText = new RawSyntaxText([literalToken]);
        }

        public SyntaxToken LiteralToken { get; }

        public override SourceLocation Location => LiteralToken.Location;

        public override void WriteTo(SyntaxWriter writer)
        {
            writer.WriteToken(LiteralToken);
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

            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(new SyntaxToken("value"), new SyntaxToken("="), valueAttrSyntax)]);

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
                genericBody.TypeSignatureColonToken ?? new SyntaxToken(":"),
                genericBody.TypeSignatureSyntax ?? throw new InvalidOperationException("Expected a type signature in the generic body for rewriting."),
                genericBody.Attributes);
            var sourceNameToken = operation.Syntax!.NameToken;
            var rewrittenNameToken = new SyntaxToken(operation.Name, sourceNameToken.LeadingTrivia, sourceNameToken.Line, sourceNameToken.Column);
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
            SyntaxToken nameToken,
            SeparatedSyntaxList<SyntaxToken> resultList,
            SyntaxToken? equalsToken,
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

            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(new SyntaxToken("value"), new SyntaxToken("="), valueResult.Value)]);

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

            SyntaxToken? colonToken = null;
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
            if (!context.TryMatch(TokenKind.Identifier, out var nameToken) || !nameToken.Text.StartsWith("i"))
            {
                return ParseResult<TypeSyntax>.NoMatch();
            }

            if (!int.TryParse(nameToken.Text[1..], out _))
            {
                return ParseResult<TypeSyntax>.NoMatch();
            }

            return ParseResult<TypeSyntax>.Success(new BuiltinIntegerTypeSyntax(nameToken));
        }

        public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)
        {
            return new IntegerTypeReference(new TypeReferenceConstructionContext(syntax, syntax.ToString(), definition, syntax.Location));
        }

        public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
        {
            if (type is IntegerTypeReference integerType)
            {
                return new BuiltinIntegerTypeSyntax(new SyntaxToken("i" + integerType.Width));
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
                return new IntegerLiteralAttributeSyntax(new SyntaxToken(i32.Value.Value.ToString()));
            }

            return attribute.Syntax ?? throw new InvalidOperationException("i32 attributes require syntax to rebuild their assembly form.");
        }
    }
}
