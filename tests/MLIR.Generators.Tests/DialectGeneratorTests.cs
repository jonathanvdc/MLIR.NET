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
        Assert.Contains("public sealed class MiniArith_ConstantOpAssemblyFormat : IOperationAssemblyFormat", registrationSource);
        Assert.Contains("public sealed class MiniArith_AddIOpAssemblyFormat : IOperationAssemblyFormat", registrationSource);
        Assert.Contains("dialect.AddOperation(\"miniarith.constant\"", registrationSource);
        Assert.Contains("dialect.AddOperation(\"miniarith.addi\"", registrationSource);
        Assert.Contains(".WithFactory(static context => new MiniArith_AddIOp(context))", registrationSource);
        Assert.Contains(".WithAssemblyFormat(new MiniArith_AddIOpAssemblyFormat())", registrationSource);
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

        // Attributes is a data-holding auto-property override on both ops.
        Assert.Contains("public override NamedAttributeCollection Attributes { get; }", registrationSource);

        // Individual named attribute is a derived accessor, not a data-holding property.
        Assert.Contains("public NamedAttribute Value => Attributes[\"value\"];", registrationSource);
        Assert.DoesNotContain("public NamedAttribute Value { get; }", registrationSource);

        // Constructors use NamedAttributeCollection attributes parameter instead of individual NamedAttribute params.
        Assert.Contains("NamedAttributeCollection attributes,", registrationSource);

        // Per-attribute convenience constructor also exists (using individual NamedAttribute params).
        Assert.Contains("NamedAttribute value,", registrationSource);
        Assert.Contains("attributes: NamedAttributeCollection.Create(value),", registrationSource);

        // Context constructor passes context.Attributes directly.
        Assert.Contains("attributes: context.Attributes,", registrationSource);
        Assert.DoesNotContain("context.Attributes[\"value\"]", registrationSource);

        // Constructor body assigns Attributes from the collection.
        Assert.Contains("Attributes = attributes;", registrationSource);
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
            "  let arguments = (ins I32Attr:$value);\n" +
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
}
