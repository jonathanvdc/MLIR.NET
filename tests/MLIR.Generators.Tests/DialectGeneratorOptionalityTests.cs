namespace MLIR.Generators.Tests;

using Xunit;

public sealed class DialectGeneratorOptionalityTests : DialectGeneratorTestBase
{
    [Fact]
    public void OptionalAttributeAccessUsesDeclaredOptionalValueAccessMetadata()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_MetadataOp : MiniArith_Op<\"metadata\", []> {",
                "  let arguments = (ins BoolAttr:$flag, F32Attr:$scale, StrAttr:$label);",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public bool? Flag",
            "set => SetAttribute(\"flag\", value.HasValue ? global::MLIR.Semantics.ConstantAttributeFactory.Bool(value.Value) : null)",
            "public global::MLIR.Numerics.ApFloat? Scale",
            "set => SetAttribute(\"scale\", value.HasValue ? global::MLIR.Semantics.ConstantAttributeFactory.F32(value.Value) : null)",
            "public string? Label",
            "set => SetAttribute(\"label\", value != null ? global::MLIR.Semantics.ConstantAttributeFactory.String(value) : null)");
        AssertDoesNotContainAny(
            registrationSource,
            "SetAttribute(\"flag\", value != null",
            "SetAttribute(\"scale\", value != null",
            "SetAttribute(\"label\", value.HasValue");
    }

    [Fact]
    public void CustomValueLikeAttributeCanOptIntoNullableValueTypeAccess()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ValueLikeAttr : AnyIntegerAttrBase<AnyI32, \"value-like\">, MLIRNet_AttrExtension {",
                "  let csharpStorageType = \"global::MLIR.Dialects.Builtin.IntegerAttr\";",
                "  let csharpReturnType = \"global::My.ValueLike\";",
                "  let csharpConvertFromStorage = \"global::My.ValueLike.FromInteger($_self.Value)\";",
                "  let csharpConstBuilderCall = \"global::My.ValueLike.ToAttribute($0)\";",
                "  let csharpOptionalValueAccess = \"NullableValueType\";",
                "}",
                string.Empty,
                "def MiniArith_CustomOp : MiniArith_Op<\"custom\", []> {",
                "  let arguments = (ins MiniArith_ValueLikeAttr:$value);",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public global::My.ValueLike? Value",
            "set => SetAttribute(\"value\", value.HasValue ? global::My.ValueLike.ToAttribute(value.Value) : null)");
    }

    [Fact]
    public void CustomAttributeCanOptIntoPresenceBooleanRepresentation()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_PresenceAttr : AnyIntegerAttrBase<AnyI32, \"presence\">, MLIRNet_AttrExtension {",
                "  let csharpStorageType = \"global::MLIR.Dialects.Builtin.IntegerAttr\";",
                "  let csharpReturnType = \"bool\";",
                "  let csharpConvertFromStorage = \"$_self != null\";",
                "  let csharpPresenceAttributeValue = \"global::MLIR.Semantics.ConstantAttributeFactory.I32(1u)\";",
                "  let csharpOptionalValueAccess = \"NullableValueType\";",
                "  let csharpOptionalAttributeRepresentation = \"PresenceBoolean\";",
                "}",
                string.Empty,
                "def MiniArith_PresenceOp : MiniArith_Op<\"presence\", []> {",
                "  let arguments = (ins MiniArith_PresenceAttr:$enabled);",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public bool Enabled",
            "get => Attributes.Contains(\"enabled\")",
            "set => SetAttribute(\"enabled\", value ? global::MLIR.Semantics.ConstantAttributeFactory.I32(1u) : null)",
            "bool enabled,",
            "enabled ? new NamedAttribute(\"enabled\", global::MLIR.Semantics.ConstantAttributeFactory.I32(1u)) : null");
    }

    [Fact]
    public void AttributesPropertyHoldsDataAndNamedAttributeAccessorsAreDerived()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let summary = \"integer constant\";",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "};",
                string.Empty,
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {",
                "  let summary = \"integer addition\";",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public uint? Value",
            "get => Attributes.TryGet(\"value\",",
            "((global::MLIR.Dialects.Builtin.IntegerAttr)",
            "set => SetAttribute(\"value\", value.HasValue ? global::MLIR.Semantics.ConstantAttributeFactory.I32(value.Value) : null)",
            "NamedAttributeCollection attributes,",
            "uint? value,",
            "attributes: context.Attributes,",
            "attributes,");
        AssertDoesNotContainAny(
            registrationSource,
            "public override NamedAttributeCollection Attributes { get; }",
            "public NamedAttribute Value { get; }",
            "context.Attributes[\"value\"]");
    }

    [Fact]
    public void OptionalUnitAttributesGenerateBooleanPropertiesWhileRequiredUnitAttributesDoNot()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_FlagOp : MiniArith_Op<\"flag\", []> {",
                "  let arguments = (ins UnitAttr:$requiredFlag, UnitAttr:$optionalFlag);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$requiredFlag (`optional` $optionalFlag^)? attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public bool OptionalFlag",
            "get => Attributes.Contains(\"optionalFlag\")",
            "SetAttribute(\"optionalFlag\", value ? global::MLIR.Semantics.ConstantAttributeFactory.Unit : null)",
            "bool optionalFlag,",
            "public global::MLIR.Dialects.Builtin.UnitAttr RequiredFlag");
        AssertDoesNotContainAny(
            registrationSource,
            "public bool RequiredFlag");
    }

    [Fact]
    public void RequiredAttributeInAssemblyFormatGeneratesNonNullablePropertyAndRequiredRegistration()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let summary = \"integer constant\";",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$value attr-dict\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public uint Value",
            "((global::MLIR.Dialects.Builtin.IntegerAttr)",
            "new NamedAttribute(\"value\", global::MLIR.Semantics.ConstantAttributeFactory.I32(value))",
            "uint value,",
            "operation.RequiredAttribute(\"value\",");
        AssertDoesNotContainAny(
            registrationSource,
            "public uint? Value",
            "uint? value,",
            "operation.OptionalAttribute(\"value\")");
    }

    [Fact]
    public void OptionalAttributeInOptionalGroupGeneratesNullablePropertyAndOptionalRegistration()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_FlagOp : MyDialect_Op<\"flag\", []> {",
                "  let arguments = (ins I32Attr:$optAttr);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"($optAttr^)? attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public uint? OptAttr",
            "Attributes.TryGet(\"optAttr\",",
            "set => SetAttribute(\"optAttr\", value.HasValue",
            "operation.OptionalAttribute(\"optAttr\",");
        AssertDoesNotContainAny(
            registrationSource,
            "public uint OptAttr",
            "operation.RequiredAttribute(\"optAttr\")");
    }

    [Fact]
    public void RequiredOperandInAssemblyFormatGeneratesNonNullableProperty()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure]> {",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public Value Lhs",
            "get => base.Operands[0].Value!;",
            "set => SetOperand(0, value);",
            "public Value Rhs",
            "get => base.Operands[1].Value!;",
            "set => SetOperand(1, value);");
        AssertDoesNotContainAny(
            registrationSource,
            "public Value? Lhs",
            "public Value? Rhs");
    }

    [Fact]
    public void OptionalOperandInOptionalGroupGeneratesNullableProperty()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_BinaryOp : MyDialect_Op<\"binary\", []> {",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs (`,` $rhs^)? attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public Value Lhs",
            "get => base.Operands[0].Value!;",
            "set => SetOperand(0, value);",
            "public Value? Rhs",
            "get => base.Operands[1].Value;",
            "set => SetOperand(1, value);");
        AssertDoesNotContainAny(
            registrationSource,
            "public Value? Lhs",
            "public Value Rhs { get; }");
    }

    [Fact]
    public void OilistAttributesAreOptionalInRegistration()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_ConfigOp : MyDialect_Op<\"config\", []> {",
                "  let arguments = (ins I32Attr:$stride, I32Attr:$padding);",
                "  let assemblyFormat = \"oilist(`stride` $stride | `padding` $padding) attr-dict\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "operation.OptionalAttribute(\"stride\",",
            "operation.OptionalAttribute(\"padding\",",
            "public uint? Stride",
            "public uint? Padding");
        AssertDoesNotContainAny(
            registrationSource,
            "operation.RequiredAttribute(\"stride\")",
            "operation.RequiredAttribute(\"padding\")");
    }
}
