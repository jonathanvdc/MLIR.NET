namespace MLIR.Generators.Tests;

using System.Linq;
using MLIR.Generators;
using Xunit;

public sealed class DialectGeneratorTests
{
    [Fact]
    public void GeneratesDialectRegistrationTypedNodesAndCustomAssemblyStubs()
    {
        const string source =
            "def Builtin_Dialect : Dialect {\n" +
            "  let name = \"builtin\";\n" +
            "  let cppNamespace = \"::mlir::builtin\";\n" +
            "};\n" +
            "\n" +
            "class Builtin_Attr<string name> : AttrDef<Builtin_Dialect, name> { let mnemonic = name; };\n" +
            "\n" +
            "def BuiltinI32Attr : Builtin_Attr<\"i32\">;\n" +
            "\n" +
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$value attr-dict\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let summary = \"integer addition\";\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        Assert.Contains("namespace MLIR.Miniarith;", registrationSource);
        Assert.Contains("public static class MiniarithDialectRegistration", registrationSource);
        Assert.Contains("public sealed class MiniArith_ConstantOp : Operation", registrationSource);
        Assert.Contains("public sealed class MiniArith_AddIOp : Operation", registrationSource);
        Assert.Contains("public static OperationDefinition OperationDefinition { get; } = CreateOperationDefinition();", registrationSource);
        Assert.Contains("public sealed class MiniArith_ConstantOpAssemblyFormat : IOperationAssemblyFormat", registrationSource);
        Assert.Contains("public sealed class MiniArith_AddIOpAssemblyFormat : IOperationAssemblyFormat", registrationSource);
        Assert.Contains("dialect.AddOperation(MiniArith_ConstantOp.OperationDefinition);", registrationSource);
        Assert.Contains("dialect.AddOperation(MiniArith_AddIOp.OperationDefinition);", registrationSource);
        Assert.Contains(".WithFactory(static context => new MiniArith_AddIOp(context))", registrationSource);
        Assert.Contains(".WithAssemblyFormat(new MiniArith_AddIOpAssemblyFormat())", registrationSource);
        Assert.DoesNotContain("RewriteChildren", registrationSource);
        Assert.Contains("public MiniArith_ConstantOp(OperationConstructionContext context)", registrationSource);
        Assert.Contains("public MiniArith_AddIOp(OperationConstructionContext context)", registrationSource);
    }

    [Fact]
    public void GeneratesOperationBodySyntaxClassForDeclarativeAssemblyFormat()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$value attr-dict\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let summary = \"integer addition\";\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // BodySyntax classes are generated for operations with declarative assembly formats.
        Assert.Contains("public sealed class MiniArith_ConstantOpBodySyntax : OperationBodySyntax", registrationSource);
        Assert.Contains("public sealed class MiniArith_AddIOpBodySyntax : OperationBodySyntax", registrationSource);

        // MiniArith_ConstantOp: $value (attribute) and attr-dict.
        Assert.Contains("public AttributeValueSyntax Value { get; }", registrationSource);

        // MiniArith_AddIOp: $lhs, `,`, $rhs, attr-dict, `:`, type($result).
        Assert.Contains("public SyntaxToken Lhs { get; }", registrationSource);
        Assert.Contains("public SyntaxToken CommaToken { get; }", registrationSource);
        Assert.Contains("public SyntaxToken Rhs { get; }", registrationSource);
        Assert.Contains("public SyntaxToken ColonToken { get; }", registrationSource);
        Assert.Contains("public TypeSyntax ResultType { get; }", registrationSource);

        // Both classes share AttrDict.
        Assert.Contains("public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }", registrationSource);

