namespace MLIR.Generators.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Generators;
using Xunit;

public sealed class DialectGeneratorTests
{
    private static readonly string[] MiniArithPreamble =
    [
        "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :",
        "    Op<MiniArith_Dialect, mnemonic, traits>;",
        string.Empty,
        "def MiniArith_Dialect : Dialect {",
        "  let name = \"miniarith\";",
        "  let cppNamespace = \"::mlir::miniarith\";",
        "};",
    ];

    private static readonly string[] MyDialectPreamble =
    [
        "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :",
        "    Op<MyDialect_Dialect, mnemonic, traits>;",
        string.Empty,
        "def MyDialect_Dialect : Dialect {",
        "  let name = \"mydialect\";",
        "  let cppNamespace = \"::mlir::mydialect\";",
        "};",
    ];

    [Fact]
    public void GeneratesDialectRegistrationTypedNodesAndCustomAssemblyStubs()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def Builtin_Dialect : Dialect {",
                "  let name = \"builtin\";",
                "  let cppNamespace = \"::mlir::builtin\";",
                "};",
                string.Empty,
                "class Builtin_Attr<string name> : AttrDef<Builtin_Dialect, name> { let mnemonic = name; };",
                string.Empty,
                "def BuiltinI32Attr : Builtin_Attr<\"i32\">;",
                string.Empty,
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let summary = \"integer constant\";",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$value attr-dict\";",
                "};",
                string.Empty,
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {",
                "  let summary = \"integer addition\";",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "namespace MLIR.Miniarith;",
            "public static class MiniarithDialectRegistration",
            "public sealed class MiniArith_ConstantOp : Operation",
            "public sealed class MiniArith_AddIOp : Operation",
            "public static OperationDefinition OperationDefinition { get; } = CreateOperationDefinition();",
            "public sealed class MiniArith_ConstantOpAssemblyFormat : IOperationAssemblyFormat",
            "public sealed class MiniArith_AddIOpAssemblyFormat : IOperationAssemblyFormat",
            "dialect.AddOperation(MiniArith_ConstantOp.OperationDefinition);",
            "dialect.AddOperation(MiniArith_AddIOp.OperationDefinition);",
            ".WithFactory(static context => new MiniArith_AddIOp(context))",
            ".WithAssemblyFormat(new MiniArith_AddIOpAssemblyFormat())",
            "public MiniArith_ConstantOp(OperationConstructionContext context)",
            "public MiniArith_AddIOp(OperationConstructionContext context)");
        Assert.DoesNotContain("RewriteChildren", registrationSource);
    }

