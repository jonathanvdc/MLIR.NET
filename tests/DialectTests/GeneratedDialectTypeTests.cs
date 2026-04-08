namespace DialectTests;

using MLIR.Typedtype;
using Xunit;

public sealed class GeneratedDialectTypeTests : DialectIntegrationTestBase
{
    [Fact]
    public void GeneratedCustomTypeRoundTripsThroughParseBindAndPrint()
    {
        const string source = "%result = typedtype.round_trip : !typed.opaque<\"hello\">";

        var operation = ReprintAndRebindSingleOperation<TypedType_RoundTripOp>(
            source,
            CreateTypedTypeRegistry(),
            out var printed);

        Assert.Contains("typedtype.round_trip", printed);
        Assert.Contains("!typed.opaque<\"hello\">", printed);
        Assert.Equal("%result", operation.ResultValue.Name);

        var type = Assert.IsType<OpaqueType>(operation.TypeSignatureReference);
        Assert.Equal("hello", type.Value);
        Assert.Equal("!typed.opaque<\"hello\">", type.Syntax!.ToString());
    }
}
