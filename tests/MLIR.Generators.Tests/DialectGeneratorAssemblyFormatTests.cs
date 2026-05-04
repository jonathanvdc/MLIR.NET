namespace MLIR.Generators.Tests;

using Xunit;

public sealed class DialectGeneratorAssemblyFormatTests : DialectGeneratorTestBase
{
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
            "public Token Lhs { get; }",
            "public Token CommaToken { get; }",
            "public Token Rhs { get; }",
            "public Token ColonToken { get; }",
            "public TypeSyntax ResultType { get; }",
            "public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }",
            "public override void WriteTo(Text.SyntaxWriter writer)");
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
            "context.TryParseAttributeValueSyntax(MLIR.Dialects.Minitest.UnitAttrConstraintAttributeValue.AttributeConstraintDefinition)",
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
            "protected override ParseResult<OperationBodySyntax> TryParseBody(",
            "OperationParseHeader header,",
            "var lhsResult = context.TryParseSsaToken();",
            "var rhsResult = context.TryParseSsaToken();",
            "context.Expect(TokenKind.Comma, ",
            "context.Expect(TokenKind.Colon, ",
            "var attrDictResult = context.TryParseAttrDict();",
            "context.TryParseTypeSyntax()",
            "return ParseResult<OperationBodySyntax>.Success(new MiniArith_AddIOpBodySyntax(lhs, commaToken, rhs, attrDict, colonToken, resultType));");
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
            "context.TryParseAttributeValueSyntax(",
            "var attrDictResult = context.TryParseAttrDict();",
            "return ParseResult<OperationBodySyntax>.Success(new MiniArith_ConstantOpBodySyntax(value, attrDict));");
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
            "context.TryParseTypeSyntax()",
            "return ParseResult<OperationBodySyntax>.Success(");
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
            "return ParseResult<OperationBodySyntax>.Success(");
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
            "Token?",
            "context.TryMatch(TokenKind.Comma,",
            "return ParseResult<OperationBodySyntax>.Success(");
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
            "Token?",
            "AttributeValueSyntax?",
            "bool foundOilist;",
            "while (foundOilist);",
            "context.IsKeyword(\"stride\")",
            "context.IsKeyword(\"padding\")",
            "return ParseResult<OperationBodySyntax>.Success(");
    }

    [Fact]
    public void TryParseGeneratesTryParseSsaTokenListForVariadicOperand()
    {
        var registrationSource = GenerateMiniArithRegistrationSource(
            [
                "def MiniArith_SumOp : MiniArith_Op<\"sum\", []> {",
                "  let summary = \"sums a variadic number of integers\";",
                "  let arguments = (ins Variadic<I32>:$inputs);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$inputs attr-dict `:` type($result)\";",
                "};",
            ]);

        // The variadic operand must use TryParseSsaTokenList (not ParseSsaTokenList) so
        // that parse failures are propagated as ParseResult instead of thrown.
        AssertContainsAll(
            registrationSource,
            "var inputsResult = context.TryParseSsaTokenList();",
            "if (!inputsResult.IsSuccess)",
            "return ParseResult<OperationBodySyntax>.Failure(inputsResult.Diagnostic!);",
            "var inputs = inputsResult.Value;");
        Assert.DoesNotContain("context.ParseSsaTokenList()", registrationSource);
    }

    [Fact]
    public void BuildCustomAssemblySyntaxUsesSemanticFunctionTypeSlicesForVariadicTypeDirective()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_VariadicTypedOp : MyDialect_Op<\"variadic_typed\", []> {",
                "  let arguments = (ins I32:$lhs, Variadic<I32>:$rest);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rest attr-dict `:` type($rest) `->` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            ".Inputs.Skip(1).Take(op.Rest.Count).Select(context.BuildTypeSyntax).ToList()",
            ".Results.Count > (0) ? context.BuildTypeSyntax(");
        AssertDoesNotContainAny(
            registrationSource,
            "functionType.InputTypes.Items.ToList()",
            "context.BuildTypeSyntax(op.TypeSignatureReference) is global::MLIR.Syntax.Types.Collections.FunctionTypeSyntax");
    }

    [Fact]
    public void BindSynthesizesFunctionTypeForVariadicOperandTypeDirective()
    {
        var registrationSource = GenerateMyDialectRegistrationSource(
            [
                "def MyDialect_VariadicTypedOp : MyDialect_Op<\"variadic_typed\", []> {",
                "  let arguments = (ins I32:$lhs, Variadic<I32>:$rest);",
                "  let results = (outs I32:$result);",
                "  let assemblyFormat = \"$lhs `,` $rest attr-dict `:` type($rest) `->` type($result)\";",
                "};",
            ]);

        AssertContainsAll(
            registrationSource,
            "new global::MLIR.Dialects.Builtin.FunctionType(",
            "body.RestType.Select(binder.BindTypeReference).ToArray()",
            "new[] { binder.BindTypeReference(body.ResultType) }.ToArray()");
        AssertDoesNotContainAny(
            registrationSource,
            "return \"null\"",
            "operation.Results.Count == 0");
    }
}
