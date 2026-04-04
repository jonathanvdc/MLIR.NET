namespace MLIR.Generators.Tests;

using Xunit;

public sealed class DialectGeneratorOptionalityTests : DialectGeneratorTestBase
{
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
            "public BigInteger? Value",
            "get => Attributes.TryGet(\"value\",",
            "set => SetAttribute(\"value\", value.HasValue",
            "NamedAttributeCollection attributes,",
            "BigInteger? value,",
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
            "SetAttribute(\"optionalFlag\", value ? new NamedAttribute(\"optionalFlag\", new UnknownAttributeValue(",
            "new UnitAttributeValueSyntax(new SyntaxToken(\"unit\"))",
            "bool optionalFlag,");
        AssertDoesNotContainAny(
            registrationSource,
            "public UnitAttributeValue RequiredFlag",
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
            "public BigInteger Value",
            "I32AttrConstraintAttributeValue)Attributes[\"value\"].Value).Value;",
            "new NamedAttribute(\"value\", new",
            "BigInteger value,",
            "operation.RequiredAttribute(\"value\",");
        AssertDoesNotContainAny(
            registrationSource,
            "public BigInteger? Value",
            "BigInteger? value,",
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
            "public BigInteger? OptAttr",
            "Attributes.TryGet(\"optAttr\",",
            "set => SetAttribute(\"optAttr\", value.HasValue",
            "operation.OptionalAttribute(\"optAttr\",");
        AssertDoesNotContainAny(
            registrationSource,
            "public BigInteger OptAttr",
            "operation.RequiredAttribute(\"optAttr\")");
    }

    [Fact]
    public void RequiredOperandInAssemblyFormatGeneratesOpOperandProperty()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure]> {",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";",
                "};",
            ]);

        // Non-variadic operands are now exposed as OpOperand (the owning slot), with no setter.
        // The slot always exists; callers access .Value to read or write the SSA value.
        AssertContainsAll(
            registrationSource,
            "public OpOperand Lhs => base.Operands[0];",
            "public OpOperand Rhs => base.Operands[1];");
        AssertDoesNotContainAny(
            registrationSource,
            "public Value Lhs",
            "public Value? Lhs",
            "public Value Rhs",
            "public Value? Rhs");
    }

    [Fact]
    public void OptionalOperandInOptionalGroupAlsoGeneratesOpOperandProperty()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_BinaryOp : MyDialect_Op<\"binary\", []> {",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs (`,` $rhs^)? attr-dict `:` type($result)\";",
                "};",
            ]);

        // Optional operands still produce an OpOperand property—the slot always exists.
        // Whether the value is present is indicated by OpOperand.Value being non-null.
        AssertContainsAll(
            registrationSource,
            "public OpOperand Lhs => base.Operands[0];",
            "public OpOperand Rhs => base.Operands[1];");
        AssertDoesNotContainAny(
            registrationSource,
            "public Value? Lhs",
            "public Value Rhs { get; }",
            "public Value Lhs",
            "public Value Rhs");
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
            "public BigInteger? Stride",
            "public BigInteger? Padding");
        AssertDoesNotContainAny(
            registrationSource,
            "operation.RequiredAttribute(\"stride\")",
            "operation.RequiredAttribute(\"padding\")");
    }
}
