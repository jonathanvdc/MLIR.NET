namespace MLIR.Generators.Tests;

using Xunit;

public sealed class DialectGeneratorTypedAttributeTests : DialectGeneratorTestBase
{
    [Fact]
    public void AttributePropertyTypeIsNarrowedForKnownConstraintKinds()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_AddImmOp : MiniArith_Op<\"add_imm\", []> {",
                "  let arguments = (ins I32Attr:$intVal, BoolAttr:$boolVal, StrAttr:$strVal, F32Attr:$floatVal, F64Attr:$doubleVal);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$intVal `,` $boolVal `,` $strVal `,` $floatVal `,` $doubleVal attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public BigInteger IntVal",
            "I32AttrConstraintAttributeValue)Attributes[\"intVal\"].Value).Value;",
            "public bool BoolVal",
            "BoolAttrConstraintAttributeValue)Attributes[\"boolVal\"].Value).Value;",
            "public string StrVal",
            "StrAttrConstraintAttributeValue)Attributes[\"strVal\"].Value).Value;",
            "public float FloatVal",
            "F32AttrConstraintAttributeValue)Attributes[\"floatVal\"].Value).Value;",
            "public double DoubleVal",
            "F64AttrConstraintAttributeValue)Attributes[\"doubleVal\"].Value).Value;",
            "BigInteger intVal,",
            "bool boolVal,",
            "string strVal,",
            "float floatVal,",
            "double doubleVal,");
        AssertDoesNotContainAny(
            registrationSource,
            "public NamedAttribute IntVal",
            "public NamedAttribute BoolVal",
            "public NamedAttribute StrVal",
            "public NamedAttribute FloatVal",
            "public NamedAttribute DoubleVal",
            "NamedAttribute intVal,");
    }

    [Theory]
    [InlineData("DenseI32ArrayAttr", "indices", "BigInteger", "DenseIntegerArrayAttributeValue", "DenseIntegerArrayAttributeAssemblyFormat", "Indices")]
    [InlineData("DenseBoolArrayAttr", "flags", "bool", "DenseBooleanArrayAttributeValue", "DenseBooleanArrayAttributeAssemblyFormat", "Flags")]
    [InlineData("DenseF32ArrayAttr", "coeffs", "float", "DenseF32ArrayAttributeValue", "DenseF32ArrayAttributeAssemblyFormat", "Coeffs")]
    [InlineData("DenseF64ArrayAttr", "weights", "double", "DenseF64ArrayAttributeValue", "DenseF64ArrayAttributeAssemblyFormat", "Weights")]
    public void GeneratesDenseArrayAttributePropertyWithTypedItemsList(
        string constraintType,
        string attributeName,
        string elementType,
        string attributeValueType,
        string assemblyFormatType,
        string propertyName)
    {
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new MLIR.Generators.DialectGenerator(),
            (
                "mydialect.td",
                ComposeMyDialectSource(
                    [
                        "def MyDialect_TestOp : MyDialect_Op<\"test\", []> {",
                        $"  let arguments = (ins {constraintType}:${attributeName}, I32:$lhs);",
                        "  let results = (outs I32:$result);",
                        $"  let assemblyFormat = \"${attributeName} `,` $lhs attr-dict `:` type($result)\";",
                        "};",
                    ])));

        var preludeSource = Assert.Single(
            generatedSources.Where(static result => result.HintName == "PreludeDialectRegistration.g.cs")).SourceText.ToString();
        var registrationSource = Assert.Single(
            generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        AssertContainsAll(
            registrationSource,
            $"public IReadOnlyList<{elementType}> {propertyName}",
            $"IReadOnlyList<{elementType}> {attributeName},");
        AssertContainsAll(
            preludeSource,
            attributeValueType,
            assemblyFormatType);
    }

    [Fact]
    public void GeneratesTypedArrayAttributePropertyWithRecursiveItemTypes()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_TypedArrayOp : MyDialect_Op<\"typed_array\", []> {",
                "  let arguments = (ins StrArrayAttr:$strings, TypeArrayAttr:$types, DictArrayAttr:$dicts, IndexListArrayAttr:$indexLists, I32:$input);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$strings `,` $types `,` $dicts `,` $indexLists `,` $input attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public IReadOnlyList<string> Strings",
            "public IReadOnlyList<TypeSyntax> Types",
            "public IReadOnlyList<NamedAttributeCollection> Dicts",
            "public IReadOnlyList<IReadOnlyList<BigInteger>> IndexLists",
            "IReadOnlyList<string> strings,",
            "IReadOnlyList<TypeSyntax> types,",
            "IReadOnlyList<NamedAttributeCollection> dicts,",
            "IReadOnlyList<IReadOnlyList<BigInteger>> indexLists,",
            "StrArrayAttrConstraintAttributeValue",
            "TypeArrayAttrConstraintAttributeValue",
            "DictArrayAttrConstraintAttributeValue",
            "IndexListArrayAttrConstraintAttributeValue");
        AssertDoesNotContainAny(
            registrationSource,
            "public NamedAttribute Strings",
            "public NamedAttribute Types",
            "public NamedAttribute Dicts",
            "public NamedAttribute IndexLists");
    }

    [Fact]
    public void GeneratesTypedEnumAttributesAndOperationsWithoutDuplicateEnumDeclarations()
    {
        var registrationSource = GenerateRegistrationSource(
            "minienum.td",
            "MinienumDialectRegistration.g.cs",
            ComposeSource(
                [
                    "include \"mlir/IR/EnumAttr.td\"",
                    string.Empty,
                    "class MiniEnum_Op<string mnemonic, list<Trait> traits = []> : Op<MiniEnum_Dialect, mnemonic, traits>;",
                    "def MiniEnum_Dialect : Dialect {",
                    "  let name = \"minienum\";",
                    "  let cppNamespace = \"::mlir::minienum\";",
                    "};",
                    "def MINI_MODE_A : I32EnumAttrCase<\"a\", 0>;",
                    "def MINI_MODE_B : I32EnumAttrCase<\"b\", 1>;",
                    "def MiniEnum_Mode : I32EnumAttr<\"Mode\", \"mode summary\", [MINI_MODE_A, MINI_MODE_B]> {",
                    "  let cppNamespace = \"::mlir::minienum\";",
                    "  let genSpecializedAttr = 0;",
                    "};",
                    "def MiniEnum_ModeAttr : EnumAttr<MiniEnum_Dialect, MiniEnum_Mode, \"mode\"> {",
                    "  let assemblyFormat = \"`<` $value `>`\";",
                    "};",
                    "def MINI_FLAG_NONE : I32BitEnumAttrCaseNone<\"none\">;",
                    "def MINI_FLAG_X : I32BitEnumAttrCaseBit<\"x\", 0>;",
                    "def MINI_FLAG_Y : I32BitEnumAttrCaseBit<\"y\", 1>;",
                    "def MINI_FLAG_XY : I32BitEnumAttrCaseGroup<\"xy\", [MINI_FLAG_X, MINI_FLAG_Y]>;",
                    "def MiniEnum_Flags : I32BitEnumAttr<\"Flags\", \"flags summary\", [MINI_FLAG_NONE, MINI_FLAG_X, MINI_FLAG_Y, MINI_FLAG_XY]> {",
                    "  let separator = \",\";",
                    "  let cppNamespace = \"::mlir::minienum\";",
                    "  let genSpecializedAttr = 0;",
                    "  let printBitEnumPrimaryGroups = 1;",
                    "};",
                    "def MiniEnum_FlagsAttr : EnumAttr<MiniEnum_Dialect, MiniEnum_Flags, \"flags\"> {",
                    "  let assemblyFormat = \"`<` $value `>`\";",
                    "};",
                    "def MiniEnum_ModeOp : MiniEnum_Op<\"mode_op\", [Pure]> {",
                    "  let arguments = (ins MiniEnum_ModeAttr:$mode, I32:$input);",
                    "  let results = (outs I32:$result);",
                    "  let assemblyFormat = \"$mode `,` $input attr-dict `:` type($result)\";",
                    "};",
                    "def MiniEnum_FlagsOp : MiniEnum_Op<\"flags_op\", [Pure]> {",
                    "  let arguments = (ins MiniEnum_FlagsAttr:$flags, I32:$input);",
                    "  let results = (outs I32:$result);",
                    "  let assemblyFormat = \"$flags $input attr-dict `:` type($result)\";",
                    "};",
                ]));

        Assert.Equal(1, CountOccurrences(registrationSource, "public enum Mode : uint"));
        Assert.Equal(1, CountOccurrences(registrationSource, "public enum Flags : uint"));
        AssertContainsAll(
            registrationSource,
            "[global::System.Flags]",
            "internal static class ModeInfo",
            "internal static class FlagsInfo",
            "public MLIR.Minienum.Mode Mode",
            "get => ((MLIR.Minienum.ModeAttr)Attributes[\"mode\"].Value).TypedValue;",
            "public MLIR.Minienum.Flags Flags",
            "new MLIR.Minienum.FlagsAttr(value)",
            "return string.Join(\",\", parts);");
    }
}
