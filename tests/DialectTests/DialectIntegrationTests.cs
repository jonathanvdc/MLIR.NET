namespace DialectTests;

using MLIR;
using MLIR.Dialects;
using MLIR.Miniarith;
using MLIR.Minienum;
using MLIR.Minitest;
using MLIR.Semantics;
using MLIR.Semantics.Attributes;
using MLIR.Semantics.Attributes.Collections;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;
using Xunit;
using System.Numerics;

public sealed class DialectIntegrationTests
{
    [Fact]
    public void GeneratedDialectRegistrationExposesTypedDefinitions()
    {
        var dialect = MiniarithDialectRegistration.Create();
        var registry = new DialectRegistry();
        registry.RegisterDialect(dialect);

        Assert.Equal("miniarith", dialect.Name);

        Assert.True(registry.TryGetOperation("miniarith.addi", out var operationDefinition));
        Assert.NotNull(operationDefinition);
        Assert.NotNull(operationDefinition!.AssemblyFormat);
    }

    [Fact]
    public void BindingGenericSyntaxProducesGeneratedOperationAndSemanticMembers()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var document = Document.Parse("%result = \"miniarith.addi\"(%lhs, %rhs) : i32");
        var module = document.Bind(registry);

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
        var addType = typeof(MiniArith_AddIOp);
        var constantType = typeof(MiniArith_ConstantOp);

