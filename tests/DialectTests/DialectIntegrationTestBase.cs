namespace DialectTests;

using MLIR;
using MLIR.Dialects;
using MLIR.Miniarith;
using MLIR.Minienum;
using MLIR.Minitest;
using MLIR.Semantics;
using MLIR.Text;
using MLIR.Transforms;
using Xunit;

/// <summary>
/// Shared helpers for integration tests that exercise generated dialects through
/// parse, bind, and custom-printing flows.
/// </summary>
public abstract class DialectIntegrationTestBase
{
    protected static ConcreteSyntaxBuilder.ConcreteSyntaxBuilderOptions CustomAssemblyOptions =>
        new(
            ConcreteSyntaxBuilder.OperationSyntaxPreference.PreferCustomAssembly,
            ConcreteSyntaxBuilder.ExistingSyntaxHandling.ReplaceExistingSyntax);

    protected static DialectRegistry CreateMiniArithRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniarithDialectRegistration.Create());
        return registry;
    }

    protected static DialectRegistry CreateMiniTestRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(MinitestDialectRegistration.Create());
        return registry;
    }

    protected static DialectRegistry CreateMiniEnumRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(MinienumDialectRegistration.Create());
        return registry;
    }

    protected static Module ParseAndBind(string source, DialectRegistry registry) =>
        Document.Parse(source, registry).Bind(registry);

    protected static TOperation BindSingleOperation<TOperation>(string source, DialectRegistry registry)
        where TOperation : Operation =>
        Assert.IsType<TOperation>(Assert.Single(ParseAndBind(source, registry).Operations));

    protected static TOperation ReprintAndRebindSingleOperation<TOperation>(string source, DialectRegistry registry, out string printed)
        where TOperation : Operation
    {
        var module = ParseAndBind(source, registry);
        printed = module.ToText(CustomAssemblyOptions);

        var rebound = ParseAndBind(printed, registry);
        Assert.Empty(rebound.AssemblyDiagnostics);
        return Assert.IsType<TOperation>(Assert.Single(rebound.Operations));
    }
}
