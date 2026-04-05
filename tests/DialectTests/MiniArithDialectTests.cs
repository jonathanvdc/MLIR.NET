namespace DialectTests;

using System.Numerics;
using MLIR;
using MLIR.Miniarith;
using MLIR.Semantics;
using MLIR.Semantics.Attributes;
using MLIR.Semantics.Attributes.Collections;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using Xunit;

public sealed class MiniArithDialectTests : DialectIntegrationTestBase
{
    [Fact]
    public void RegistrationExposesTypedDefinitions()
    {
        var dialect = MiniarithDialectRegistration.Create();
        var registry = CreateMiniArithRegistry();

        Assert.Equal("miniarith", dialect.Name);
        Assert.True(registry.TryGetOperation("miniarith.addi", out var operationDefinition));
        Assert.NotNull(operationDefinition);
        Assert.NotNull(operationDefinition!.AssemblyFormat);
    }

    [Fact]
    public void BindingGenericSyntaxProducesTypedOperation()
    {
        var module = Document.Parse("%result = \"miniarith.addi\"(%lhs, %rhs) : i32").Bind(CreateMiniArithRegistry());

        var operation = Assert.IsType<MiniArith_AddIOp>(Assert.Single(module.Operations));
        Assert.Equal("miniarith.addi", operation.Name);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
        Assert.NotNull(operation.TypeSignatureReference);
    }

    [Fact]
    public void GeneratedTypesAreUsableFromNormalRuntimeCode()
    {
        var dialect = MiniarithDialectRegistration.Create();

        Assert.Equal("miniarith", dialect.Name);
        Assert.Equal("MiniArith_AddIOp", typeof(MiniArith_AddIOp).Name);
        Assert.Equal("MiniArith_ConstantOp", typeof(MiniArith_ConstantOp).Name);
    }

    [Fact]
    public void DocumentsRoundTripWithoutBinding()
    {
        const string source = "%result = \"miniarith.addi\"(%lhs, %rhs) : i32";

        Assert.Equal(source, Document.Parse(source).ToText());
    }

    [Fact]
    public void BoundModulesRoundTrip()
    {
        const string source = "%result = \"miniarith.addi\"(%lhs, %rhs) : i32";

        Assert.Equal(source, Document.Parse(source).Bind(CreateMiniArithRegistry()).ToText());
    }

    [Fact]
    public void GeneratedAssemblyFormatBindProducesTypedAddIOpFromCustomBodySyntax()
    {
        var body = new MiniArith_AddIOpBodySyntax(
            new SyntaxToken("%lhs"),
            new SyntaxToken(","),
            new SyntaxToken("%rhs"),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null),
            new SyntaxToken(":"),
            new RawTypeSyntax(new RawSyntaxText("i32")));

        var syntax = new OperationSyntax(
            resultList: new SeparatedSyntaxList<SyntaxToken>([new SyntaxToken("%result")], []),
            equalsToken: new SyntaxToken("="),
            nameToken: new SyntaxToken("miniarith.addi"),
            body: body);