        Assert.Equal("miniarith", dialect.Name);
        Assert.Equal("MiniArith_AddIOp", addType.Name);
        Assert.Equal("MiniArith_ConstantOp", constantType.Name);
    }

    [Fact]
    public void GeneratedDialectCanReadAndWriteDocuments()
    {
        const string source = "%result = \"miniarith.addi\"(%lhs, %rhs) : i32";

        var document = Document.Parse(source);

        Assert.Equal(source, document.ToText());
    }

    [Fact]
    public void GeneratedDialectCanReadAndWriteBoundModules()
    {
        const string source = "%result = \"miniarith.addi\"(%lhs, %rhs) : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Document.Parse(source).Bind(registry);

        Assert.Equal(source, module.ToText());
    }

    [Fact]
    public void GeneratedAssemblyFormatBindProducesTypedAddIOpFromCustomBodySyntax()
    {
        // Arrange: construct a custom body matching "$lhs `,` $rhs attr-dict `:` type($result)"
        var body = new MiniArith_AddIOpBodySyntax(
            new SyntaxToken("%lhs"),
            new SyntaxToken(","),
            new SyntaxToken("%rhs"),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null),
            new SyntaxToken(":"),
            new RawTypeSyntax(new RawSyntaxText("i32")));

        var syntax = new OperationSyntax(
            resultTokens: [new SyntaxToken("%result")],
            resultCommaTokens: [],
            equalsToken: new SyntaxToken("="),
            nameToken: new SyntaxToken("miniarith.addi"),
            body: body);

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        // Act: bind invokes MiniArith_AddIOpAssemblyFormat.Bind because body is not GenericOperationBodySyntax
        var module = Binder.BindModule(new ModuleSyntax([syntax]), registry);

        // Assert
        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniArith_AddIOp>(Assert.Single(module.Operations));
        Assert.Equal("miniarith.addi", operation.Name);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
        Assert.NotNull(operation.TypeSignatureReference);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesPrimitiveAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_immediate 1, %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddImmediateOp>(Assert.Single(module.Operations));
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal((BigInteger)1, operation.Value);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesBooleanAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_bool_immediate true, %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddBoolImmediateOp>(Assert.Single(module.Operations));
        Assert.True(operation.Value);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesFloatingPointAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_float_immediate 1.5, %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddFloatImmediateOp>(Assert.Single(module.Operations));
        Assert.Equal(1.5f, operation.Value);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesStringAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_string_immediate \"hi\", %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddStringImmediateOp>(Assert.Single(module.Operations));
        Assert.Equal("hi", operation.Value);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesTypeAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_type_immediate i32, %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddTypeImmediateOp>(Assert.Single(module.Operations));
        var value = operation.Value;
        Assert.Equal("i32", value.TypeSyntax.GetRawText().Text);
        Assert.IsType<TypeAttributeValueSyntax>(value.Syntax);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesUnitAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_unit_immediate keyword %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddUnitImmediateOp>(Assert.Single(module.Operations));
        Assert.True(operation.Value);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesMissingUnitAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_unit_immediate %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddUnitImmediateOp>(Assert.Single(module.Operations));
        Assert.False(operation.Value);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesDenseArrayAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_array_immediate array<i32: 1, 2>, %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddArrayImmediateOp>(Assert.Single(module.Operations));
        var value = operation.Value;
        Assert.IsType<DenseArrayAttributeValueSyntax>(operation.Attributes["value"].Value.Syntax);
        Assert.Equal(2, value.Count);
        Assert.Equal(new System.Numerics.BigInteger(1), value[0]);
        Assert.Equal(new System.Numerics.BigInteger(2), value[1]);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesElementsAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_elements_immediate dense<[1, 2]> : tensor<2xi32>, %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddElementsImmediateOp>(Assert.Single(module.Operations));
        var value = operation.Value;
        Assert.IsType<ElementsAttributeValueSyntax>(value.Syntax);
        var payload = Assert.IsAssignableFrom<ArrayAttributeValue>(value.Payload);
        var first = Assert.IsAssignableFrom<IntegerAttributeValue>(payload.Items[0]);
        var second = Assert.IsAssignableFrom<IntegerAttributeValue>(payload.Items[1]);
        Assert.Equal(new System.Numerics.BigInteger(1), first.Value);
        Assert.Equal(new System.Numerics.BigInteger(2), second.Value);
        Assert.Equal("tensor<2xi32>", value.TypeSyntax.GetRawText().Text);
    }

    [Fact]
    public void GeneratedAssemblyFormatParsesDictionaryAttributeBeforeOperand()
    {
        const string source = "%result = miniarith.add_dictionary_immediate {inner = 1, nested = {flag = true}}, %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniArith_AddDictionaryImmediateOp>(Assert.Single(module.Operations));
        var value = operation.Value;
        Assert.IsType<DictionaryAttributeValueSyntax>(value.Syntax);
        var inner = Assert.IsAssignableFrom<IntegerAttributeValue>(value.Attributes["inner"].Value);
        Assert.Equal(new System.Numerics.BigInteger(1), inner.Value);
        var nested = Assert.IsAssignableFrom<DictionaryAttributeValue>(value.Attributes["nested"].Value);
        var flag = Assert.IsAssignableFrom<BooleanAttributeValue>(nested.Attributes["flag"].Value);
        Assert.True(flag.Value);
    }

    [Fact]
    public void GeneratedAssemblyFormatBindProducesTypedConstantOpFromCustomBodySyntax()
    {
        // Arrange: construct a custom body matching "$value attr-dict"
        var body = new MiniArith_ConstantOpBodySyntax(
            new RawAttributeValueSyntax(new RawSyntaxText("42")),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null));

        var syntax = new OperationSyntax(
            resultTokens: [new SyntaxToken("%result")],
            resultCommaTokens: [],
            equalsToken: new SyntaxToken("="),
            nameToken: new SyntaxToken("miniarith.constant"),
            body: body);

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        // Act: bind invokes MiniArith_ConstantOpAssemblyFormat.Bind because body is not GenericOperationBodySyntax
        var module = Binder.BindModule(new ModuleSyntax([syntax]), registry);

        // Assert
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
        // Arrange: pass a MiniArith_ConstantOpBodySyntax to the addi operation (wrong type)
        var body = new MiniArith_ConstantOpBodySyntax(
            new RawAttributeValueSyntax(new RawSyntaxText("42")),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null));

        var syntax = new OperationSyntax(
            resultTokens: [new SyntaxToken("%result")],
            resultCommaTokens: [],
            equalsToken: new SyntaxToken("="),
            nameToken: new SyntaxToken("miniarith.addi"),
            body: body);

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        // Act
        var module = Binder.BindModule(new ModuleSyntax([syntax]), registry);

        // Assert: one diagnostic reported, operation is uninterpreted
        Assert.Single(module.AssemblyDiagnostics);
        Assert.IsType<UninterpretedOperation>(Assert.Single(module.Operations));
    }

    [Fact]
    public void GeneratedAssemblyFormatBindReportsDiagnosticForWrongResultCount()
    {
        // Arrange: zero result tokens when the operation expects exactly one
        var body = new MiniArith_AddIOpBodySyntax(
            new SyntaxToken("%lhs"),
            new SyntaxToken(","),
            new SyntaxToken("%rhs"),
            new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null),
            new SyntaxToken(":"),
            new RawTypeSyntax(new RawSyntaxText("i32")));

        var syntax = new OperationSyntax(
            resultTokens: [],
            resultCommaTokens: [],
            equalsToken: null,
            nameToken: new SyntaxToken("miniarith.addi"),
            body: body);

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        // Act
        var module = Binder.BindModule(new ModuleSyntax([syntax]), registry);

        // Assert: one diagnostic reported, operation is uninterpreted
        Assert.Single(module.AssemblyDiagnostics);
        Assert.IsType<UninterpretedOperation>(Assert.Single(module.Operations));
    }

    // -----------------------------------------------------------------------
    // MiniTest dialect: QualifiedDirectiveChunk, OptionalGroup, OilistDirectiveChunk
    // -----------------------------------------------------------------------

    [Fact]
    public void MinitestDialectRegistrationExposesAllOperations()
    {
        var dialect = MinitestDialectRegistration.Create();
        var registry = new DialectRegistry();
        registry.RegisterDialect(dialect);

        Assert.Equal("minitest", dialect.Name);

        Assert.True(registry.TryGetOperation("minitest.cast", out var castDef));
        Assert.NotNull(castDef!.AssemblyFormat);

        Assert.True(registry.TryGetOperation("minitest.binary", out var binaryDef));
        Assert.NotNull(binaryDef!.AssemblyFormat);

        Assert.True(registry.TryGetOperation("minitest.config", out var configDef));
        Assert.NotNull(configDef!.AssemblyFormat);
    }

    // -----------------------------------------------------------------------
    // minitest.cast — QualifiedDirectiveChunk
    // -----------------------------------------------------------------------

    [Fact]
    public void CastOpParsesQualifiedTypeFormat()
    {
        // minitest.cast uses qualified(type($result)) in its assembly format.
        // Parsing is identical to a plain type(...) directive.
        const string source = "%result = minitest.cast %input : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var document = Document.Parse(source, registry);

        var op = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_CastOpBodySyntax>(op.Body);
        Assert.Equal("%input", body.Input.Text);
        Assert.Equal("i32", body.ResultType.GetRawText().Text);
    }

    [Fact]
    public void CastOpBindsToTypedOperation()
    {
        const string source = "%result = minitest.cast %input : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);

        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniTest_CastOp>(Assert.Single(module.Operations));
        Assert.Equal("minitest.cast", operation.Name);
        Assert.Equal("%input", operation.Input.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
    }

    // -----------------------------------------------------------------------
    // minitest.binary — OptionalGroup (punctuation guard)
    // -----------------------------------------------------------------------

    [Fact]
    public void BinaryOpParsesWithBothOperands()
    {
        // The optional group is guarded by a leading comma; when present both rhs and
        // commaToken fields must be populated.
        const string source = "%result = minitest.binary %lhs, %rhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var document = Document.Parse(source, registry);

        var op = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_BinaryOpBodySyntax>(op.Body);
        Assert.Equal("%lhs", body.Lhs.Text);
        Assert.True(body.CommaToken.HasValue);
        Assert.Equal(",", body.CommaToken!.Value.Text);
        Assert.True(body.Rhs.HasValue);
        Assert.Equal("%rhs", body.Rhs!.Value.Text);
        Assert.Equal("i32", body.ResultType.GetRawText().Text);
    }

    [Fact]
    public void BinaryOpParsesWithOptionalOperandAbsent()
    {
        // When no comma follows lhs, the optional group is skipped and rhs/commaToken stay null.
        const string source = "%result = minitest.binary %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var document = Document.Parse(source, registry);

        var op = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_BinaryOpBodySyntax>(op.Body);
        Assert.Equal("%lhs", body.Lhs.Text);
        Assert.False(body.CommaToken.HasValue);
        Assert.False(body.Rhs.HasValue);
        Assert.Equal("i32", body.ResultType.GetRawText().Text);
    }

    // -----------------------------------------------------------------------
    // minitest.config — OilistDirectiveChunk
    // -----------------------------------------------------------------------

    [Fact]
    public void ConfigOpParsesWithStrideThenPadding()
    {
        // Oilist clause order: stride before padding.
        const string source =
            "minitest.config\n" +
            "    stride 4\n" +
            "    padding 0\n" +
            "    {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var document = Document.Parse(source, registry);

        var op = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_ConfigOpBodySyntax>(op.Body);
        Assert.True(body.StrideKeyword.HasValue);
        Assert.Equal("stride", body.StrideKeyword!.Value.Text);
        Assert.NotNull(body.Stride);
        Assert.True(body.PaddingKeyword.HasValue);
        Assert.Equal("padding", body.PaddingKeyword!.Value.Text);
        Assert.NotNull(body.Padding);
    }

    [Fact]
    public void ConfigOpParsesWithPaddingThenStride()
    {
        // Oilist allows any clause order; padding before stride must produce the same fields.
        const string source =
            "minitest.config\n" +
            "    padding 0\n" +
            "    stride 4\n" +
            "    {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var document = Document.Parse(source, registry);

        var op = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_ConfigOpBodySyntax>(op.Body);
        Assert.True(body.StrideKeyword.HasValue);
        Assert.Equal("stride", body.StrideKeyword!.Value.Text);
        Assert.NotNull(body.Stride);
        Assert.True(body.PaddingKeyword.HasValue);
        Assert.Equal("padding", body.PaddingKeyword!.Value.Text);
        Assert.NotNull(body.Padding);
    }

    [Fact]
    public void ConfigOpParsesWithOnlyOneClause()
    {
        // Oilist clauses are optional individually; only stride being present is valid.
        const string source =
            "minitest.config\n" +
            "    stride 4\n" +
            "    {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var document = Document.Parse(source, registry);

        var op = Assert.Single(document.Module.Operations);
        var body = Assert.IsType<MiniTest_ConfigOpBodySyntax>(op.Body);
        Assert.True(body.StrideKeyword.HasValue);
        Assert.Equal("stride", body.StrideKeyword!.Value.Text);
        Assert.NotNull(body.Stride);
        Assert.False(body.PaddingKeyword.HasValue);
        Assert.Null(body.Padding);
    }

    [Fact]
    public void ConfigOpBindsWithBothAttributes()
    {
        const string source =
            "minitest.config\n" +
            "    stride 4\n" +
            "    padding 0\n" +
            "    {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);

        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniTest_ConfigOp>(Assert.Single(module.Operations));
        Assert.Equal("minitest.config", operation.Name);
        Assert.Equal((BigInteger)4, operation.Stride);
        Assert.Equal((BigInteger)0, operation.Padding);
    }

    [Fact]
    public void BinaryOpBindsWithOptionalRhsAbsent()
    {
        // The optional group "(`,` $rhs^)?" is absent; Rhs should be null after binding.
        const string source = "%result = minitest.binary %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);

        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniTest_BinaryOp>(Assert.Single(module.Operations));
        Assert.Equal("minitest.binary", operation.Name);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Null(operation.Rhs);
        Assert.Equal("%result", operation.ResultValue.Name);
    }

    [Fact]
    public void GeneratedOperandSetterUpdatesOptionalOperandAndCustomPrinting()
    {
        const string source =
            "%lhs = \"test.left\"() : () -> i32\n" +
            "%rhs = \"test.right\"() : () -> i32\n" +
            "%result = minitest.binary %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var rhsValue = module.Operations[1].Results[0];
        var operation = Assert.IsType<MiniTest_BinaryOp>(module.Operations[2]);
        string printed;

        operation.Rhs = rhsValue;
        printed = module.ToText(CustomAssemblyOptions);

        Assert.Same(rhsValue, operation.Rhs);
        var rebound = Document.Parse(printed, registry).Bind(registry);
        var reboundOperation = Assert.IsType<MiniTest_BinaryOp>(rebound.Operations[2]);
        Assert.NotNull(reboundOperation.Rhs);
        Assert.Equal("%rhs", reboundOperation.Rhs!.Name);
    }

    [Fact]
    public void ConfigOpBindsWithOnlyStridePresent()
    {
        // The padding oilist clause is absent; Padding should be null after binding.
        const string source =
            "minitest.config\n" +
            "    stride 4\n" +
            "    {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);

        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniTest_ConfigOp>(Assert.Single(module.Operations));
        Assert.Equal("minitest.config", operation.Name);
        Assert.Equal((BigInteger)4, operation.Stride);
        Assert.Null(operation.Padding);
    }

    [Fact]
    public void ConfigOpBindsWithNeitherAttributePresent()
    {
        // No oilist clauses at all; both Stride and Padding should be null after binding.
        const string source = "minitest.config {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);

        Assert.Empty(module.AssemblyDiagnostics);
        var operation = Assert.IsType<MiniTest_ConfigOp>(Assert.Single(module.Operations));
        Assert.Equal("minitest.config", operation.Name);
        Assert.Null(operation.Stride);
        Assert.Null(operation.Padding);
    }

    [Fact]
    public void GeneratedAttributeSetterAddsAndRemovesOptionalAttribute()
    {
        const string source = "minitest.config {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var operation = Assert.IsType<MiniTest_ConfigOp>(Assert.Single(module.Operations));

        operation.Stride = (BigInteger)4;

        Assert.Equal((BigInteger)4, operation.Stride);
        Assert.Contains("stride 4", module.ToText(CustomAssemblyOptions));

        operation.Stride = null;

        Assert.Null(operation.Stride);
        Assert.DoesNotContain("stride", module.ToText(CustomAssemblyOptions));
    }

    // -----------------------------------------------------------------------
    // BuildCustomAssemblySyntax – printing with ReplaceExistingSyntax
    // -----------------------------------------------------------------------

    private static ConcreteSyntaxBuilder.ConcreteSyntaxBuilderOptions CustomAssemblyOptions =>
        new(ConcreteSyntaxBuilder.OperationSyntaxPreference.PreferCustomAssembly,
            ConcreteSyntaxBuilder.ExistingSyntaxHandling.ReplaceExistingSyntax);

    [Fact]
    public void BuildCustomAssemblySyntaxEmitsCustomNameAndBodyForAddIOp()
    {
        // Parse from custom format, bind, then force custom printing (ReplaceExistingSyntax).
        // The result must use the unquoted operation name and parse back as MiniArith_AddIOpBodySyntax.
        const string source = "%result = miniarith.addi %lhs, %rhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var printed = module.ToText(CustomAssemblyOptions);

        // Should not use the generic (quoted) name.
        Assert.DoesNotContain("\"miniarith.addi\"", printed);
        Assert.Contains("miniarith.addi", printed);

        // Output should round-trip: parse back to the same custom body type and semantic data.
        var module2 = Document.Parse(printed, registry).Bind(registry);
        Assert.Empty(module2.AssemblyDiagnostics);
        var op = Assert.IsType<MiniArith_AddIOp>(Assert.Single(module2.Operations));
        Assert.Equal("%lhs", op.Lhs.Name);
        Assert.Equal("%rhs", op.Rhs.Name);
        Assert.Equal("%result", op.ResultValue.Name);
    }

    [Fact]
    public void BuildCustomAssemblySyntaxEmitsCustomNameAndBodyForConstantOp()
    {
        // miniarith.constant uses "$value attr-dict" (no type directive).
        const string source = "%result = miniarith.constant 42";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var printed = module.ToText(CustomAssemblyOptions);

        Assert.DoesNotContain("\"miniarith.constant\"", printed);
        Assert.Contains("miniarith.constant", printed);

        var module2 = Document.Parse(printed, registry).Bind(registry);
        Assert.Empty(module2.AssemblyDiagnostics);
        var op = Assert.IsType<MiniArith_ConstantOp>(Assert.Single(module2.Operations));
        Assert.Equal((BigInteger)42, op.Value);
        Assert.Equal("%result", op.ResultValue.Name);
    }

    [Fact]
    public void BuildCustomAssemblySyntaxEmitsCustomBodyForCastOp()
    {
        // minitest.cast uses "$input attr-dict `:` qualified(type($result))".
        const string source = "%result = minitest.cast %input : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var printed = module.ToText(CustomAssemblyOptions);

        Assert.DoesNotContain("\"minitest.cast\"", printed);
        Assert.Contains("minitest.cast", printed);

        var module2 = Document.Parse(printed, registry).Bind(registry);
        Assert.Empty(module2.AssemblyDiagnostics);
        var op = Assert.IsType<MiniTest_CastOp>(Assert.Single(module2.Operations));
        Assert.Equal("%input", op.Input.Name);
        Assert.Equal("%result", op.ResultValue.Name);
    }

    [Fact]
    public void BuildCustomAssemblySyntaxEmitsOptionalGroupWhenRhsPresent()
    {
        // minitest.binary with rhs present – the optional group should be emitted.
        const string source = "%result = minitest.binary %lhs, %rhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var printed = module.ToText(CustomAssemblyOptions);

        Assert.Contains("minitest.binary", printed);

        var module2 = Document.Parse(printed, registry).Bind(registry);
        Assert.Empty(module2.AssemblyDiagnostics);
        var op = Assert.IsType<MiniTest_BinaryOp>(Assert.Single(module2.Operations));
        Assert.Equal("%lhs", op.Lhs.Name);
        Assert.NotNull(op.Rhs);
        Assert.Equal("%rhs", op.Rhs!.Name);
        Assert.Equal("%result", op.ResultValue.Name);
    }

    [Fact]
    public void BuildCustomAssemblySyntaxOmitsOptionalGroupWhenRhsAbsent()
    {
        // minitest.binary without rhs – the optional group should be absent from output.
        const string source = "%result = minitest.binary %lhs : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var printed = module.ToText(CustomAssemblyOptions);

        Assert.Contains("minitest.binary", printed);

        var module2 = Document.Parse(printed, registry).Bind(registry);
        Assert.Empty(module2.AssemblyDiagnostics);
        var op = Assert.IsType<MiniTest_BinaryOp>(Assert.Single(module2.Operations));
        Assert.Equal("%lhs", op.Lhs.Name);
        Assert.Null(op.Rhs);
        Assert.Equal("%result", op.ResultValue.Name);
    }

    [Fact]
    public void BuildCustomAssemblySyntaxEmitsOilistClausesForConfigOp()
    {
        // minitest.config with both attributes – both oilist clauses should be emitted.
        const string source =
            "minitest.config\n" +
            "    stride 4\n" +
            "    padding 0\n" +
            "    {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var printed = module.ToText(CustomAssemblyOptions);

        Assert.Contains("minitest.config", printed);
        Assert.Contains("stride", printed);
        Assert.Contains("padding", printed);

        var module2 = Document.Parse(printed, registry).Bind(registry);
        Assert.Empty(module2.AssemblyDiagnostics);
        var op = Assert.IsType<MiniTest_ConfigOp>(Assert.Single(module2.Operations));
        Assert.NotNull(op.Stride);
        Assert.NotNull(op.Padding);
    }

    [Fact]
    public void BuildCustomAssemblySyntaxOmitsAbsentOilistClauseForConfigOp()
    {
        // minitest.config with only stride – padding clause should be absent.
        const string source =
            "minitest.config\n" +
            "    stride 4\n" +
            "    {}";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());

        var module = Document.Parse(source, registry).Bind(registry);
        var printed = module.ToText(CustomAssemblyOptions);

        Assert.Contains("minitest.config", printed);
        Assert.Contains("stride", printed);
        Assert.DoesNotContain("padding", printed);

        var module2 = Document.Parse(printed, registry).Bind(registry);
        Assert.Empty(module2.AssemblyDiagnostics);
        var op = Assert.IsType<MiniTest_ConfigOp>(Assert.Single(module2.Operations));
        Assert.NotNull(op.Stride);
        Assert.Null(op.Padding);
    }

    [Fact]
    public void GeneratedEnumOperationParsesRegularEnumAttributeIntoTypedProperty()
    {
        const string source = "%result = minienum.mode_op b, %input : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinienumDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniEnum_ModeOp>(Assert.Single(module.Operations));
        Assert.Equal(Mode.B, operation.Mode);
        Assert.Equal("%input", operation.Input.Name);
    }

    [Fact]
    public void GeneratedEnumOperationParsesBitEnumAttributeIntoTypedFlagsProperty()
    {
        const string source = "%result = minienum.flags_op x,y %input : i32";

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinienumDialectRegistration.Create());

        var module = Binder.BindModule(Parser.ParseModule(source, registry), registry);

        var operation = Assert.IsType<MiniEnum_FlagsOp>(Assert.Single(module.Operations));
        Assert.Equal(Flags.X | Flags.Y, operation.Flags);
        Assert.Equal("%input", operation.Input.Name);
    }

    [Fact]
    public void GeneratedEnumOperationPrintsBitEnumsUsingConfiguredSeparatorAndAlias()
    {
        var operation = new MiniEnum_FlagsOp(
            input: new UnresolvedValue(new SyntaxToken("%input")),
            resultValue: new OperationResult(new SyntaxToken("%result")),
            flags: Flags.X | Flags.Y,
            typeSignatureReference: new UnknownTypeReference(new RawTypeSyntax(new RawSyntaxText("i32")), "i32", null, SourceLocation.Unknown));

        var registry = new DialectRegistry();
        registry.RegisterDialect(MinienumDialectRegistration.Create());

        var printed = new Module(new ModuleSyntax([]), [operation], []).ToText(CustomAssemblyOptions);

        Assert.Contains("minienum.flags_op xy %input: i32", printed);
    }
}
