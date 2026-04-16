namespace DialectTests;

using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Miniemitc;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

/// <summary>
/// Integration tests for the <c>miniemitc</c> dialect, which exercises parametrised
/// <c>AttrDef</c> attributes with declarative <c>assemblyFormat</c> strings.
/// </summary>
public sealed class MiniEmitCDialectTests : DialectIntegrationTestBase
{
    private static DialectRegistry CreateMiniEmitCRegistry()
    {
        var registry = new DialectRegistry();
        registry.RegisterDialect(MiniemitcDialectRegistration.Create());
        return registry;
    }

    // -------------------------------------------------------------------------
    // OpaqueAttr round-trip: parse → bind → typed property
    // -------------------------------------------------------------------------

    [Fact]
    public void ParsesOpaqueAttributeIntoTypedValueProperty()
    {
        var op = BindSingleOperation<MiniEmitC_UseOpaqueOp>(
            "miniemitc.use_opaque #miniemitc.opaque<\"NULL\">",
            CreateMiniEmitCRegistry());

        var opaqueAttr = Assert.IsType<MLIR.Dialects.Miniemitc.OpaqueAttr>(op.Attr);
        Assert.Equal("NULL", opaqueAttr.Value);
    }

    [Fact]
    public void ParsesEmptyOpaqueAttributeValue()
    {
        var op = BindSingleOperation<MiniEmitC_UseOpaqueOp>(
            "miniemitc.use_opaque #miniemitc.opaque<\"\">",
            CreateMiniEmitCRegistry());

        var opaqueAttr = Assert.IsType<MLIR.Dialects.Miniemitc.OpaqueAttr>(op.Attr);
        Assert.Equal(string.Empty, opaqueAttr.Value);
    }

    [Fact]
    public void PrintsOpaqueAttributeWithCustomAssemblyFormat()
    {
        var op = new MiniEmitC_UseOpaqueOp(
            attr: new MLIR.Dialects.Miniemitc.OpaqueAttr("nullptr"),
            typeSignatureReference: null);

        var module = new Module(new ModuleSyntax([]), [op], []);
        var printed = module.ToText(CustomAssemblyOptions);

        Assert.Contains("#miniemitc.opaque<\"nullptr\">", printed);
    }

    [Fact]
    public void RoundTripsOpaqueAttributeViaParseAndPrint()
    {
        var registry = CreateMiniEmitCRegistry();
        var rebound = ReprintAndRebindSingleOperation<MiniEmitC_UseOpaqueOp>(
            "miniemitc.use_opaque #miniemitc.opaque<\"NULL\">",
            registry,
            out var printed);

        Assert.Contains("#miniemitc.opaque<\"NULL\">", printed);
        var opaqueAttr = Assert.IsType<MLIR.Dialects.Miniemitc.OpaqueAttr>(rebound.Attr);
        Assert.Equal("NULL", opaqueAttr.Value);
    }

    // -------------------------------------------------------------------------
    // OpaqueAttr typed constructor
    // -------------------------------------------------------------------------

    [Fact]
    public void TypedConstructorCreatesOpaqueAttrWithCorrectValue()
    {
        var attr = new MLIR.Dialects.Miniemitc.OpaqueAttr("my_value");
        Assert.Equal("my_value", attr.Value);
        Assert.Equal("miniemitc.opaque", attr.Name);
    }
}
