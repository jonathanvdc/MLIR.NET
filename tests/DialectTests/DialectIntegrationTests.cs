namespace DialectTests;

using MLIR;
using MLIR.Dialects;
using MLIR.Miniarith;
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
}