    [Fact]
    public void GeneratesOperationBodySyntaxClassForDeclarativeAssemblyFormat()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let summary = \"integer constant\";",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$value attr-dict\";",
                "};",
                string.Empty,
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {",
                "  let summary = \"integer addition\";",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public sealed class MiniArith_ConstantOpBodySyntax : OperationBodySyntax",
            "public sealed class MiniArith_AddIOpBodySyntax : OperationBodySyntax",
            "public AttributeValueSyntax Value { get; }",
            "public SyntaxToken Lhs { get; }",
            "public SyntaxToken CommaToken { get; }",
            "public SyntaxToken Rhs { get; }",
            "public SyntaxToken ColonToken { get; }",
            "public TypeSyntax ResultType { get; }",
            "public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }",
            "public override void WriteTo(Text.SyntaxWriter writer, int indentLevel)");
    }

    [Fact]
    public void GeneratedBindMethodUsesPatternMatchInsteadOfHardCastForBodyType()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {",
                "  let summary = \"integer addition\";",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";",
                "};",
            ]);

        Assert.Contains("if (syntax.Body is not MiniArith_AddIOpBodySyntax body)", registrationSource);
        Assert.DoesNotContain("(MiniArith_AddIOpBodySyntax)syntax.Body", registrationSource);
    }

    [Fact]
    public void BodySyntaxClassIsNotGeneratedForOperationsWithoutDeclarativeAssemblyFormat()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        Assert.DoesNotContain("BodySyntax", registrationSource);
    }

    [Fact]
    public void GeneratesXmlDocCommentsFromSummaryAndDescription()
    {
        var registrationSource = GenerateRegistrationSource(
            "miniarith.td",
            "MiniarithDialectRegistration.g.cs",
            ComposeSource(
                [
                    "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :",
                    "    Op<MiniArith_Dialect, mnemonic, traits>;",
                    string.Empty,
                    "def MiniArith_Dialect : Dialect {",
                    "  let name = \"miniarith\";",
                    "  let cppNamespace = \"::mlir::miniarith\";",
                    "  let summary = \"Mini arithmetic dialect\";",
                    "  let description = [{A dialect for basic integer arithmetic operations.}];",
                    "};",
                    string.Empty,
                    "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                    "  let summary = \"integer constant\";",
                    "  let description = [{Produces a constant integer value.}];",
                    "  let arguments = (ins I32Attr:$value);",
                    "  let results = (outs I32:$result);",
                    "};",
                ]));

        AssertContainsAll(
            registrationSource,
            "/// <summary>integer constant</summary>",
            "/// <remarks>",
            "/// Produces a constant integer value.",
            "/// <summary>Mini arithmetic dialect</summary>",
            "/// A dialect for basic integer arithmetic operations.");
    }

    [Fact]
    public void DoesNotGenerateDocCommentsWhenSummaryAndDescriptionAreAbsent()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let arguments = (ins I32Attr:$value);",
                "  let results = (outs I32:$result);",
                "};",
            ]);

        Assert.DoesNotContain("/// <summary>", registrationSource);
        Assert.DoesNotContain("/// <remarks>", registrationSource);
    }

    [Fact]
    public void ReportsDiagnosticWhenEmissionFails()
    {
        var runResult = GeneratorTestHelpers.RunGeneratorDetailed(
            new DialectGenerator(),
            true,
            (
                "miniarith.td",
                ComposeMiniArithSource(
                    [
                        "def MiniArith_BrokenOp : MiniArith_Op<\"broken\", [Pure]> {",
                        "  let arguments = (ins I32:$lhs, I32:$rhs);",
                        "  let results = (outs I32:$result);",
                        "  let assemblyFormat = \"attr-dict\";",
                        "};",
                    ])));

        var result = Assert.Single(runResult.Results);
        var diagnostic = Assert.Single(result.Diagnostics.Where(static diagnostic => diagnostic.Id == "MLIRGEN002"));

        Assert.Empty(result.GeneratedSources);
        Assert.Contains("MiniArith_BrokenOp", diagnostic.GetMessage());
        Assert.Contains("No body field was generated for operand 'lhs'", diagnostic.GetMessage());
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
    public void UnitAttributeAnchorInsideOptionalGroupUsesKeywordSyntax()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_AddUnitImmediateOp : MiniArith_Op<\"add_unit_immediate\", [Pure]> {",
                "  let summary = \"integer addition with a unit immediate\";",
                "  let arguments = (ins UnitAttr:$value, I32:$lhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"(`keyword` $value^)? `,` $lhs attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "value = new UnitAttributeValueSyntax(keywordKeyword.Value);",
            "new NamedAttributeCollection(new NamedAttribute?[]",
            "if (op.Value)");
        AssertDoesNotContainAny(
            registrationSource,
            "context.ParseAttributeValueSyntax(MLIR.Minitest.UnitAttrConstraintAttributeValue.AttributeConstraintDefinition)",
            "NamedAttributeCollection.Create(value ?",
            "if (op.Value != null)");
    }

    [Fact]
    public void TryParseGeneratesOperandAndLiteralParsingCallsForAddIOp()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure]> {",
                "  let summary = \"integer addition\";",
                "  let arguments = (ins I32:$lhs, I32:$rhs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "public bool TryParse(",
            "var lhs = context.ParseSsaToken();",
            "var rhs = context.ParseSsaToken();",
            "context.Expect(TokenKind.Comma, ",
            "context.Expect(TokenKind.Colon, ",
            "var attrDict = context.ParseAttrDict();",
            "context.ParseTypeSyntax()",
            "body = new MiniArith_AddIOpBodySyntax(lhs, commaToken, rhs, attrDict, colonToken, resultType);",
            "return true;");
    }

    [Fact]
    public void TryParseGeneratesAttributeValueParsingForConstantOp()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def Builtin_Dialect : Dialect {",
                "  let name = \"builtin\";",
                "  let cppNamespace = \"::mlir::builtin\";",
                "};",
                string.Empty,
                "class Builtin_Attr<string name> : AttrDef<Builtin_Dialect, name> { let mnemonic = name; };",
                string.Empty,
                "def BuiltinI32Attr : Builtin_Attr<\"i32\">;",
                string.Empty,
                "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {",
                "  let summary = \"integer constant\";",
                "  let arguments = (ins BuiltinI32Attr:$value);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$value attr-dict\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "context.ParseAttributeValueSyntax(",
            "var attrDict = context.ParseAttrDict();",
            "body = new MiniArith_ConstantOpBodySyntax(value, attrDict);",
            "return true;");
    }

    [Fact]
    public void TryParseGeneratesKeywordExpectForKeywordLiterals()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_CastOp : MyDialect_Op<\"cast\", []> {",
                "  let arguments = (ins I32:$input);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$input `to` attr-dict `:` type($result)\";",
                "};",
            ]);

        Assert.Contains("context.ExpectKeyword(\"to\", ", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesQualifiedTypeField()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_ConvertOp : MyDialect_Op<\"convert\", []> {",
                "  let arguments = (ins I32:$input);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$input attr-dict `:` qualified(type($result))\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "TypeSyntax",
            "context.ParseTypeSyntax()",
            "return true;");
    }

    [Fact]
    public void TryParseGeneratesResultsTypeField()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_GenOp : MyDialect_Op<\"gen\", []> {",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"attr-dict `:` results\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "ResultsType",
            "TypeSyntax",
            "return true;");
    }

    [Fact]
    public void TryParseGeneratesOptionalGroupConditionalCode()
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
            "SyntaxToken?",
            "context.TryMatch(TokenKind.Comma,",
            "return true;");
    }

    [Fact]
    public void TryParseGeneratesOilistDoWhileLoop()
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
            "SyntaxToken?",
            "AttributeValueSyntax?",
            "bool foundOilist;",
            "while (foundOilist);",
            "context.IsKeyword(\"stride\")",
            "context.IsKeyword(\"padding\")",
            "return true;");
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
            "get => Operands[0].Value!;",
            "set => SetOperand(0, value);",
            "public Value Rhs",
            "get => Operands[1].Value!;",
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
            "get => Operands[0].Value!;",
            "set => SetOperand(0, value);",
            "public Value? Rhs",
            "get => Operands[1].Value;",
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
            "public BigInteger? Stride",
            "public BigInteger? Padding");
        AssertDoesNotContainAny(
            registrationSource,
            "operation.RequiredAttribute(\"stride\")",
            "operation.RequiredAttribute(\"padding\")");
    }

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
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_TestOp : MyDialect_Op<\"test\", []> {",
                $"  let arguments = (ins {constraintType}:${attributeName}, I32:$lhs);",
                "  let results = (outs I32:$result);",
                $"  let assemblyFormat = \"${attributeName} `,` $lhs attr-dict `:` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            attributeValueType,
            $"public IReadOnlyList<{elementType}> {propertyName}",
            assemblyFormatType,
            $"IReadOnlyList<{elementType}> {attributeName},");
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

    private static string GenerateMiniArithRegistrationSource(IEnumerable<string> lines)
    {
        return GenerateRegistrationSource(
            "miniarith.td",
            "MiniarithDialectRegistration.g.cs",
            ComposeMiniArithSource(lines));
    }

    private static string GenerateMyDialectRegistrationSource(IEnumerable<string> lines)
    {
        return GenerateRegistrationSource(
            "mydialect.td",
            "MydialectDialectRegistration.g.cs",
            ComposeMyDialectSource(lines));
    }

    private static string GenerateRegistrationSource(string path, string hintName, string source)
    {
        var generatedSources = GeneratorTestHelpers.RunGenerator(new DialectGenerator(), (path, source));
        return Assert.Single(generatedSources.Where(result => result.HintName == hintName)).SourceText.ToString();
    }

    private static string ComposeMiniArithSource(IEnumerable<string> lines)
    {
        return ComposeSource(MiniArithPreamble.Concat(new[] { string.Empty }).Concat(lines));
    }

    private static string ComposeMyDialectSource(IEnumerable<string> lines)
    {
        return ComposeSource(MyDialectPreamble.Concat(new[] { string.Empty }).Concat(lines));
    }

    private static string ComposeSource(IEnumerable<string> lines)
    {
        return string.Join("\n", lines);
    }

    private static void AssertContainsAll(string text, params string[] snippets)
    {
        foreach (var snippet in snippets)
        {
            Assert.Contains(snippet, text);
        }
    }

    private static void AssertDoesNotContainAny(string text, params string[] snippets)
    {
        foreach (var snippet in snippets)
        {
            Assert.DoesNotContain(snippet, text);
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