        var module = Binder.BindModule(new ModuleSyntax([syntax]), CreateMiniArithRegistry());

        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniArith_AddIOp>(Assert.Single(module.Operations));
        Assert.Equal("miniarith.addi", operation.Name);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
        Assert.NotNull(operation.TypeSignatureReference);
    }

    [Fact]
    public void GeneratedAssemblyFormatBindProducesTypedConstantOpFromCustomBodySyntax()
    {
        var body = new MiniArith_ConstantOpBodySyntax(
            new RawAttributeValueSyntax(new RawSyntaxText("42")),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null));

        var syntax = new OperationSyntax(
            resultList: new SeparatedSyntaxList<SyntaxToken>([new SyntaxToken("%result")], []),
            equalsToken: new SyntaxToken("="),
            nameToken: new SyntaxToken("miniarith.constant"),
            body: body);

        var module = Binder.BindModule(new ModuleSyntax([syntax]), CreateMiniArithRegistry());

        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniArith_ConstantOp>(Assert.Single(module.Operations));
        Assert.Equal("miniarith.constant", operation.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
        Assert.Equal((BigInteger)42, operation.Value);
        Assert.Null(operation.TypeSignatureReference);
    }

    [Fact]
    public void GeneratedAssemblyFormatBindReportsDiagnosticForWrongBodyType()
    {
        var body = new MiniArith_ConstantOpBodySyntax(
            new RawAttributeValueSyntax(new RawSyntaxText("42")),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null));

        var syntax = new OperationSyntax(
            resultList: new SeparatedSyntaxList<SyntaxToken>([new SyntaxToken("%result")], []),
            equalsToken: new SyntaxToken("="),
            nameToken: new SyntaxToken("miniarith.addi"),
            body: body);

        var module = Binder.BindModule(new ModuleSyntax([syntax]), CreateMiniArithRegistry());

        Assert.Single(module.AssemblyDiagnostics);
        Assert.IsType<UninterpretedOperation>(Assert.Single(module.Operations));
    }

    [Fact]
    public void GeneratedAssemblyFormatBindReportsDiagnosticForWrongResultCount()
    {
        var body = new MiniArith_AddIOpBodySyntax(
            new SyntaxToken("%lhs"),
            new SyntaxToken(","),
            new SyntaxToken("%rhs"),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null),
            new SyntaxToken(":"),
            new RawTypeSyntax(new RawSyntaxText("i32")));

        var syntax = new OperationSyntax(
            resultList: SeparatedSyntaxList<SyntaxToken>.Empty,
            equalsToken: null,
            nameToken: new SyntaxToken("miniarith.addi"),
            body: body);

        var module = Binder.BindModule(new ModuleSyntax([syntax]), CreateMiniArithRegistry());

        Assert.Single(module.AssemblyDiagnostics);
        Assert.IsType<UninterpretedOperation>(Assert.Single(module.Operations));
    }

    [Fact]
    public void ParsesIntegerImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddImmediateOp>(
            "%result = miniarith.add_immediate 1, %lhs : i32",
            CreateMiniArithRegistry());

        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal((BigInteger)1, operation.Value);
    }

    [Fact]
    public void ParsesBooleanImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddBoolImmediateOp>(
            "%result = miniarith.add_bool_immediate true, %lhs : i32",
            CreateMiniArithRegistry());

        Assert.True(operation.Value);
    }

    [Fact]
    public void ParsesFloatingPointImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddFloatImmediateOp>(
            "%result = miniarith.add_float_immediate 1.5, %lhs : i32",
            CreateMiniArithRegistry());

        Assert.Equal(1.5f, operation.Value);
    }

    [Fact]
    public void ParsesStringImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddStringImmediateOp>(
            "%result = miniarith.add_string_immediate \"hi\", %lhs : i32",
            CreateMiniArithRegistry());

        Assert.Equal("hi", operation.Value);
    }

    [Fact]
    public void ParsesTypeImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddTypeImmediateOp>(
            "%result = miniarith.add_type_immediate i32, %lhs : i32",
            CreateMiniArithRegistry());

        Assert.Equal("i32", operation.Value.TypeSyntax.ToString());
        Assert.IsType<TypeAttributeValueSyntax>(operation.Value.Syntax);
    }

    [Theory]
    [InlineData("%result = miniarith.add_unit_immediate keyword %lhs : i32", true)]
    [InlineData("%result = miniarith.add_unit_immediate %lhs : i32", false)]
    public void ParsesUnitImmediateBeforeOperand(string source, bool expectedValue)
    {
        var operation = BindSingleOperation<MiniArith_AddUnitImmediateOp>(source, CreateMiniArithRegistry());

        Assert.Equal(expectedValue, operation.Value);
    }

    [Fact]
    public void ParsesDenseArrayImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddArrayImmediateOp>(
            "%result = miniarith.add_array_immediate array<i32: 1, 2>, %lhs : i32",
            CreateMiniArithRegistry());

        var value = operation.Value;
        Assert.IsType<DenseArrayAttributeValueSyntax>(operation.Attributes["value"].Value.Syntax);
        Assert.Equal(2, value.Count);
        Assert.Equal((BigInteger)1, value[0]);
        Assert.Equal((BigInteger)2, value[1]);
    }

    [Fact]
    public void ParsesElementsImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddElementsImmediateOp>(
            "%result = miniarith.add_elements_immediate dense<[1, 2]> : tensor<2xi32>, %lhs : i32",
            CreateMiniArithRegistry());

        var value = operation.Value;
        Assert.IsType<ElementsAttributeValueSyntax>(value.Syntax);
        var payload = Assert.IsAssignableFrom<ArrayAttributeValue>(value.Payload);
        var first = Assert.IsAssignableFrom<IntegerAttributeValue>(payload.Items[0]);
        var second = Assert.IsAssignableFrom<IntegerAttributeValue>(payload.Items[1]);
        Assert.Equal((BigInteger)1, first.Value);
        Assert.Equal((BigInteger)2, second.Value);
        Assert.Equal("tensor<2xi32>", value.TypeSyntax.ToString());
    }

    [Fact]
    public void ParsesDictionaryImmediateBeforeOperand()
    {
        var operation = BindSingleOperation<MiniArith_AddDictionaryImmediateOp>(
            "%result = miniarith.add_dictionary_immediate {inner = 1, nested = {flag = true}}, %lhs : i32",
            CreateMiniArithRegistry());

        var value = operation.Value;
        Assert.IsType<DictionaryAttributeValueSyntax>(value.Syntax);
        var inner = Assert.IsAssignableFrom<IntegerAttributeValue>(value.Attributes["inner"].Value);
        var nested = Assert.IsAssignableFrom<DictionaryAttributeValue>(value.Attributes["nested"].Value);
        var flag = Assert.IsAssignableFrom<BooleanAttributeValue>(nested.Attributes["flag"].Value);
        Assert.Equal((BigInteger)1, inner.Value);
        Assert.True(flag.Value);
    }
}
