namespace DialectTests;

using MLIR;
using MLIR.Dialects.Minienum;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using Xunit;

public sealed class MiniEnumDialectTests : DialectIntegrationTestBase
{
    [Fact]
    public void GeneratedEnumOperationParsesRegularEnumAttributeIntoTypedProperty()
    {
        var operation = BindSingleOperation<MiniEnum_ModeOp>(
            "%result = minienum.mode_op b, %input : i32",
            CreateMiniEnumRegistry());

        Assert.Equal(Mode.B, operation.Mode);
        Assert.Equal("%input", operation.Input.Name);
    }

    [Fact]
    public void GeneratedEnumOperationParsesBitEnumAttributeIntoTypedFlagsProperty()
    {
        var operation = BindSingleOperation<MiniEnum_FlagsOp>(
            "%result = minienum.flags_op x,y %input : i32",
            CreateMiniEnumRegistry());

        Assert.Equal(Flags.X | Flags.Y, operation.Flags);
        Assert.Equal("%input", operation.Input.Name);
    }

    [Fact]
    public void GeneratedEnumOperationPrintsBitEnumsUsingConfiguredSeparatorAndAlias()
    {
        var operation = new MiniEnum_FlagsOp(
            input: new UnresolvedValue(TokenFactory.SsaName("%input")),
            resultValue: new OperationResult(TokenFactory.SsaName("%result")),
            flags: Flags.X | Flags.Y,
            typeSignatureReference: TypeFactory.I32);

        var registry = CreateMiniEnumRegistry();
        var printed = new Module(new ModuleSyntax([]), [operation], []).ToText(CustomAssemblyOptions);

        Assert.Contains("minienum.flags_op <xy> %input : i32", printed);
    }
}
