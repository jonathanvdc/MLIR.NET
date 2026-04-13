namespace MLIR.Generators.Tests;

using Xunit;

/// <summary>
/// Tests for the generator's handling of ODS <c>Symbol</c> and <c>SymbolTable</c> traits:
/// correct base-class selection, interface declaration, and property emission.
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

    // An op with the Symbol trait that also explicitly declares the underlying sym_name attribute.
    private static readonly string[] SymbolOpWithExplicitSymNameAttrLines =
    [
        "include \"mlir/IR/SymbolInterfaces.td\"",
        string.Empty,
        "def MyDialect_FuncWithExplicitSymNameAttrOp : MyDialect_Op<\"func_with_explicit_sym_name\", [Symbol]> {",
        "  let summary = \"A named function operation with explicit sym_name attr\";",
        "  let arguments = (ins SymbolNameAttr:$sym_name);",
        "};",
    ];

    // An op that only has the SymbolTable trait.
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

    // An op with neither Symbol nor SymbolTable.
    private static readonly string[] PlainOpLines =
    [
        "def MyDialect_PlainOp : MyDialect_Op<\"plain\", []> {",
        "  let summary = \"A plain op without symbol traits\";",
        "};",
    ];

    // --- Symbol trait: base class and interface ---

    [Fact]
    public void SymbolOpInheritsFromOperationAndImplementsISymbolOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        // Must declare Operation as base class and ISymbolOp as interface.
        Assert.Contains(": Operation, ISymbolOp", source);
    }

    [Fact]
    public void GeneratesSymbolNamePropertyForSymbolOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        Assert.Contains("public string? SymbolName", source);
        Assert.Contains("Attributes.TryGet(\"sym_name\",", source);
        Assert.Contains("StringAttr sv", source);
        Assert.Contains("ConstantAttributeFactory.String(value)", source);
    }

    [Fact]
    public void SymbolNamePropertyDocCommentMentionsSymbolTrait()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        Assert.Contains("ODS <c>Symbol</c> trait", source);
    }

    [Fact]
    public void SymbolOpWithExplicitSymNameAttributeDoesNotGenerateSymNameProperty()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOpWithExplicitSymNameAttrLines);

        Assert.Contains("public string? SymbolName", source);
        Assert.DoesNotContain(" SymName", source);
    }

    [Fact]
    public void DoesNotGenerateSymbolNameForOpWithoutSymbolTrait()
    {
        var source = GenerateMyDialectRegistrationSource(PlainOpLines);

        Assert.DoesNotContain("SymbolName", source);
    }

    // --- SymbolTable trait: base class ---

    [Fact]
    public void SymbolTableOpInheritsFromSymbolTableOperation()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolTableOnlyOpLines);

        // Must use SymbolTableOperation as the base class.
        Assert.Contains(": SymbolTableOperation", source);
    }

    [Fact]
    public void SymbolTableOpDoesNotEmitSymbolCacheFieldOrHelperMethods()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolTableOnlyOpLines);

        // All cache logic lives in SymbolTableOperation; nothing should be generated into the class.
        Assert.DoesNotContain("_symbolCache", source);
        Assert.DoesNotContain("GetOrBuildSymbolCache", source);
        Assert.DoesNotContain("override void InvalidateSyntax", source);
        Assert.DoesNotContain("override TSymbol GetSymbol", source);
        Assert.DoesNotContain("IReadOnlyDictionary<string, Operation> Symbols", source);
    }

    [Fact]
    public void DoesNotGenerateSymbolsDictionaryForOpWithoutSymbolTableTrait()
    {
        var source = GenerateMyDialectRegistrationSource(PlainOpLines);

        Assert.DoesNotContain("IReadOnlyDictionary<string, Operation> Symbols", source);
        Assert.DoesNotContain("GetSymbol<TSymbol>", source);
    }

    [Fact]
    public void PlainOpInheritsFromOperation()
    {
        var source = GenerateMyDialectRegistrationSource(PlainOpLines);

        Assert.Contains(": Operation", source);
        Assert.DoesNotContain("SymbolTableOperation", source);
        Assert.DoesNotContain("ISymbolOp", source);
    }

    // --- Combined Symbol + SymbolTable ---

    [Fact]
    public void OpWithBothTraitsInheritsFromSymbolTableOperationAndImplementsISymbolOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolAndSymbolTableOpLines);

        Assert.Contains(": SymbolTableOperation, ISymbolOp", source);
    }

    [Fact]
    public void OpWithBothTraitsGeneratesSymbolNameProperty()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolAndSymbolTableOpLines);

        Assert.Contains("public string? SymbolName", source);
    }

    [Fact]
    public void OpWithBothTraitsDoesNotEmitCacheOrOverrides()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolAndSymbolTableOpLines);

        Assert.DoesNotContain("_symbolCache", source);
        Assert.DoesNotContain("GetOrBuildSymbolCache", source);
    }

    // --- Symbol-only op: no SymbolTable emission ---

    [Fact]
    public void SymbolOnlyOpDoesNotInheritFromSymbolTableOperation()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        Assert.DoesNotContain("SymbolTableOperation", source);
    }

    [Fact]
    public void DoesNotGenerateSymbolsDictionaryForSymbolOnlyOp()
    {
        var source = GenerateMyDialectRegistrationSource(SymbolOnlyOpLines);

        Assert.DoesNotContain("IReadOnlyDictionary<string, Operation> Symbols", source);
        Assert.DoesNotContain("GetSymbol<TSymbol>", source);
    }
}