        // WriteTo is implemented.
        Assert.Contains("public override void WriteTo(Text.SyntaxWriter writer, int indentLevel)", registrationSource);
    }

    [Fact]
    public void GeneratedBindMethodUsesPatternMatchInsteadOfHardCastForBodyType()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let summary = \"integer addition\";\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // The Bind method must use a safe pattern-match rather than a hard cast so that
        // a wrong body type yields a diagnostic instead of an InvalidCastException.
        Assert.Contains("if (syntax.Body is not MiniArith_AddIOpBodySyntax body)", registrationSource);
        Assert.DoesNotContain("(MiniArith_AddIOpBodySyntax)syntax.Body", registrationSource);
    }

    [Fact]
    public void BodySyntaxClassIsNotGeneratedForOperationsWithoutDeclarativeAssemblyFormat()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // No BodySyntax class when there is no declarative assembly format.
        Assert.DoesNotContain("BodySyntax", registrationSource);
    }

    [Fact]
    public void GeneratesXmlDocCommentsFromSummaryAndDescription()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "  let summary = \"Mini arithmetic dialect\";\n" +
            "  let description = [{A dialect for basic integer arithmetic operations.}];\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let description = [{Produces a constant integer value.}];\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // Operation summary and description as doc-comments.
        Assert.Contains("/// <summary>integer constant</summary>", registrationSource);
        Assert.Contains("/// <remarks>", registrationSource);
        Assert.Contains("/// Produces a constant integer value.", registrationSource);

        // Dialect class summary and description as doc-comments.
        Assert.Contains("/// <summary>Mini arithmetic dialect</summary>", registrationSource);
        Assert.Contains("/// A dialect for basic integer arithmetic operations.", registrationSource);
    }

    [Fact]
    public void DoesNotGenerateDocCommentsWhenSummaryAndDescriptionAreAbsent()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        Assert.DoesNotContain("/// <summary>", registrationSource);
        Assert.DoesNotContain("/// <remarks>", registrationSource);
    }

    [Fact]
    public void AttributesPropertyHoldsDataAndNamedAttributeAccessorsAreDerived()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let summary = \"integer addition\";\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // Attribute access flows through the mutable base operation state.
        Assert.DoesNotContain("public override NamedAttributeCollection Attributes { get; }", registrationSource);

        // Individual named attribute is a derived accessor using the narrowed BigInteger type.
        // MiniArith_ConstantOp has no assembly format, so 'value' cannot be determined to
        // be required – it is generated as a nullable optional accessor.
        Assert.Contains("public BigInteger? Value", registrationSource);
        Assert.Contains("get => Attributes.TryGet(\"value\",", registrationSource);
        Assert.Contains("set => SetAttribute(\"value\", value.HasValue", registrationSource);
        Assert.DoesNotContain("public NamedAttribute Value { get; }", registrationSource);

        // Constructors use NamedAttributeCollection attributes parameter instead of individual NamedAttribute params.
        Assert.Contains("NamedAttributeCollection attributes,", registrationSource);

        // Per-attribute convenience constructor also exists (using individual BigInteger? param for optional).
        Assert.Contains("BigInteger? value,", registrationSource);

        // Context constructor passes context.Attributes directly.
        Assert.Contains("attributes: context.Attributes,", registrationSource);
        Assert.DoesNotContain("context.Attributes[\"value\"]", registrationSource);

        // Base constructor now receives the attribute collection directly.
        Assert.Contains("attributes,", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesOperandAndLiteralParsingCallsForAddIOp()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure]> {\n" +
            "  let summary = \"integer addition\";\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // TryParse method is generated for the operation.
        Assert.Contains("public bool TryParse(", registrationSource);

        // Operand variables are parsed as SSA tokens.
        Assert.Contains("var lhs = context.ParseSsaToken();", registrationSource);
        Assert.Contains("var rhs = context.ParseSsaToken();", registrationSource);

        // Punctuation literals are consumed via Expect.
        Assert.Contains("context.Expect(TokenKind.Comma, ", registrationSource);
        Assert.Contains("context.Expect(TokenKind.Colon, ", registrationSource);

        // Attribute dictionary is parsed.
        Assert.Contains("var attrDict = context.ParseAttrDict();", registrationSource);

        // Type directive is parsed.
        Assert.Contains("context.ParseTypeSyntax()", registrationSource);

        // Body is constructed from the parsed locals.
        Assert.Contains("body = new MiniArith_AddIOpBodySyntax(lhs, commaToken, rhs, attrDict, colonToken, resultType);", registrationSource);

        // Method returns true on success.
        Assert.Contains("return true;", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesAttributeValueParsingForConstantOp()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let arguments = (ins BuiltinI32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$value attr-dict\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // Attribute variable is parsed as an attribute value.
        Assert.Contains("context.ParseAttributeValueSyntax(", registrationSource);

        // Attribute dictionary is parsed.
        Assert.Contains("var attrDict = context.ParseAttrDict();", registrationSource);

        // Body is constructed with the parsed attribute and attr-dict.
        Assert.Contains("body = new MiniArith_ConstantOpBodySyntax(value, attrDict);", registrationSource);

        // The implementation returns true on success.
        Assert.Contains("return true;", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesKeywordExpectForKeywordLiterals()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_CastOp : MyDialect_Op<\"cast\", []> {\n" +
            "  let arguments = (ins I32:$input);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$input `to` attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // Keyword literal produces an ExpectKeyword call.
        Assert.Contains("context.ExpectKeyword(\"to\", ", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesQualifiedTypeField()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_ConvertOp : MyDialect_Op<\"convert\", []> {\n" +
            "  let arguments = (ins I32:$input);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$input attr-dict `:` qualified(type($result))\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // qualified(...) does not affect parsing – a TypeSyntax field is generated and a type is parsed.
        Assert.Contains("TypeSyntax", registrationSource);
        // The generated parse call should be one of the type-parsing variants.
        Assert.Contains("context.ParseTypeSyntax()", registrationSource);
        Assert.Contains("return true;", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesResultsTypeField()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_GenOp : MyDialect_Op<\"gen\", []> {\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"attr-dict `:` results\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // A TypeSyntax field called ResultsType should be generated for the results directive.
        Assert.Contains("ResultsType", registrationSource);
        Assert.Contains("TypeSyntax", registrationSource);
        Assert.Contains("return true;", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesOptionalGroupConditionalCode()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_BinaryOp : MyDialect_Op<\"binary\", []> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs (`,` $rhs^)? attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // Optional group fields are nullable.
        Assert.Contains("SyntaxToken?", registrationSource);

        // The conditional guard uses TryMatch for the leading comma.
        Assert.Contains("context.TryMatch(TokenKind.Comma,", registrationSource);

        // The TryParse returns true for the success path.
        Assert.Contains("return true;", registrationSource);
    }

    [Fact]
    public void TryParseGeneratesOilistDoWhileLoop()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_ConfigOp : MyDialect_Op<\"config\", []> {\n" +
            "  let arguments = (ins I32Attr:$stride, I32Attr:$padding);\n" +
            "  let assemblyFormat = \"oilist(`stride` $stride | `padding` $padding) attr-dict\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // Oilist fields are nullable.
        Assert.Contains("SyntaxToken?", registrationSource);
        Assert.Contains("AttributeValueSyntax?", registrationSource);

        // The oilist loop is emitted.
        Assert.Contains("bool foundOilist;", registrationSource);
        Assert.Contains("while (foundOilist);", registrationSource);

        // IsKeyword is used to dispatch each clause.
        Assert.Contains("context.IsKeyword(\"stride\")", registrationSource);
        Assert.Contains("context.IsKeyword(\"padding\")", registrationSource);

        Assert.Contains("return true;", registrationSource);
    }

    [Fact]
    public void RequiredAttributeInAssemblyFormatGeneratesNonNullablePropertyAndRequiredRegistration()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_ConstantOp : MiniArith_Op<\"constant\", [Pure]> {\n" +
            "  let summary = \"integer constant\";\n" +
            "  let arguments = (ins I32Attr:$value);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$value attr-dict\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // $value appears directly at the top level of the format → required.
        // The accessor property is non-nullable with the narrowed BigInteger type.
        Assert.Contains("public BigInteger Value", registrationSource);
        Assert.Contains("((IntegerAttributeValue)Attributes[\"value\"].Value).Value;", registrationSource);
        Assert.Contains("new NamedAttribute(\"value\", new", registrationSource);
        Assert.DoesNotContain("public BigInteger? Value", registrationSource);

        // Per-attribute convenience constructor uses non-nullable BigInteger.
        Assert.Contains("BigInteger value,", registrationSource);
        Assert.DoesNotContain("BigInteger? value,", registrationSource);
        Assert.Contains("new NamedAttribute(\"value\", new", registrationSource);

        // OperationDefinition registration uses RequiredAttribute, not OptionalAttribute.
        Assert.Contains("operation.RequiredAttribute(\"value\",", registrationSource);
        Assert.DoesNotContain("operation.OptionalAttribute(\"value\")", registrationSource);
    }

    [Fact]
    public void OptionalAttributeInOptionalGroupGeneratesNullablePropertyAndOptionalRegistration()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_FlagOp : MyDialect_Op<\"flag\", []> {\n" +
            "  let arguments = (ins I32Attr:$optAttr);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"($optAttr^)? attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // $optAttr is inside an optional group → optional.
        // The accessor property is nullable BigInteger? with the narrowed type.
        Assert.Contains("public BigInteger? OptAttr", registrationSource);
        Assert.Contains("Attributes.TryGet(\"optAttr\",", registrationSource);
        Assert.Contains("set => SetAttribute(\"optAttr\", value.HasValue", registrationSource);
        Assert.DoesNotContain("public BigInteger OptAttr", registrationSource);

        // OperationDefinition registration uses OptionalAttribute.
        Assert.Contains("operation.OptionalAttribute(\"optAttr\",", registrationSource);
        Assert.DoesNotContain("operation.RequiredAttribute(\"optAttr\")", registrationSource);
    }

    [Fact]
    public void RequiredOperandInAssemblyFormatGeneratesNonNullableProperty()
    {
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure]> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs `,` $rhs attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // Both operands appear at the top level → required, non-nullable.
        Assert.Contains("public Value Lhs", registrationSource);
        Assert.Contains("get => Operands[0].Value!;", registrationSource);
        Assert.Contains("set => SetOperand(0, value);", registrationSource);
        Assert.Contains("public Value Rhs", registrationSource);
        Assert.Contains("get => Operands[1].Value!;", registrationSource);
        Assert.Contains("set => SetOperand(1, value);", registrationSource);
        Assert.DoesNotContain("public Value? Lhs", registrationSource);
        Assert.DoesNotContain("public Value? Rhs", registrationSource);
    }

    [Fact]
    public void OptionalOperandInOptionalGroupGeneratesNullableProperty()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_BinaryOp : MyDialect_Op<\"binary\", []> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$lhs (`,` $rhs^)? attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // $lhs at the top level → required, non-nullable.
        Assert.Contains("public Value Lhs", registrationSource);
        Assert.Contains("get => Operands[0].Value!;", registrationSource);
        Assert.Contains("set => SetOperand(0, value);", registrationSource);
        Assert.DoesNotContain("public Value? Lhs", registrationSource);

        // $rhs inside optional group → optional, nullable.
        Assert.Contains("public Value? Rhs", registrationSource);
        Assert.Contains("get => Operands[1].Value;", registrationSource);
        Assert.Contains("set => SetOperand(1, value);", registrationSource);
        Assert.DoesNotContain("public Value Rhs { get; }", registrationSource);
    }

    [Fact]
    public void OilistAttributesAreOptionalInRegistration()
    {
        const string source =
            "class MyDialect_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MyDialect_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MyDialect_Dialect : Dialect {\n" +
            "  let name = \"mydialect\";\n" +
            "  let cppNamespace = \"::mlir::mydialect\";\n" +
            "};\n" +
            "\n" +
            "def MyDialect_ConfigOp : MyDialect_Op<\"config\", []> {\n" +
            "  let arguments = (ins I32Attr:$stride, I32Attr:$padding);\n" +
            "  let assemblyFormat = \"oilist(`stride` $stride | `padding` $padding) attr-dict\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("mydialect.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MydialectDialectRegistration.g.cs")).SourceText.ToString();

        // Attributes inside oilist are optional.
        Assert.Contains("operation.OptionalAttribute(\"stride\",", registrationSource);
        Assert.Contains("operation.OptionalAttribute(\"padding\",", registrationSource);
        Assert.DoesNotContain("operation.RequiredAttribute(\"stride\")", registrationSource);
        Assert.DoesNotContain("operation.RequiredAttribute(\"padding\")", registrationSource);

        // Nullable BigInteger? accessor properties for the optional attributes.
        Assert.Contains("public BigInteger? Stride", registrationSource);
        Assert.Contains("public BigInteger? Padding", registrationSource);
    }

    [Fact]
    public void AttributePropertyTypeIsNarrowedForKnownConstraintKinds()
    {
        // Verify that when an operation uses a well-known ODS attribute constraint,
        // the generated property exposes the underlying value type rather than NamedAttribute.
        const string source =
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> :\n" +
            "    Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "  let cppNamespace = \"::mlir::miniarith\";\n" +
            "};\n" +
            "\n" +
            "def MiniArith_AddImmOp : MiniArith_Op<\"add_imm\", []> {\n" +
            "  let arguments = (ins I32Attr:$intVal, BoolAttr:$boolVal, StrAttr:$strVal, F32Attr:$floatVal);\n" +
            "  let results = (outs I32:$result);\n" +
            "  let assemblyFormat = \"$intVal `,` $boolVal `,` $strVal `,` $floatVal attr-dict `:` type($result)\";\n" +
            "};";

        var generatedSources = GeneratorTestHelpers.RunGenerator(
            new DialectGenerator(),
            ("miniarith.td", source));
        var registrationSource = Assert.Single(generatedSources.Where(static result => result.HintName == "MiniarithDialectRegistration.g.cs")).SourceText.ToString();

        // I32Attr → required BigInteger (all four are required since they appear at the top level)
        Assert.Contains("public BigInteger IntVal", registrationSource);
        Assert.Contains("((IntegerAttributeValue)Attributes[\"intVal\"].Value).Value;", registrationSource);
        Assert.DoesNotContain("public NamedAttribute IntVal", registrationSource);

        // BoolAttr → required bool
        Assert.Contains("public bool BoolVal", registrationSource);
        Assert.Contains("((BooleanAttributeValue)Attributes[\"boolVal\"].Value).Value;", registrationSource);
        Assert.DoesNotContain("public NamedAttribute BoolVal", registrationSource);

        // StrAttr → required string
        Assert.Contains("public string StrVal", registrationSource);
        Assert.Contains("((StringAttributeValue)Attributes[\"strVal\"].Value).Value;", registrationSource);
        Assert.DoesNotContain("public NamedAttribute StrVal", registrationSource);

        // F32Attr → required string (LiteralText)
        Assert.Contains("public string FloatVal", registrationSource);
        Assert.Contains("((FloatingPointAttributeValue)Attributes[\"floatVal\"].Value).LiteralText;", registrationSource);
        Assert.DoesNotContain("public NamedAttribute FloatVal", registrationSource);

        // Per-attribute convenience constructor uses narrowed value types, not NamedAttribute.
        Assert.Contains("BigInteger intVal,", registrationSource);
        Assert.Contains("bool boolVal,", registrationSource);
        Assert.Contains("string strVal,", registrationSource);
        Assert.Contains("string floatVal,", registrationSource);
        Assert.DoesNotContain("NamedAttribute intVal,", registrationSource);
    }
}
