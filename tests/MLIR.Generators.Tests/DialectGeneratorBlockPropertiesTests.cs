namespace MLIR.Generators.Tests;

using Xunit;

/// <summary>
/// Tests for the generator's automatic emission of <c>Block</c> and <c>Operations</c>
/// convenience properties on operations that satisfy the ODS <c>SingleBlock</c> and
/// <c>NoRegionArguments</c> constraints.
/// </summary>
public sealed class DialectGeneratorBlockPropertiesTests : DialectGeneratorTestBase
{
    // A minimal op definition that has SingleBlock via GraphRegionNoTerminator and
    // NoRegionArguments explicitly — matching the ModuleOp pattern.
    private static readonly string[] ModuleStyleOpLines =
    [
        "include \"mlir/IR/RegionKindInterface.td\"",
        string.Empty,
        "def MyDialect_ModuleOp : MyDialect_Op<\"module\", [",
        "    NoRegionArguments",
        "  ] # GraphRegionNoTerminator.traits> {",
        "  let summary = \"A top level container operation\";",
        "  let regions = (region SizedRegion<1>:$bodyRegion);",
        "  let assemblyFormat = \"$bodyRegion attr-dict\";",
        "};",
    ];

    // An op with SingleBlock but without NoRegionArguments — only Block should be generated.
    private static readonly string[] SingleBlockOnlyOpLines =
    [
        "include \"mlir/IR/RegionKindInterface.td\"",
        string.Empty,
        "def MyDialect_SbOnlyOp : MyDialect_Op<\"sb_only\", [",
        "    SingleBlock",
        "]> {",
        "  let summary = \"An op with a single block but region arguments\";",
        "  let regions = (region SizedRegion<1>:$body);",
        "  let assemblyFormat = \"$body attr-dict\";",
        "};",
    ];

    // A plain op with a region but no SingleBlock trait — no convenience properties.
    private static readonly string[] PlainRegionOpLines =
    [
        "def MyDialect_PlainRegionOp : MyDialect_Op<\"plain_region\", []> {",
        "  let regions = (region SizedRegion<1>:$body);",
        "  let assemblyFormat = \"$body attr-dict\";",
        "};",
    ];

    // An op with multiple regions — no convenience properties even if SingleBlock is present.
    private static readonly string[] MultiRegionOpLines =
    [
        "include \"mlir/IR/RegionKindInterface.td\"",
        string.Empty,
        "def MyDialect_MultiRegionOp : MyDialect_Op<\"multi_region\", [",
        "    SingleBlock, NoRegionArguments",
        "]> {",
        "  let regions = (region AnyRegion:$trueRegion, AnyRegion:$falseRegion);",
        "  let assemblyFormat = \"$trueRegion `else` $falseRegion attr-dict\";",
        "};",
    ];

    // An op with no regions — no convenience properties.
    private static readonly string[] NoRegionOpLines =
    [
        "def MyDialect_NoRegionOp : MyDialect_Op<\"no_region\", [",
        "    SingleBlock, NoRegionArguments",
        "]> {",
        "  let arguments = (ins I32:$value);",
        "  let results = (outs I32:$result);",
        "  let assemblyFormat = \"$value attr-dict `:` type($result)\";",
        "};",
    ];

    [Fact]
    public void GeneratesBlockAndOperationsForModuleStyleOp()
    {
        var source = GenerateMyDialectRegistrationSource(ModuleStyleOpLines);

        AssertContainsAll(
            source,
            "public Block Block => BodyRegion.Blocks.Single();",
            "public IReadOnlyList<Operation> Operations => Block.Operations;");
    }

    [Fact]
    public void GeneratesBlockButNotOperationsWhenNoRegionArgumentsIsMissing()
    {
        var source = GenerateMyDialectRegistrationSource(SingleBlockOnlyOpLines);

        Assert.Contains("public Block Block => Body.Blocks.Single();", source);
        Assert.DoesNotContain("public IReadOnlyList<Operation> Operations", source);
    }

    [Fact]
    public void DoesNotGenerateBlockOrOperationsWhenSingleBlockIsAbsent()
    {
        var source = GenerateMyDialectRegistrationSource(PlainRegionOpLines);

        Assert.DoesNotContain("public Block Block", source);
        Assert.DoesNotContain("public IReadOnlyList<Operation> Operations", source);
    }

    [Fact]
    public void DoesNotGenerateBlockOrOperationsForMultipleRegions()
    {
        var source = GenerateMyDialectRegistrationSource(MultiRegionOpLines);

        Assert.DoesNotContain("public Block Block", source);
        Assert.DoesNotContain("public IReadOnlyList<Operation> Operations", source);
    }

    [Fact]
    public void DoesNotGenerateBlockOrOperationsWhenNoRegionsDeclared()
    {
        var source = GenerateMyDialectRegistrationSource(NoRegionOpLines);

        Assert.DoesNotContain("public Block Block", source);
        Assert.DoesNotContain("public IReadOnlyList<Operation> Operations", source);
    }

    [Fact]
    public void BlockPropertyDocCommentMentionsSingleBlockConstraintAndSummary()
    {
        var source = GenerateMyDialectRegistrationSource(ModuleStyleOpLines);

        AssertContainsAll(
            source,
            "/// <summary>Gets the single block of this operation's body region.</summary>",
            "/// satisfies the ODS <c>SingleBlock</c> constraint.",
            "A top level container operation");
    }

    [Fact]
    public void OperationsPropertyDocCommentMentionsBothConstraintsAndSummary()
    {
        var source = GenerateMyDialectRegistrationSource(ModuleStyleOpLines);

        AssertContainsAll(
            source,
            "/// <summary>Gets the operations in the single block of this operation's body region.</summary>",
            "/// satisfies ODS constraints that imply a single block (<c>SingleBlock</c>) and no region",
            "/// arguments (<c>NoRegionArguments</c>).",
            "A top level container operation");
    }

    [Fact]
    public void GeneratesBlockAndOperationsForSingleBlockOpWithGraphRegionNoTerminatorNested()
    {
        // Verify that SingleBlock discovered via TraitListModel nesting in
        // GraphRegionNoTerminator also triggers the convenience properties.
        var lines = new[]
        {
            "include \"mlir/IR/RegionKindInterface.td\"",
            string.Empty,
            "def MyDialect_GraphOp : MyDialect_Op<\"graph\", [NoRegionArguments, GraphRegionNoTerminator]> {",
            "  let regions = (region SizedRegion<1>:$body);",
            "  let assemblyFormat = \"$body attr-dict\";",
            "};",
        };

        var source = GenerateMyDialectRegistrationSource(lines);

        // GraphRegionNoTerminator is a TraitList that includes SingleBlock.
        Assert.Contains("public Block Block => Body.Blocks.Single();", source);
        Assert.Contains("public IReadOnlyList<Operation> Operations => Block.Operations;", source);
    }
}
