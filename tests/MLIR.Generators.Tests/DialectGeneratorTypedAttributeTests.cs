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
                "  let arguments = (ins I32Attr:$intVal, BoolAttr:$boolVal, StrAttr:$strVal, F32Attr:$floatVal, F64Attr:$doubleVal, AnyAttr:$fallback);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$intVal `,` $boolVal `,` $strVal `,` $floatVal `,` $doubleVal `,` $fallback attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public uint IntVal",
            "((global::MLIR.Dialects.Builtin.IntegerAttr)Attributes[\"intVal\"].Value).Value.ToUInt64()",
            "public bool BoolVal",
            "((global::MLIR.Dialects.Builtin.IntegerAttr)Attributes[\"boolVal\"].Value).Value.ToUInt64() != 0",
            "public string StrVal",
            "((global::MLIR.Dialects.Builtin.StringAttr)Attributes[\"strVal\"].Value).Value",
            "public global::MLIR.Numerics.ApFloat FloatVal",
            "((global::MLIR.Dialects.Builtin.FloatAttr)Attributes[\"floatVal\"].Value).Value",
            "public global::MLIR.Numerics.ApFloat DoubleVal",
            "((global::MLIR.Dialects.Builtin.FloatAttr)Attributes[\"doubleVal\"].Value).Value",
            " Fallback",
            "Attributes[\"fallback\"].Value;",
            "SetAttribute(\"fallback\", value);",
            "SetAttribute(\"intVal\", global::MLIR.Semantics.ConstantAttributeFactory.I32(value));",
            "SetAttribute(\"floatVal\", global::MLIR.Semantics.ConstantAttributeFactory.F32(value));",
            "SetAttribute(\"doubleVal\", global::MLIR.Semantics.ConstantAttributeFactory.F64(value));",
            "uint intVal,",
            "bool boolVal,",
            "string strVal,",
            "global::MLIR.Numerics.ApFloat floatVal,",
            "global::MLIR.Numerics.ApFloat doubleVal,");
        AssertDoesNotContainAny(
            registrationSource,
            "public NamedAttribute IntVal",
            "public NamedAttribute BoolVal",
            "public NamedAttribute StrVal",
            "public NamedAttribute FloatVal",
            "public NamedAttribute DoubleVal",
            "public NamedAttribute Fallback",
            "NamedAttribute intVal,");
    }

    [Theory]
    [InlineData("DenseI32ArrayAttr", "indices", "global::System.ReadOnlySpan<int>", "DenseI32ArrayAttrConstraintAttributeValue", "DenseIntegerArrayAttributeAssemblyFormat", "DenseI32", "Indices")]
    [InlineData("DenseBoolArrayAttr", "flags", "global::System.ReadOnlySpan<bool>", "DenseBoolArrayAttrConstraintAttributeValue", "DenseBooleanArrayAttributeAssemblyFormat", "DenseBool", "Flags")]
    [InlineData("DenseF32ArrayAttr", "coeffs", "global::System.ReadOnlySpan<float>", "DenseF32ArrayAttrConstraintAttributeValue", "DenseFloatingPointArrayAttributeAssemblyFormat", "DenseF32", "Coeffs")]
    [InlineData("DenseF64ArrayAttr", "weights", "global::System.ReadOnlySpan<double>", "DenseF64ArrayAttrConstraintAttributeValue", "DenseFloatingPointArrayAttributeAssemblyFormat", "DenseF64", "Weights")]
    public void GeneratesDenseArrayAttributePropertyWithTypedItemsList(
        string constraintType,
        string attributeName,
        string propertyType,
        string constraintClassName,
        string assemblyFormatType,
        string constantFactoryMethodName,
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
            $"public {propertyType} {propertyName}",
            $"{propertyType} {attributeName},",
            $"global::MLIR.Semantics.ConstantAttributeFactory.{constantFactoryMethodName}(value)");
        AssertContainsAll(
            preludeSource,
            $"public static class {constraintClassName}",
            assemblyFormatType);
        AssertDoesNotContainAny(
            preludeSource,
            $"public sealed class {constraintClassName} :",
            "DenseBooleanArrayAttributeValue",
            "DenseIntegerArrayAttributeValue",
            "DenseFloatingPointArrayAttributeValue");
        Assert.DoesNotContain(constraintClassName + ".Create(", registrationSource, System.StringComparison.Ordinal);
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
            "IReadOnlyList<string> Strings",
            "IReadOnlyList<global::MLIR.Semantics.TypeReference> Types",
            "IReadOnlyList<global::MLIR.Semantics.NamedAttributeCollection> Dicts",
            "IReadOnlyList<string> strings,",
            "IReadOnlyList<global::MLIR.Semantics.TypeReference> types,",
            "IReadOnlyList<global::MLIR.Semantics.NamedAttributeCollection> dicts,",
            "StrArrayAttrConstraintAttributeValue",
            "TypeArrayAttrConstraintAttributeValue",
            "DictArrayAttrConstraintAttributeValue");
        AssertDoesNotContainAny(
            registrationSource,
            "public NamedAttribute Strings",
            "public NamedAttribute Types",
            "public NamedAttribute Dicts",
            "public NamedAttribute IndexLists");
    }

    [Fact]
    public void TypedArrayAttrConstraintsAreGeneratedAsConstraintOnlyStaticClasses()
    {
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new MLIR.Generators.DialectGenerator(),
            (
                "mydialect.td",
                ComposeMyDialectSource(
                    [
                        "def MyDialect_ArrayConstraintOp : MyDialect_Op<\"array_constraint\", []> {",
                        "  let arguments = (ins I32ArrayAttr:$ints, StrArrayAttr:$strings, IndexListArrayAttr:$indexLists, I32:$input);",
                        "  let results = (outs I32:$result);",
                        "  let assemblyFormat = \"$ints `,` $strings `,` $indexLists `,` $input attr-dict `:` type($result)\";",
                        "};",
                    ])));

        var preludeSource = Assert.Single(
            generatedSources.Where(static result => result.HintName == "PreludeDialectRegistration.g.cs")).SourceText.ToString();
        var registrationSource = Assert.Single(
            generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        AssertContainsAll(
            preludeSource,
            "public static class I32ArrayAttrConstraintAttributeValue",
            "public static class StrArrayAttrConstraintAttributeValue",
            "public static class IndexListArrayAttrConstraintAttributeValue");
        AssertDoesNotContainAny(
            preludeSource,
            "public sealed class I32ArrayAttrConstraintAttributeValue :",
            "public sealed class StrArrayAttrConstraintAttributeValue :",
            "public sealed class IndexListArrayAttrConstraintAttributeValue :",
            "TypedArrayAttributeValue<",
            "private sealed class Value :");
        AssertDoesNotContainAny(
            preludeSource,
            "I32ArrayAttrConstraintAttributeValue.Create(",
            "I32ArrayAttrConstraintAttributeValue.GetItems(",
            "StrArrayAttrConstraintAttributeValue.Create(",
            "StrArrayAttrConstraintAttributeValue.GetItems(",
            "IndexListArrayAttrConstraintAttributeValue.Create(",
            "IndexListArrayAttrConstraintAttributeValue.GetItems(");
        const int expectedTypedArrayConstraints = 3; // I32ArrayAttr, StrArrayAttr, and IndexListArrayAttr from this test dialect.
        Assert.True(
            CountOccurrences(
                preludeSource,
                "new global::MLIR.Dialects.Attributes.Collections.TypedArrayAttributeAssemblyFormat()") >= expectedTypedArrayConstraints);
        AssertDoesNotContainAny(
            preludeSource,
            "internal sealed class I32ArrayAttrConstraintAttributeValueAssemblyFormat",
            "internal sealed class StrArrayAttrConstraintAttributeValueAssemblyFormat",
            "internal sealed class IndexListArrayAttrConstraintAttributeValueAssemblyFormat",
            "TypedArrayAttributeAssemblyFormat<");
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
            "public MLIR.Dialects.Minienum.Mode Mode",
            "public MLIR.Dialects.Minienum.Flags Flags",
            "new MLIR.Dialects.Minienum.FlagsAttr(value)",
            "return string.Join(\",\", parts);");
        Assert.True(
            registrationSource.Contains("Attributes[\"mode\"].Value).TypedValue;", System.StringComparison.Ordinal)
            || registrationSource.Contains("Attributes[\"mode\"].Value).Value;", System.StringComparison.Ordinal));
    }

    [Fact]
    public void PureEnumConstraintsUsedByOperationDoNotGenerateEnumAttrWrapperClass()
    {
        var registrationSource = GenerateRegistrationSource(
            "myarith.td",
            "MyarithDialectRegistration.g.cs",
            ComposeSource(
                [
                    "include \"mlir/IR/EnumAttr.td\"",
                    string.Empty,
                    "class MyArith_Op<string mnemonic, list<Trait> traits = []> : Op<MyArith_Dialect, mnemonic, traits>;",
                    "def MyArith_Dialect : Dialect {",
                    "  let name = \"myarith\";",
                    "  let cppNamespace = \"::mlir::myarith\";",
                    "};",
                    "def MYARITH_PRED_EQ : I32EnumAttrCase<\"eq\", 0>;",
                    "def MYARITH_PRED_NE : I32EnumAttrCase<\"ne\", 1>;",
                    "def MyArith_CmpPredicate : I32EnumAttr<\"CmpPredicate\", \"cmp predicate\", [MYARITH_PRED_EQ, MYARITH_PRED_NE]> {",
                    "  let cppNamespace = \"::mlir::myarith\";",
                    "};",
                    "def MYARITH_FLAG_NONE : I32BitEnumAttrCaseNone<\"none\">;",
                    "def MYARITH_FLAG_X : I32BitEnumAttrCaseBit<\"x\", 0>;",
                    "def MYARITH_FLAG_Y : I32BitEnumAttrCaseBit<\"y\", 1>;",
                    "def MyArith_CmpMask : I32BitEnumAttr<\"CmpMask\", \"cmp mask\", [MYARITH_FLAG_NONE, MYARITH_FLAG_X, MYARITH_FLAG_Y]> {",
                    "  let cppNamespace = \"::mlir::myarith\";",
                    "};",
                    "def MyArith_CmpOp : MyArith_Op<\"cmp\", []> {",
                    "  let arguments = (ins MyArith_CmpPredicate:$predicate, MyArith_CmpMask:$mask, I32:$lhs, I32:$rhs);",
                    "  let results = (outs I1:$result);",
                    "  let assemblyFormat = \"$predicate `,` $mask `,` $lhs `,` $rhs attr-dict `:` type($result)\";",
                    "};",
                ]));

        AssertContainsAll(
            registrationSource,
            "public enum CmpPredicate : uint",
            "public enum CmpMask : uint",
            "public MLIR.Dialects.Myarith.CmpPredicate Predicate",
            "public MLIR.Dialects.Myarith.CmpMask Mask",
            "MyArithCmpPredicateConstraintAttributeValue",
            "MyArithCmpMaskConstraintAttributeValue",
            "((global::MLIR.Dialects.Builtin.IntegerAttr)Attributes[\"predicate\"].Value).Value",
            "((global::MLIR.Dialects.Builtin.IntegerAttr)Attributes[\"mask\"].Value).Value",
            "SetAttribute(\"predicate\", new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I32, global::MLIR.Numerics.ApInt.FromUInt64(32, (ulong)value), null))",
            "SetAttribute(\"mask\", new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I32, global::MLIR.Numerics.ApInt.FromUInt64(32, (ulong)value), null))",
            "return new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I32, global::MLIR.Numerics.ApInt.FromUInt64(32, (ulong)ParseEnumValue(syntax)), syntax)");

        AssertDoesNotContainAny(
            registrationSource,
            "public sealed class MyArithCmpPredicateConstraintAttributeValue :",
            "public sealed class MyArithCmpMaskConstraintAttributeValue :",
            "public sealed class CmpPredicateAttr : AttributeValue",
            "public sealed class CmpMaskAttr : AttributeValue",
            "new MLIR.Dialects.Myarith.CmpPredicateAttr(value)",
            "new MLIR.Dialects.Myarith.CmpMaskAttr(value)",
            "new MLIR.Dialects.Myarith.MyArithCmpPredicateConstraintAttributeValue(value)",
            "new MLIR.Dialects.Myarith.MyArithCmpMaskConstraintAttributeValue(value)",
            "((MLIR.Dialects.Myarith.CmpPredicateAttr)Attributes[\"predicate\"].Value).",
            "((MLIR.Dialects.Myarith.CmpMaskAttr)Attributes[\"mask\"].Value).");
    }

    [Fact]
    public void EnumAttrDefinitionsStillGenerateDedicatedSemanticAttributeClass()
    {
        var registrationSource = GenerateRegistrationSource(
            "myarith.td",
            "MyarithDialectRegistration.g.cs",
            ComposeSource(
                [
                    "include \"mlir/IR/EnumAttr.td\"",
                    string.Empty,
                    "class MyArith_Op<string mnemonic, list<Trait> traits = []> : Op<MyArith_Dialect, mnemonic, traits>;",
                    "def MyArith_Dialect : Dialect {",
                    "  let name = \"myarith\";",
                    "  let cppNamespace = \"::mlir::myarith\";",
                    "};",
                    "def MYARITH_PRED_EQ : I64EnumAttrCase<\"eq\", 0>;",
                    "def MYARITH_PRED_NE : I64EnumAttrCase<\"ne\", 1>;",
                    "def MyArith_CmpPredicate : I64EnumAttr<\"CmpPredicate\", \"cmp predicate\", [MYARITH_PRED_EQ, MYARITH_PRED_NE]> {",
                    "  let cppNamespace = \"::mlir::myarith\";",
                    "};",
                    "def MyArith_CmpPredicateAttr : EnumAttr<MyArith_Dialect, MyArith_CmpPredicate, \"cmp_predicate\"> {",
                    "  let assemblyFormat = \"`<` $value `>`\";",
                    "};",
                    "def MyArith_CmpOp : MyArith_Op<\"cmp\", []> {",
                    "  let arguments = (ins MyArith_CmpPredicateAttr:$predicate, I32:$lhs, I32:$rhs);",
                    "  let results = (outs I1:$result);",
                    "  let assemblyFormat = \"$predicate `,` $lhs `,` $rhs attr-dict `:` type($result)\";",
                    "};",
                ]));

        AssertContainsAll(
            registrationSource,
            "public sealed class CmpPredicateAttr : AttributeValue",
            "public MLIR.Dialects.Myarith.CmpPredicate Predicate",
            "new MLIR.Dialects.Myarith.CmpPredicateAttr(value)");
        Assert.True(
            registrationSource.Contains("Attributes[\"predicate\"].Value).TypedValue;", System.StringComparison.Ordinal)
            || registrationSource.Contains("Attributes[\"predicate\"].Value).Value;", System.StringComparison.Ordinal));

        AssertDoesNotContainAny(
            registrationSource,
            "((global::MLIR.Dialects.Builtin.IntegerAttr)Attributes[\"predicate\"].Value).Value.ToUInt64()");
    }

    [Fact]
    public void EnumConstraintWithCppNamespaceIsPlacedInMatchingDialectNamespace()
    {
        // This covers the case from the issue: an I64EnumAttr (attribute constraint, not an
        // AttrDef wrapper) whose cppNamespace matches the dialect.  The generated enum type and
        // constraint class should appear in the dialect namespace, not the prelude.
        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new MLIR.Generators.DialectGenerator(),
            (
                "myarith.td",
                ComposeSource(
                    [
                        "include \"mlir/IR/EnumAttr.td\"",
                        string.Empty,
                        "class MyArith_Op<string mnemonic, list<Trait> traits = []> : Op<MyArith_Dialect, mnemonic, traits>;",
                        "def MyArith_Dialect : Dialect {",
                        "  let name = \"myarith\";",
                        "  let cppNamespace = \"::mlir::myarith\";",
                        "};",
                        "def MyArith_CmpPredicateAttr : I64EnumAttr<",
                        "    \"CmpPredicate\", \"\",",
                        "    [",
                        "      I64EnumAttrCase<\"eq\", 0, \"eq\">,",
                        "      I64EnumAttrCase<\"ne\", 1, \"ne\">,",
                        "    ]> {",
                        "  let cppNamespace = \"::mlir::myarith\";",
                        "};",
                        "def MyArith_CmpOp : MyArith_Op<\"cmp\", []> {",
                        "  let arguments = (ins MyArith_CmpPredicateAttr:$predicate, I32:$lhs, I32:$rhs);",
                        "  let results = (outs I1:$result);",
                        "  let assemblyFormat = \"$predicate `,` $lhs `,` $rhs attr-dict `:` type($result)\";",
                        "};",
                    ])));

        var preludeSource = System.Linq.Enumerable.Single(
            generatedSources,
            static result => result.HintName == "PreludeDialectRegistration.g.cs").SourceText.ToString();
        var dialectSource = System.Linq.Enumerable.Single(
            generatedSources,
            static result => result.HintName == "MyarithDialectRegistration.g.cs").SourceText.ToString();

        // The enum and its constraint class must be in the dialect file, not the prelude.
        AssertContainsAll(
            dialectSource,
            "public enum CmpPredicate : ulong",
            "internal static class CmpPredicateInfo",
            "public static class MyArithCmpPredicateAttrConstraintAttributeValue",
            "internal sealed class MyArithCmpPredicateAttrConstraintAttributeValueAssemblyFormat",
            "return new global::MLIR.Dialects.Builtin.IntegerAttr(global::MLIR.Semantics.TypeFactory.I64, global::MLIR.Numerics.ApInt.FromUInt64(64, (ulong)ParseEnumValue(syntax)), syntax);");

        // The operation property must use the fully-qualified dialect type.
        AssertContainsAll(
            dialectSource,
            "public MLIR.Dialects.Myarith.CmpPredicate Predicate");

        // The prelude must not contain the enum or its constraint.
        AssertDoesNotContainAny(
            preludeSource,
            "CmpPredicate",
            "MyArithCmpPredicateAttr");
    }

    [Fact]
    public void AttrDefWithStringParameterAndAssemblyFormatGeneratesSyntaxAndValueClasses()
    {
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "def TestDialect : Dialect {",
            "  let name = \"test\";",
            "  let cppNamespace = \"::mlir::test\";",
            "};",
            string.Empty,
            "class Test_Attr<string name, string m> : AttrDef<TestDialect, name> {",
            "  let mnemonic = m;",
            "}",
            string.Empty,
            "def Test_FooAttr : Test_Attr<\"Foo\", \"foo\"> {",
            "  let parameters = (ins StringRefParameter<\"the opaque value\">:$value);",
            "  let assemblyFormat = \"`<` $value `>`\";",
            "}",
        ]);

        var registrationSource = GenerateRegistrationSource("test.td", "TestDialectRegistration.g.cs", source);

        // Syntax class
        AssertContainsAll(
            registrationSource,
            "public sealed class FooAttrSyntax : DialectPrefixedAttributeValueSyntax",
            "public StringAttributeValueSyntax ValueSyntax { get; }",
            "ValueSyntax.WriteTo(writer)");

        // Attribute value class
        AssertContainsAll(
            registrationSource,
            "public sealed class FooAttr : AttributeValue",
            "public static AttributeDefinition AttributeDefinition",
            "new FooAttrAssemblyFormat()",
            "public string Value { get; }",
            "public FooAttr(string value, MLIR.Syntax.AttributeValueSyntax? syntax = null)");

        // Assembly format class
        AssertContainsAll(
            registrationSource,
            // Body-only marker so the parser strips '#name' before calling TryParse
            "internal sealed class FooAttrAssemblyFormat : IBodyOnlyAttributeAssemblyFormat",
            "ParseResult<AttributeValueSyntax> TryParse",
            // StringRefParameter.csharpParser delegates to the string-literal helper
            "context.TryParseStringLiteralSyntax()",
            "BindValue(AttributeValueSyntax syntax, Binder binder)",
            "AttributeValue Bind(",
            "AttributeValueSyntax BuildCustomAssemblySyntax(",
            // StringRefParameter.csharpPrinter wraps the string value in a quoted literal
            "StringLiteralAttributeAssemblyFormat.Quote(attr.Value)");
    }

    [Fact]
    public void AttrDefWithSelfTypeParameterAndAssemblyFormatBindsTypeReferencesThroughBinder()
    {
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "def TestDialect : Dialect {",
            "  let name = \"test\";",
            "  let cppNamespace = \"::mlir::test\";",
            "};",
            string.Empty,
            "class Test_Attr<string name, string m> : AttrDef<TestDialect, name> {",
            "  let mnemonic = m;",
            "}",
            string.Empty,
            "def Test_SelfAttr : Test_Attr<\"Self\", \"self\"> {",
            "  let parameters = (ins AttributeSelfTypeParameter<\"\">:$type);",
            "  let assemblyFormat = \"`<` $type `>`\";",
            "}",
        ]);

        var registrationSource = GenerateRegistrationSource("test.td", "TestDialectRegistration.g.cs", source);

        AssertContainsAll(
            registrationSource,
            "public global::MLIR.Semantics.TypeReference Type { get; }",
            "public SelfAttr(global::MLIR.Semantics.TypeReference type, MLIR.Syntax.AttributeValueSyntax? syntax = null)",
            "new AttributeDefinition(\"test.self\", new SelfAttrAssemblyFormat())",
            "BindValue(AttributeValueSyntax syntax, Binder binder)",
            "binder.BindTypeReference(structured.TypeSyntax.TypeSyntax)");
    }

    [Fact]
    public void AttrDefWithParametersAndNoAssemblyFormatStillGeneratesTypedAttributeClass()
    {
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "def TestDialect : Dialect {",
            "  let name = \"test\";",
            "  let cppNamespace = \"::mlir::test\";",
            "};",
            string.Empty,
            "class Test_Attr<string name, string m> : AttrDef<TestDialect, name> {",
            "  let mnemonic = m;",
            "}",
            string.Empty,
            "def Test_LabelAttr : Test_Attr<\"Label\", \"label\"> {",
            "  let parameters = (ins StringRefParameter<\"the label\">:$value);",
            "}",
        ]);

        var registrationSource = GenerateRegistrationSource("test.td", "TestDialectRegistration.g.cs", source);

        AssertContainsAll(
            registrationSource,
            "public sealed class LabelAttr : AttributeValue",
            "public static AttributeDefinition AttributeDefinition { get; } =",
            "new AttributeDefinition(\"test.label\")",
            "public string Value { get; }",
            "public LabelAttr(string value, MLIR.Syntax.AttributeValueSyntax? syntax = null)",
            ": base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)",
            "Value = value;");

        AssertDoesNotContainAny(
            registrationSource,
            "LabelAttrSyntax",
            "LabelAttrAssemblyFormat",
            "BindValueParam(",
            "BindValue(AttributeValueConstructionContext context)",
            "factory: static context =>");
    }

    [Fact]
    public void AttrDefWithApIntParametersAndAssemblyFormatGeneratesTwoProperties()
    {
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "def TestDialect : Dialect {",
            "  let name = \"test\";",
            "  let cppNamespace = \"::mlir::test\";",
            "};",
            string.Empty,
            "class Test_Attr<string name, string m> : AttrDef<TestDialect, name> {",
            "  let mnemonic = m;",
            "}",
            string.Empty,
            "def Test_PairAttr : Test_Attr<\"Pair\", \"pair\"> {",
            "  let parameters = (ins APIntParameter<\"first\">:$first,",
            "                        APIntParameter<\"second\">:$second);",
            "  let assemblyFormat = \"`<` $first `,` $second `>`\";",
            "}",
        ]);

        var registrationSource = GenerateRegistrationSource("test.td", "TestDialectRegistration.g.cs", source);

        // Syntax class
        AssertContainsAll(
            registrationSource,
            "public sealed class PairAttrSyntax : DialectPrefixedAttributeValueSyntax",
            "public IntegerAttributeValueSyntax FirstSyntax { get; }",
            "public IntegerAttributeValueSyntax SecondSyntax { get; }");

        // Attribute value class
        AssertContainsAll(
            registrationSource,
            "public sealed class PairAttr : AttributeValue",
            "public global::MLIR.Numerics.ApInt First { get; }",
            "public global::MLIR.Numerics.ApInt Second { get; }",
            "public PairAttr(global::MLIR.Numerics.ApInt first, global::MLIR.Numerics.ApInt second, MLIR.Syntax.AttributeValueSyntax? syntax = null)");

        // Assembly format class uses the APIntParameter.csharpParser helper for both parameters
        AssertContainsAll(
            registrationSource,
            // APIntParameter.csharpParser delegates to the integer-literal helper
            "context.TryParseIntegerLiteralSyntax()");
    }

    [Fact]
    public void AttrDefWithCsharpParametersStringLiteralOverridesInferredType()
    {
        // When csharpParameters provides a string literal for a parameter, the generated
        // C# property uses that type instead of what would be inferred from the parameters dag.
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "def TestDialect : Dialect {",
            "  let name = \"test\";",
            "  let cppNamespace = \"::mlir::test\";",
            "};",
            string.Empty,
            "class Test_Attr<string name, string m> : AttrDef<TestDialect, name> {",
            "  let mnemonic = m;",
            "}",
            string.Empty,
            "def Test_SizedAttr : Test_Attr<\"Sized\", \"sized\"> {",
            "  let parameters = (ins APIntParameter<\"the width\">:$width);",
            "  let assemblyFormat = \"`<` $width `>`\";",
            "}",
            string.Empty,
            // Override: use ulong instead of the default ApInt type.
            "extends Test_SizedAttr : MLIRNet_AttrOrTypeDefExtension {",
            "  let csharpParameters = (ins \"ulong\":$width);",
            "}",
        ]);

        var registrationSource = GenerateRegistrationSource("test.td", "TestDialectRegistration.g.cs", source);

        // The property type must be the overridden C# type.
        AssertContainsAll(
            registrationSource,
            "public ulong Width { get; }",
            "public SizedAttr(ulong width, MLIR.Syntax.AttributeValueSyntax? syntax = null)");

        // The inferred ApInt type must not appear.
        AssertDoesNotContainAny(
            registrationSource,
            "public global::MLIR.Numerics.ApInt Width",
            "global::MLIR.Numerics.ApInt width,");
    }

    [Fact]
    public void AttrDefWithCsharpParametersRecordEntryUsesExtensionMetadata()
    {
        // When csharpParameters provides a parameter class instance, the generated code uses
        // the C# metadata from that class's MLIRNet_AttrOrTypeParameterExtension annotations.
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "def TestDialect : Dialect {",
            "  let name = \"test\";",
            "  let cppNamespace = \"::mlir::test\";",
            "};",
            string.Empty,
            "class Test_Attr<string name, string m> : AttrDef<TestDialect, name> {",
            "  let mnemonic = m;",
            "}",
            string.Empty,
            // Use a plain C++ string parameter in the upstream parameters dag...
            "def Test_LabelAttr : Test_Attr<\"Label\", \"label\"> {",
            "  let parameters = (ins \"std::string\":$label);",
            "  let assemblyFormat = \"`<` $label `>`\";",
            "}",
            string.Empty,
            // ...but map it to the richer StringRefParameter C# metadata via csharpParameters.
            "extends Test_LabelAttr : MLIRNet_AttrOrTypeDefExtension {",
            "  let csharpParameters = (ins StringRefParameter<\"the label\">:$label);",
            "}",
        ]);

        var registrationSource = GenerateRegistrationSource("test.td", "TestDialectRegistration.g.cs", source);

        // The C# type and syntax type come from StringRefParameter's extension metadata.
        AssertContainsAll(
            registrationSource,
            "public string Label { get; }",
            "public LabelAttr(string label, MLIR.Syntax.AttributeValueSyntax? syntax = null)",
            "public StringAttributeValueSyntax LabelSyntax { get; }",
            // StringRefParameter.csharpParser is used.
            "context.TryParseStringLiteralSyntax()");
    }

    [Fact]
    public void AttrDefWithCustomParserAndPrinterUsesProvidedExpressions()
    {
        var source = ComposeSource(
        [
            "include \"mlir/IR/AttrTypeBase.td\"",
            string.Empty,
            "def TestDialect : Dialect {",
            "  let name = \"test\";",
            "  let cppNamespace = \"::mlir::test\";",
            "};",
            string.Empty,
            "class MyStrParam<string desc> : AttrOrTypeParameter<\"std::string\", desc>;",
            "extends MyStrParam : MLIRNet_AttrOrTypeParameterExtension {",
            "  let csharpType = \"string\";",
            "  let csharpParser = \"$_parser.TryMatch(TokenKind.Identifier, out var idTok_) ? ParseResult<AttributeValueSyntax>.Success(new StringAttributeValueSyntax(idTok_, idTok_.Text)) : ParseResult<AttributeValueSyntax>.NoMatch()\";",
            "  let csharpPrinter = \"new StringAttributeValueSyntax(TokenFactory.Identifier($_self), $_self)\";",
            "}",
            string.Empty,
            "class Test_Attr<string name, string m> : AttrDef<TestDialect, name> {",
            "  let mnemonic = m;",
            "}",
            string.Empty,
            "def Test_IdAttr : Test_Attr<\"Id\", \"id\"> {",
            "  let parameters = (ins MyStrParam<\"the identifier\">:$name);",
            "  let assemblyFormat = \"`<` $name `>`\";",
            "}",
        ]);

        var registrationSource = GenerateRegistrationSource("test.td", "TestDialectRegistration.g.cs", source);

        // Custom parser expression should be used (with $_parser replaced by context)
        Assert.Contains(
            "context.TryMatch(TokenKind.Identifier, out var idTok_)",
            registrationSource);

        // Custom printer expression should be used (with $_self replaced by attr.Name)
        Assert.Contains(
            "new StringAttributeValueSyntax(TokenFactory.Identifier(attr.Name), attr.Name)",
            registrationSource);
    }
}
