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
            "public global::MLIR.Numerics.ApInt IntVal",
            "I32AttrConstraintAttributeValue)Attributes[\"intVal\"].Value).Value;",
            "public bool BoolVal",
            "BoolAttrConstraintAttributeValue)Attributes[\"boolVal\"].Value).Value;",
            "public string StrVal",
            "StrAttrConstraintAttributeValue)Attributes[\"strVal\"].Value).Value;",
            "public global::MLIR.Numerics.ApFloat FloatVal",
            "F32AttrConstraintAttributeValue)Attributes[\"floatVal\"].Value).Value;",
            "public global::MLIR.Numerics.ApFloat DoubleVal",
            "F64AttrConstraintAttributeValue)Attributes[\"doubleVal\"].Value).Value;",
            "global::MLIR.Numerics.ApInt intVal,",
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
            "NamedAttribute intVal,");
    }

    [Theory]
    [InlineData("DenseI32ArrayAttr", "indices", "global::MLIR.Numerics.ApInt", "DenseIntegerArrayAttributeValue", "DenseIntegerArrayAttributeAssemblyFormat", "Indices")]
    [InlineData("DenseBoolArrayAttr", "flags", "bool", "DenseBooleanArrayAttributeValue", "DenseBooleanArrayAttributeAssemblyFormat", "Flags")]
    [InlineData("DenseF32ArrayAttr", "coeffs", "global::MLIR.Numerics.ApFloat", "DenseFloatingPointArrayAttributeValue", "DenseFloatingPointArrayAttributeAssemblyFormat", "Coeffs")]
    [InlineData("DenseF64ArrayAttr", "weights", "global::MLIR.Numerics.ApFloat", "DenseFloatingPointArrayAttributeValue", "DenseFloatingPointArrayAttributeAssemblyFormat", "Weights")]
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
            "public IReadOnlyList<IReadOnlyList<global::MLIR.Numerics.ApInt>> IndexLists",
            "IReadOnlyList<string> strings,",
            "IReadOnlyList<TypeSyntax> types,",
            "IReadOnlyList<NamedAttributeCollection> dicts,",
            "IReadOnlyList<IReadOnlyList<global::MLIR.Numerics.ApInt>> indexLists,",
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
            "MyArithCmpPredicateAttrConstraintAttributeValue");

        // The operation property must use the fully-qualified dialect type.
        AssertContainsAll(
            dialectSource,
            "public MLIR.Myarith.CmpPredicate Predicate");

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
            "public FooAttr(string value)");

        // Assembly format class
        AssertContainsAll(
            registrationSource,
            // Body-only marker so the parser strips '#name' before calling TryParse
            "internal sealed class FooAttrAssemblyFormat : IBodyOnlyAttributeAssemblyFormat",
            "ParseResult<AttributeValueSyntax> TryParse",
            // StringRefParameter.csharpParser delegates to the string-literal helper
            "context.TryParseStringLiteralSyntax()",
            "AttributeValue Bind(",
            "AttributeValueSyntax BuildCustomAssemblySyntax(",
            // StringRefParameter.csharpPrinter wraps the string value in a quoted literal
            "StringLiteralAttributeAssemblyFormat.Quote(attr.Value)");
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
            "public PairAttr(global::MLIR.Numerics.ApInt first, global::MLIR.Numerics.ApInt second)");

        // Assembly format class uses the APIntParameter.csharpParser helper for both parameters
        AssertContainsAll(
            registrationSource,
            // APIntParameter.csharpParser delegates to the integer-literal helper
            "context.TryParseIntegerLiteralSyntax()");
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
            "  let csharpPrinter = \"new StringAttributeValueSyntax(new SyntaxToken($_self), $_self)\";",
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
            "new StringAttributeValueSyntax(new SyntaxToken(attr.Name), attr.Name)",
            registrationSource);
    }
}
