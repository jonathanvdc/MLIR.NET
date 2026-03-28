namespace DialectTests;

using MLIR;
using MLIR.Dialects;
using MLIR.Miniarith;
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
}
