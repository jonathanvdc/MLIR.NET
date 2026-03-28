namespace MLIR.GeneratedIntegrationTests;

using MLIR.Dialects;
using MLIR.Generated.Arith;
using Xunit;

public sealed class GeneratedDialectIntegrationTests
{
    [Fact]
    public void GeneratedDialectRegistrationExposesTypedDefinitions()
    {
        var dialect = ArithDialectRegistration.Create();
        var registry = new DialectRegistry();
        registry.RegisterDialect(dialect);

        Assert.Equal("arith", dialect.Name);

        Assert.True(registry.TryGetOperation("arith.addi", out var operationDefinition));
        Assert.NotNull(operationDefinition);
        Assert.NotNull(operationDefinition!.AssemblyFormat);

        Assert.True(registry.TryGetAttribute("fastmath", out var attributeDefinition));
        Assert.NotNull(attributeDefinition);

        Assert.True(registry.TryGetType("i32", out var typeDefinition));
        Assert.NotNull(typeDefinition);
    }

    [Fact]
    public void BindingGenericSyntaxProducesGeneratedOperationAndSemanticMembers()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(ArithDialectRegistration.Create());

        var document = Document.Parse("%result = \"arith.addi\"(%lhs, %rhs) {fastmath = #fastmath} : i32");
        var module = document.Bind(registry);

        var operation = Assert.IsType<AddIOperation>(Assert.Single(module.Operations));
        Assert.Equal("arith.addi", operation.Name);
        Assert.Equal("%lhs", operation.Lhs.Name);
        Assert.Equal("%rhs", operation.Rhs.Name);
        Assert.Equal("%result", operation.ResultValue.Name);
        Assert.IsType<I32TypeReference>(operation.TypeSignatureReference);

        var attribute = operation.GetAttribute("fastmath");
        Assert.IsType<FastMathAttributeValue>(attribute.ValueReference);
    }

    [Fact]
    public void GeneratedTypesAreUsableFromNormalRuntimeCode()
    {
        var dialect = ArithDialectRegistration.Create();
        var operationType = typeof(AddIOperation);
        var attributeType = typeof(FastMathAttributeValue);
        var typeReferenceType = typeof(I32TypeReference);

        Assert.Equal("arith", dialect.Name);
        Assert.Equal("AddIOperation", operationType.Name);
        Assert.Equal("FastMathAttributeValue", attributeType.Name);
        Assert.Equal("I32TypeReference", typeReferenceType.Name);
    }
}
