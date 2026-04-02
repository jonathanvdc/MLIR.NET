namespace MLIR.Tests;

using MLIR.Dialects;
using Xunit;

public sealed partial class SemanticTests
{
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
}
