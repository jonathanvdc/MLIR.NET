namespace DialectTests;

using MLIR;
using MLIR.Dialects;
using MLIR.Miniarith;
using MLIR.Minitest;
using MLIR.Semantics;
using MLIR.Syntax;
using Xunit;

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
        Assert.Equal("value", operation.Value.Name);
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
        Assert.Equal("stride", operation.Stride.Name);
        Assert.Equal("padding", operation.Padding.Name);
    }
}
