namespace MLIR.Generators.Tests;

using Xunit;

/// <summary>
/// Tests for the generator's automatic emission of <c>SymbolName</c>, <c>Symbols</c>, and
/// <c>GetSymbol</c> properties on operations that carry the ODS <c>Symbol</c> or
/// <c>SymbolTable</c> traits.
/// </summary>
public sealed class DialectGeneratorSymbolPropertiesTests : DialectGeneratorTestBase
{
    // An op that only has the Symbol trait.
    private static readonly string[] SymbolOnlyOpLines =
    [
        "include \"mlir/IR/SymbolInterfaces.td\"",
        string.Empty,
        "def MyDialect_FuncOp : MyDialect_Op<\"func\", [Symbol]> {",
        "  let summary = \"A named function operation\";",
        "};",
    ];

    // An op that only has the SymbolTable trait (no regions — intentionally; the trait is still emitted).
    private static readonly string[] SymbolTableOnlyOpLines =
    [
        "include \"mlir/IR/SymbolInterfaces.td\"",
        string.Empty,
        "def MyDialect_ContainerOp : MyDialect_Op<\"container\", [SymbolTable]> {",
        "  let summary = \"A symbol-table container operation\";",
        "  let regions = (region SizedRegion<1>:$bodyRegion);",
        "};",
    ];

    // An op that has both Symbol and SymbolTable traits (like mlir::builtin::ModuleOp).
    private static readonly string[] SymbolAndSymbolTableOpLines =
    [
        "include \"mlir/IR/SymbolInterfaces.td\"",
        string.Empty,
        "def MyDialect_ModuleOp : MyDialect_Op<\"module\", [Symbol, SymbolTable]> {",
        "  let summary = \"A top-level module with symbol table\";",
        "  let regions = (region SizedRegion<1>:$bodyRegion);",
        "};",
    ];

    // An op with neither Symbol nor SymbolTable — no symbol properties should be generated.
    private static readonly string[] PlainOpLines =
    [
        "def MyDialect_PlainOp : MyDialect_Op<\"plain\", []> {",
        "  let summary = \"A plain op without symbol traits\";",
        "};",
    ];

    // --- Symbol trait tests ---

    [Fact]
    public void GeneratesSymbolNamePropertyForSymbolOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        Assert.Contains("public string? SymbolName", source);
        Assert.Contains("Attributes.TryGet(\"sym_name\",", source);
        Assert.Contains("StringAttributeValue sv", source);
        Assert.Contains("SyntheticStringAttributeValue(value)", source);
    }

    [Fact]
    public void SymbolNamePropertyDocCommentMentionsSymbolTrait()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        Assert.Contains("ODS <c>Symbol</c> trait", source);
    }

    [Fact]
    public void DoesNotGenerateSymbolNameForOpWithoutSymbolTrait()
    {
        var source = GenerateMyDialectRegistrationSource(PlainOpLines);

        Assert.DoesNotContain("SymbolName", source);
    }

    // --- SymbolTable trait tests ---

    [Fact]
    public void GeneratesSymbolsDictionaryForSymbolTableOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolTableOnlyOpLines);

        Assert.Contains("private Dictionary<string, Operation>? _symbolCache;", source);
        Assert.Contains("public IReadOnlyDictionary<string, Operation> Symbols => GetOrBuildSymbolCache();", source);
        Assert.Contains("private Dictionary<string, Operation> GetOrBuildSymbolCache()", source);
        Assert.Contains("_symbolCache = cache;", source);
    }

    [Fact]
    public void GeneratesGetSymbolOverrideForSymbolTableOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolTableOnlyOpLines);

        Assert.Contains("[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]", source);
        Assert.Contains("public override TSymbol GetSymbol<TSymbol>(string name)", source);
        Assert.Contains("GetOrBuildSymbolCache().TryGetValue(name, out var op)", source);
    }

    [Fact]
    public void GeneratesInvalidateSyntaxOverrideForSymbolTableOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolTableOnlyOpLines);

        Assert.Contains("public override void InvalidateSyntax()", source);
        Assert.Contains("_symbolCache = null;", source);
        Assert.Contains("base.InvalidateSyntax();", source);
    }

    [Fact]
    public void SymbolTablePropertiesDocCommentsMentionSymbolTableTrait()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolTableOnlyOpLines);

        Assert.Contains("ODS <c>SymbolTable</c> trait", source);
        Assert.Contains("built lazily and cached", source);
    }

    [Fact]
    public void DoesNotGenerateSymbolsDictionaryForOpWithoutSymbolTableTrait()
    {
        var source = GenerateMyDialectRegistrationSource(PlainOpLines);

        Assert.DoesNotContain("IReadOnlyDictionary<string, Operation> Symbols", source);
        Assert.DoesNotContain("GetSymbol<TSymbol>", source);
    }

    // --- Combined Symbol + SymbolTable ---

    [Fact]
    public void GeneratesBothSymbolNameAndSymbolsForOpWithBothTraits()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolAndSymbolTableOpLines);

        Assert.Contains("public string? SymbolName", source);
        Assert.Contains("public IReadOnlyDictionary<string, Operation> Symbols => GetOrBuildSymbolCache();", source);
        Assert.Contains("public override TSymbol GetSymbol<TSymbol>(string name)", source);
        Assert.Contains("public override void InvalidateSyntax()", source);
    }

    [Fact]
    public void DoesNotGenerateSymbolNameForSymbolTableOnlyOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolTableOnlyOpLines);

        Assert.DoesNotContain("public string? SymbolName", source);
    }

    [Fact]
    public void DoesNotGenerateSymbolsDictionaryForSymbolOnlyOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        Assert.DoesNotContain("IReadOnlyDictionary<string, Operation> Symbols", source);
        Assert.DoesNotContain("public override TSymbol? GetSymbol<TSymbol>", source);
    }
}
