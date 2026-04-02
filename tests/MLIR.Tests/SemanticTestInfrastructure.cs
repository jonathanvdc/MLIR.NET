namespace MLIR.Tests;

using System.Collections.Generic;
using System.Linq;
using MLIR;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Syntax;
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

        public DelimitedSyntaxList<NamedAttributeSyntax> Attributes => genericBody.Attributes;

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
        private readonly RawSyntaxText rawText;

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

            var tokens = new List<SyntaxToken> { hashToken, nameToken, lessThanToken };
            tokens.AddRange(payload.Tokens);
            tokens.Add(greaterThanToken);
            if (colonToken.HasValue)
            {
                tokens.Add(colonToken.Value);
            }

            if (typeSyntax != null && typeSyntax.TryGetRawText(out var rawType))
            {
                tokens.AddRange(rawType!.Tokens);
            }

            rawText = new RawSyntaxText(tokens);
        }

        public SyntaxToken HashToken { get; }

        public SyntaxToken NameToken { get; }

        public SyntaxToken LessThanToken { get; }

        public RawSyntaxText Payload { get; }

        public SyntaxToken GreaterThanToken { get; }

        public SyntaxToken? ColonToken { get; }

        public TypeSyntax? TypeSyntax { get; }

        public override bool TryGetRawText(out RawSyntaxText? rawText)
        {
            rawText = this.rawText;
            return true;
        }

        public override void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
        {
            writer.WriteRaw(rawText, defaultLeadingTrivia);
        }
    }

    private sealed class BuiltinIntegerTypeReference : TypeReference
    {
        public BuiltinIntegerTypeReference(TypeReferenceConstructionContext context)
            : base(context.Syntax, context.Location)
        {
            Name = context.Name;
            Definition = context.Definition;
        }

        public override string? Name { get; }

        public override TypeDefinition? Definition { get; }

        public int? Width { get; private set; }

        public void BindWidth(int width)
        {
            Width = width;
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

    private sealed class BuiltinIntegerTypeSyntax : TypeSyntax
    {
        private readonly RawSyntaxText rawText;

        public BuiltinIntegerTypeSyntax(SyntaxToken nameToken)
        {
            NameToken = nameToken;
            rawText = new RawSyntaxText([nameToken]);
        }

        public SyntaxToken NameToken { get; }

        public override bool TryGetRawText(out RawSyntaxText? rawText)
        {
            rawText = this.rawText;
            return true;
        }

        public override void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
        {
            writer.WriteToken(NameToken, defaultLeadingTrivia);
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

        public override bool TryGetRawText(out RawSyntaxText? rawText)
        {
            rawText = this.rawText;
            return true;
        }

        public override void WriteTo(SyntaxWriter writer, string defaultLeadingTrivia)
        {
            writer.WriteToken(LiteralToken, defaultLeadingTrivia);
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
                new OperationResult(syntax.ResultTokens.Single()),
                binder.BindAttributeValue(body.Value),
                binder.BindTypeReference(body.TypeSignature));
        }

        public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
        {
            var genericBody = context.TransformGenericBody(operation);
            var valueAttr = operation.Attributes.FirstOrDefault(a => a.Name == "value");
            var body = new PrefixConstantBodySyntax(
                valueAttr != null && valueAttr.Value.Syntax != null ? valueAttr.Value.Syntax.GetRawText() : new RawSyntaxText(string.Empty),
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

            var value = context.ParseAttributeValueSyntax(expectedAttributeDefinition, TokenKind.Colon);
            var colonToken = context.Expect(TokenKind.Colon, "Expected ':' after the custom constant value.");
            var type = context.ParseTypeSyntax();
            var attributes = context.CreateAttributeDictionary([new NamedAttributeSyntax(new SyntaxToken("value"), new SyntaxToken("="), value)]);

            body = new PrefixConstantBodySyntax(value.GetRawText(), colonToken, type, attributes);
            return true;
        }

        public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
        {
            var body = (PrefixConstantBodySyntax)syntax.Body;
            return new GeneratedConstantOperation(
                syntax,
                definition,
                new OperationResult(syntax.ResultTokens.Single()),
                binder.BindAttributeValue(body.Attributes[0].ValueSyntax, expectedAttributeDefinition),
                binder.BindTypeReference(body.TypeSignature));
        }

        public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
        {
            var genericBody = context.TransformGenericBody(operation);
            var valueAttr = operation.Attributes.FirstOrDefault(a => a.Name == "value");
            var body = new PrefixConstantBodySyntax(
                valueAttr != null ? context.BuildAttributeValueSyntax(valueAttr.Value).GetRawText() : new RawSyntaxText(string.Empty),
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
        public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
        {
            syntax = null;
            if (!context.TryMatch(TokenKind.Hash, out var hashToken))
            {
                return false;
            }

            if (!(context.Is(TokenKind.Identifier) && context.TryMatch(TokenKind.Identifier, out var nameToken) && nameToken.Text == "dense"))
            {
                return false;
            }

            var lessThanToken = context.Expect(TokenKind.LessThan, "Expected '<' after '#dense'.");
            var payload = context.ParseRawUntilDelimiter(TokenKind.GreaterThan);
            var greaterThanToken = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the dense attribute.");
            SyntaxToken? colonToken = null;
            TypeSyntax? typeSyntax = null;
            if (context.Is(TokenKind.Colon))
            {
                colonToken = context.Expect(TokenKind.Colon, "Expected ':' before the dense attribute type.");
                typeSyntax = new RawTypeSyntax(context.ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.RBrace));
            }

            syntax = new DenseAttributeValueSyntax(hashToken, nameToken, lessThanToken, payload, greaterThanToken, colonToken, typeSyntax);
            return true;
        }

        public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
        {
            var denseAttribute = new DenseAttributeValue(new AttributeValueConstructionContext(syntax, "dense", definition, syntax.Location));
            denseAttribute.BindDense();
            if (!syntax.GetRawText().Text.Contains("tensor<"))
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
        public bool TryParse(TypeParsingContext context, out TypeSyntax? syntax)
        {
            syntax = null;
            if (!context.TryMatch(TokenKind.Identifier, out var nameToken) || !nameToken.Text.StartsWith("i"))
            {
                return false;
            }

            if (!int.TryParse(nameToken.Text[1..], out _))
            {
                return false;
            }

            syntax = new BuiltinIntegerTypeSyntax(nameToken);
            return true;
        }

        public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)
        {
            var integerType = new BuiltinIntegerTypeReference(new TypeReferenceConstructionContext(syntax, syntax.GetRawText().Text, definition, syntax.Location));
            integerType.BindWidth(int.Parse(integerType.Name![1..]));
            return integerType;
        }

        public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
        {
            if (type is BuiltinIntegerTypeReference integerType && integerType.Width.HasValue)
            {
                return new BuiltinIntegerTypeSyntax(new SyntaxToken("i" + integerType.Width.Value));
            }

            return type.Syntax;
        }
    }

    private sealed class I32AttributeAssemblyFormat : IAttributeAssemblyFormat
    {
        public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
        {
            syntax = null;
            if (!context.TryMatch(TokenKind.Integer, out var literalToken))
            {
                return false;
            }

            syntax = new IntegerLiteralAttributeSyntax(literalToken);
            return true;
        }

        public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
        {
            var attribute = new I32AttributeValue(new AttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));
            attribute.BindValue(int.Parse(syntax.GetRawText().Text));
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
