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

    [Fact]
    public void RegistryResolvesStandaloneAndConcreteTypeConstraints()
    {
        var registry = new DialectRegistry();
        var standaloneConstraint = new TypeConstraintDefinition("AnyType");
        var concreteType = new TypeDefinition("i32");
        registry.RegisterDialect(new Dialect("builtin", [], [], [concreteType], [], [], [standaloneConstraint]));

        Assert.True(registry.TryResolveTypeConstraint("AnyType", out var resolvedStandaloneConstraint));
        Assert.Same(standaloneConstraint, resolvedStandaloneConstraint);

        Assert.True(registry.TryResolveTypeConstraint("i32", out var resolvedConcreteConstraint));
        Assert.Same(concreteType, resolvedConcreteConstraint);
        Assert.IsType<TypeDefinition>(resolvedConcreteConstraint);

        Assert.True(registry.TryGetType("i32", out var resolvedConcreteType));
        Assert.Same(concreteType, resolvedConcreteType);

        Assert.False(registry.TryGetType("AnyType", out _));
    }

    [Fact]
    public void ConcreteTypeDefinitionSupersedesEarlierConstraintWithSameName()
    {
        var registry = new DialectRegistry();
        var aggregateConstraint = new TypeConstraintDefinition("builtin.function");
        var aggregateType = new TypeDefinition("builtin.function");

        registry.RegisterDialect(new Dialect("prelude", [], [], [], [], [], [aggregateConstraint]));
        registry.RegisterDialect(new Dialect("builtin", [], [], [aggregateType]));

        Assert.True(registry.TryResolveTypeConstraint("builtin.function", out var resolvedConstraint));
        Assert.Same(aggregateType, resolvedConstraint);
        Assert.True(registry.TryGetType("builtin.function", out var resolvedType));
        Assert.Same(aggregateType, resolvedType);
    }
}
