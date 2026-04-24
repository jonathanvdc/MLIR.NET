namespace TableGen.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using TableGen.Evaluation;
using Xunit;

public sealed class EvaluationTests
{
    [Fact]
    public void EvaluatesInheritanceTemplateArgumentsAndLets()
    {
        const string source =
            "class Base<int width, string name = \"anon\"> {\n" +
            "  int Width = width;\n" +
            "  string Name = name;\n" +
            "  bit Enabled = 0;\n" +
            "};\n" +
            "def Example : Base<8> {\n" +
            "  let Enabled = 1;\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("Example", record.Name);
        Assert.Equal(8, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
        Assert.Equal("anon", Assert.IsType<StringValue>(record.GetField("Name")).Value);
        Assert.True(Assert.IsType<BitValue>(record.GetField("Enabled")).Value);
    }

    [Fact]
    public void EvaluatesExtendsOverlaysIntoTheTargetRecord()
    {
        const string source =
            "class AssemblyOverlay<string asm> {\n" +
            "  string csharpAsmFormatCode = asm;\n" +
            "};\n" +
            "class PriorityOverlay<int value> {\n" +
            "  int priority = value;\n" +
            "};\n" +
            "def Example {\n" +
            "  string mnemonic = \"select\";\n" +
            "};\n" +
            "extends Example : AssemblyOverlay<\"global::MLIR.Dialects.Extensions.SelectLikeOperationAssemblyFormat.Instance\">, PriorityOverlay<7> {\n" +
            "  let priority = 9;\n" +
            "}";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("Example", record.Name);
        Assert.Equal("select", Assert.IsType<StringValue>(record.GetField("mnemonic")).Value);
        Assert.Equal("global::MLIR.Dialects.Extensions.SelectLikeOperationAssemblyFormat.Instance", Assert.IsType<StringValue>(record.GetField("csharpAsmFormatCode")).Value);
        Assert.Equal(9, Assert.IsType<IntegerValue>(record.GetField("priority")).Value);
    }

    [Fact]
    public void ReportsDuplicateFieldsAcrossExtendsDeclarations()
    {
        const string source =
            "class OverlayA {\n" +
            "  string csharpAsmFormatCode = ?;\n" +
            "};\n" +
            "def Example {\n" +
            "  string mnemonic = \"demo\";\n" +
            "};\n" +
            "extends Example : OverlayA {\n" +
            "  let csharpAsmFormatCode = \"first\";\n" +
            "}\n" +
            "extends Example : OverlayA {\n" +
            "  let csharpAsmFormatCode = \"second\";\n" +
            "}";

        var exception = Assert.Throws<InvalidOperationException>(() => Document.Parse(source).Value.Evaluate());

        Assert.Contains("already defines field 'csharpAsmFormatCode'", exception.Message);
    }

    [Fact]
    public void ReportsFieldsThatAreNotDeclaredByTheExtensionSchema()
    {
        const string source =
            "class OverlayA {\n" +
            "  string csharpAsmFormatCode = ?;\n" +
            "};\n" +
            "def Example;\n" +
            "extends Example : OverlayA {\n" +
            "  let notARealField = \"oops\";\n" +
            "}";

        var exception = Assert.Throws<InvalidOperationException>(() => Document.Parse(source).Value.Evaluate());

        Assert.Contains("not declared by any extension schema base", exception.Message);
    }

    [Fact]
    public void ReportsDuplicateFieldsAcrossMultipleSchemaBases()
    {
        const string source =
            "class OverlayA {\n" +
            "  string common = \"a\";\n" +
            "};\n" +
            "class OverlayB {\n" +
            "  string common = \"b\";\n" +
            "};\n" +
            "def Example;\n" +
            "extends Example : OverlayA, OverlayB {\n" +
            "  let common = \"value\";\n" +
            "}";

        var exception = Assert.Throws<InvalidOperationException>(() => Document.Parse(source).Value.Evaluate());

        Assert.Contains("defined by more than one base class", exception.Message);
    }

    [Fact]
    public void AppliesClassExtensionToAllRecordsDerivedFromTargetClass()
    {
        const string source =
            "class Schema {\n" +
            "  string csharpType = ?;\n" +
            "};\n" +
            "class MyParam<string desc> {\n" +
            "  string description = desc;\n" +
            "};\n" +
            "def ParamA : MyParam<\"first\">;\n" +
            "def ParamB : MyParam<\"second\">;\n" +
            "def Unrelated;\n" +
            "extends MyParam : Schema {\n" +
            "  let csharpType = \"string\";\n" +
            "}";

        var document = Document.Parse(source).Value.Evaluate();
        var paramA = document.Records.Single(static r => r.Name == "ParamA");
        var paramB = document.Records.Single(static r => r.Name == "ParamB");
        var unrelated = document.Records.Single(static r => r.Name == "Unrelated");

        Assert.Equal("string", Assert.IsType<StringValue>(paramA.GetField("csharpType")).Value);
        Assert.Equal("string", Assert.IsType<StringValue>(paramB.GetField("csharpType")).Value);
        Assert.False(unrelated.Fields.ContainsKey("csharpType"));
    }

    [Fact]
    public void ClassExtensionFieldsAreVisibleThroughTransitiveBaseClass()
    {
        const string source =
            "class Schema {\n" +
            "  string tag = ?;\n" +
            "};\n" +
            "class Base;\n" +
            "class Derived : Base;\n" +
            "def Example : Derived;\n" +
            "extends Base : Schema {\n" +
            "  let tag = \"from-base\";\n" +
            "}";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("from-base", Assert.IsType<StringValue>(record.GetField("tag")).Value);
    }

    [Fact]
    public void RecordLocalFieldsShadowClassExtensionFields()
    {
        const string source =
            "class Schema {\n" +
            "  string csharpType = ?;\n" +
            "};\n" +
            "class MyParam;\n" +
            "def Explicit : MyParam {\n" +
            "  string csharpType = \"custom\";\n" +
            "};\n" +
            "def Inherited : MyParam;\n" +
            "extends MyParam : Schema {\n" +
            "  let csharpType = \"default\";\n" +
            "}";

        var document = Document.Parse(source).Value.Evaluate();
        var explicit_ = document.Records.Single(static r => r.Name == "Explicit");
        var inherited = document.Records.Single(static r => r.Name == "Inherited");

        // Record-local field wins over the class extension.
        Assert.Equal("custom", Assert.IsType<StringValue>(explicit_.GetField("csharpType")).Value);
        // Record with no local field sees the extension.
        Assert.Equal("default", Assert.IsType<StringValue>(inherited.GetField("csharpType")).Value);
    }

    [Fact]
    public void ClassExtensionFieldsAppearInFieldsEnumeration()
    {
        const string source =
            "class Schema {\n" +
            "  string meta = ?;\n" +
            "};\n" +
            "class MyParam;\n" +
            "def Example : MyParam;\n" +
            "extends MyParam : Schema {\n" +
            "  let meta = \"injected\";\n" +
            "}";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.True(record.Fields.ContainsKey("meta"));
        Assert.Equal("injected", Assert.IsType<StringValue>(record.Fields["meta"]).Value);
    }

    [Fact]
    public void AnonymousRecordValueSeesExtensionFieldsOnItsOwnClass()
    {
        // The critical case: the extended class IS the class being instantiated in the dag.
        // The EvaluatedClass for MyParam must be carried directly on the AnonymousRecordValue
        // (as OwnClass) so that its extensions are visible even though no top-level def uses
        // MyParam as a base class.
        const string source =
            "class Schema {\n" +
            "  string csharpType = ?;\n" +
            "};\n" +
            "class MyParam<string desc>;\n" +
            "class Holder {\n" +
            "  dag parameters = (ins MyParam<\"d\">:$p);\n" +
            "};\n" +
            "def Example : Holder;\n" +
            "extends MyParam : Schema {\n" +
            "  let csharpType = \"string\";\n" +
            "}";

        var record = TestHelpers.EvaluateSingleRecord(source);
        var dag = Assert.IsType<DagValue>(record.GetField("parameters"));
        var param = Assert.IsType<AnonymousRecordValue>(dag.Arguments[0].Value);

        Assert.Equal("string", Assert.IsType<StringValue>(param.Fields["csharpType"]).Value);
    }

    [Fact]
    public void AnonymousRecordInstantiationInListCanApplyInlineBodyLets()
    {
        const string source =
            "class Member<string name> {\n" +
            "  string Name = name;\n" +
            "  string Summary = ?;\n" +
            "};\n" +
            "def Example {\n" +
            "  list<Member> Members = [\n" +
            "    Member<\"value\"> {\n" +
            "      let Summary = \"doc\";\n" +
            "    }\n" +
            "  ];\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);
        var members = Assert.IsType<ListValue>(record.GetField("Members"));
        var member = Assert.IsType<AnonymousRecordValue>(Assert.Single(members.Items));

        Assert.Equal("value", Assert.IsType<StringValue>(member.Fields["Name"]).Value);
        Assert.Equal("doc", Assert.IsType<StringValue>(member.Fields["Summary"]).Value);
    }

    [Fact]
    public void NamedAndAnonymousRecordsShareRecordLikeHelpers()
    {
        const string source =
            "class Base {\n" +
            "  string Marker = \"x\";\n" +
            "};\n" +
            "class Holder {\n" +
            "  dag Params = (ins Base<>:$p);\n" +
            "};\n" +
            "def Named : Base;\n" +
            "def Example : Holder;\n";

        var document = Document.Parse(source).Value.Evaluate();
        RecordLikeValue named = Assert.Single(document.Records, static record => record.Name == "Named");
        var example = Assert.Single(document.Records, static record => record.Name == "Example");
        var parameters = Assert.IsType<DagValue>(example.GetField("Params"));
        RecordLikeValue anonymous = Assert.IsType<AnonymousRecordValue>(parameters.Arguments[0].Value);

        Assert.True(named.HasBaseClass("Base"));
        Assert.True(anonymous.HasBaseClass("Base"));
        Assert.Equal("x", Assert.IsType<StringValue>(named.GetField("Marker")).Value);
        Assert.Equal("x", Assert.IsType<StringValue>(anonymous.GetField("Marker")).Value);
        Assert.Equal("Named", named.DisplayName);
        Assert.Equal("Base", anonymous.DisplayName);
    }

    [Fact]
    public void EvaluatesListsAndNestedTemplateInstantiation()
    {
        const string source =
            "class Numbers<list<int> values> {\n" +
            "  list<int> Values = values;\n" +
            "};\n" +
            "class Wrapper<list<int> inner> : Numbers<inner> {\n" +
            "  string Tag = \"wrapped\";\n" +
            "};\n" +
            "def Example : Wrapper<[1, 2, 3]>;";

        var record = TestHelpers.EvaluateSingleRecord(source);
        var values = Assert.IsType<ListValue>(record.GetField("Values"));

        Assert.Equal(new[] { 1, 2, 3 }, values.Items.Cast<IntegerValue>().Select(static item => item.Value).ToArray());
        Assert.Equal("wrapped", Assert.IsType<StringValue>(record.GetField("Tag")).Value);
        Assert.Equal(["Wrapper", "Numbers"], record.BaseClassNames);
    }

    [Fact]
    public void EvaluatesEmptyListLiterals()
    {
        const string source =
            "def Example {\n" +
            "  list<int> Values = [];\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Empty(Assert.IsType<ListValue>(record.GetField("Values")).Items);
    }

    [Fact]
    public void EvaluatesTrailingCommaListsAndNegativeIndices()
    {
        const string source =
            "def Example {\n" +
            "  list<int> Values = [1, 2, 3,];\n" +
            "  int Last = Values[-1];\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(3, Assert.IsType<IntegerValue>(record.GetField("Last")).Value);
    }

    [Fact]
    public void EvaluatesBitInitializersAndOverridesFromIntegersAndBooleans()
    {
        const string source =
            "def Example {\n" +
            "  bit First = 1;\n" +
            "  bit Second = true;\n" +
            "  bit Third = false;\n" +
            "  let First = 0;\n" +
            "  let Second = false;\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.False(Assert.IsType<BitValue>(record.GetField("First")).Value);
        Assert.False(Assert.IsType<BitValue>(record.GetField("Second")).Value);
        Assert.False(Assert.IsType<BitValue>(record.GetField("Third")).Value);
    }

    [Fact]
    public void LetsPreserveNonBitFieldTypes()
    {
        const string source =
            "def Example {\n" +
            "  int Width = 4;\n" +
            "  string Name = \"old\";\n" +
            "  let Width = 9;\n" +
            "  let Name = \"new\";\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(9, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
        Assert.Equal("new", Assert.IsType<StringValue>(record.GetField("Name")).Value);
    }

    [Fact]
    public void EvaluatesThroughThePublicDocumentApi()
    {
        const string source =
            "def First { int Width = 1; };\n" +
            "def Second { int Width = 2; };";

        var document = Document.Parse(source).Value.Evaluate();

        Assert.Equal(2, document.Records.Count);
        Assert.Equal(1, Assert.IsType<IntegerValue>(document.Records[0].GetField("Width")).Value);
        Assert.Equal(2, Assert.IsType<IntegerValue>(document.Records[1].GetField("Width")).Value);
    }

    [Fact]
    public void EvaluatesCodeBlockStrings()
    {
        const string source =
            "def Example {\n" +
            "  string Description = [{Hello from a code block.}];\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("Hello from a code block.", Assert.IsType<StringValue>(record.GetField("Description")).Value);
    }

    [Fact]
    public void EvaluatesDagExpressionsAndRecordReferences()
    {
        const string source =
            "include \"mlir/IR/OpBase.td\"\n" +
            "\n" +
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> : Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "};\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "};";

        var record = TestHelpers.LoadWithPrelude(source).Evaluate().Records.Single(static record => record.Name == "MiniArith_AddIOp");

        Assert.Contains("MiniArith_Op", record.BaseClassNames);
        Assert.Contains("Op", record.BaseClassNames);
        Assert.Equal("addi", Assert.IsType<StringValue>(record.GetField("opName")).Value);
        Assert.Equal("MiniArith_Dialect", Assert.IsType<RecordReferenceValue>(record.GetField("opDialect")).RecordName);
        var traits = Assert.IsType<ListValue>(record.GetField("traits"));
        Assert.Equal(
            ["Pure", "Commutative"],
            traits.Items.Select(static trait => trait switch
            {
                SymbolReferenceValue symbol => symbol.SymbolName,
                RecordReferenceValue recordReference => recordReference.RecordName,
                _ => throw new InvalidCastException(),
            }).ToArray());
        var arguments = Assert.IsType<DagValue>(record.GetField("arguments"));
        Assert.Equal("ins", arguments.OperatorName);
        Assert.Equal("lhs", arguments.Arguments[0].Name);
        Assert.Equal("rhs", arguments.Arguments[1].Name);
    }

    [Fact]
    public void EvaluatesDagExpressionsWithKeywordShapedArgumentNames()
    {
        const string source =
            "include \"mlir/IR/OpBase.td\"\n" +
            "\n" +
            "def Example {\n" +
            "  dag arguments = (ins I32:$in);\n" +
            "  dag results = (outs I32:$out);\n" +
            "};";

        var record = TestHelpers.LoadWithPrelude(source).Evaluate().Records.Single(static record => record.Name == "Example");

        var arguments = Assert.IsType<DagValue>(record.GetField("arguments"));
        var results = Assert.IsType<DagValue>(record.GetField("results"));

        Assert.Equal("in", arguments.Arguments[0].Name);
        Assert.Equal("out", results.Arguments[0].Name);
    }

    [Fact]
    public void EvaluatesTypedBangOperatorsUsedByUpstreamEnumAttr()
    {
        const string source =
            "class Base<int value> {\n" +
            "  int Value = value;\n" +
            "};\n" +
            "class Derived<int value> : Base<value>;\n" +
            "def Example {\n" +
            "  Derived<3> derived = Derived<3>;\n" +
            "  bit IsBase = !isa<Base>(derived);\n" +
            "  int Shifted = !shl(1, 3);\n" +
            "  list<int> Filtered = !filter(iter, [1, 2, 3], !gt(iter, 1));\n" +
            "  int CastValue = !cast<Base>(derived).Value;\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.True(Assert.IsType<BitValue>(record.GetField("IsBase")).Value);
        Assert.Equal(8, Assert.IsType<IntegerValue>(record.GetField("Shifted")).Value);
        Assert.Equal([2, 3], Assert.IsType<ListValue>(record.GetField("Filtered")).Items.Cast<IntegerValue>().Select(static item => item.Value).ToArray());
        Assert.Equal(3, Assert.IsType<IntegerValue>(record.GetField("CastValue")).Value);
    }

    [Fact]
    public void DerivedLetsAffectBaseComputedFieldsBeforeBodyResolution()
    {
        const string source =
            "class Base<string fallback> {\n" +
            "  string mnemonic = fallback;\n" +
            "  string attrName = mnemonic # \".suffix\";\n" +
            "};\n" +
            "class Derived<string name> : Base<\"default\"> {\n" +
            "  let mnemonic = name;\n" +
            "};\n" +
            "def Example : Derived<\"real\">;";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("real", Assert.IsType<StringValue>(record.GetField("mnemonic")).Value);
        Assert.Equal("real.suffix", Assert.IsType<StringValue>(record.GetField("attrName")).Value);
    }

    [Fact]
    public void LocalLetsAffectInheritedComputedFieldsBeforeInterFieldResolution()
    {
        const string source =
            "class C<int x> {\n" +
            "  int Y = x;\n" +
            "  int Yplus1 = !add(Y, 1);\n" +
            "  int xplus1 = !add(x, 1);\n" +
            "}\n" +
            "\n" +
            "def Example : C<5> {\n" +
            "  let Y = 10;\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(10, Assert.IsType<IntegerValue>(record.GetField("Y")).Value);
        Assert.Equal(11, Assert.IsType<IntegerValue>(record.GetField("Yplus1")).Value);
        Assert.Equal(6, Assert.IsType<IntegerValue>(record.GetField("xplus1")).Value);
    }

    [Fact]
    public void TopLevelLetsAffectInheritedComputedFieldsBeforeInterFieldResolution()
    {
        const string source =
            "class C<int x> {\n" +
            "  int Y = x;\n" +
            "  int Yplus1 = !add(Y, 1);\n" +
            "  int xplus1 = !add(x, 1);\n" +
            "}\n" +
            "\n" +
            "let Y = 10 in {\n" +
            "  def Example : C<5>;\n" +
            "}\n";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(10, Assert.IsType<IntegerValue>(record.GetField("Y")).Value);
        Assert.Equal(11, Assert.IsType<IntegerValue>(record.GetField("Yplus1")).Value);
        Assert.Equal(6, Assert.IsType<IntegerValue>(record.GetField("xplus1")).Value);
    }

    [Fact]
    public void TopLevelLetsCanWrapASingleDefinitionWithoutBraces()
    {
        const string source =
            "class Base<int width> { int Width = width; };\n" +
            "let Width = 9 in def Example : Base<4>;";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(9, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
    }

    [Fact]
    public void NestedTopLevelLetsUseTheInnermostBinding()
    {
        const string source =
            "class Base<int width> {\n" +
            "  int Width = width;\n" +
            "  int WidthPlusOne = !add(Width, 1);\n" +
            "};\n" +
            "let Width = 7 in let Width = 9 in def Example : Base<4>;";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(9, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
        Assert.Equal(10, Assert.IsType<IntegerValue>(record.GetField("WidthPlusOne")).Value);
    }

    [Fact]
    public void TopLevelLetsDoNotAffectLocalOnlyFields()
    {
        const string source =
            "let Width = 9 in def Example {\n" +
            "  int Width = 4;\n" +
            "  int WidthPlusOne = !add(Width, 1);\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(4, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
        Assert.Equal(5, Assert.IsType<IntegerValue>(record.GetField("WidthPlusOne")).Value);
    }

    [Fact]
    public void ClassesDeclaredInsideTopLevelLetsRetainThoseBindings()
    {
        const string source =
            "class Root<int width> {\n" +
            "  int Width = width;\n" +
            "};\n" +
            "let Width = 11 in {\n" +
            "  class Wrapped<int width> : Root<width>;\n" +
            "}\n" +
            "def Example : Wrapped<4>;";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(11, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
    }

    [Fact]
    public void CollectsBaseClassesLeftToRightAndAncestorsTopToBottom()
    {
        const string source =
            "class Root;\n" +
            "class LeftLeaf : Root;\n" +
            "class RightBranch;\n" +
            "class RightLeaf : RightBranch, Root;\n" +
            "def Example : LeftLeaf, RightLeaf;";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(["LeftLeaf", "Root", "RightLeaf", "RightBranch"], record.BaseClassNames);
    }

    [Fact]
    public void ReportsMissingTemplateArgumentsWhenNoDefaultExists()
    {
        const string source =
            "class Base<int width> { int Width = width; };\n" +
            "def Example : Base<>;";

        var document = Document.Parse(source).Value;
        var exception = Assert.Throws<InvalidOperationException>(() => document.Evaluate());

        Assert.Contains("Missing value for template parameter 'width'", exception.Message);
    }

    [Fact]
    public void ReportsUnknownBaseClasses()
    {
        var exception = Assert.Throws<KeyNotFoundException>(() => Document.Parse("def Example : MissingBase;").Value.Evaluate());

        Assert.Contains("Unknown TableGen class 'MissingBase'.", exception.Message);
    }

    [Fact]
    public void ReportsTypeMismatchesForUnknownIdentifiersInTypedFields()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Document.Parse("def Example { int Width = missing; };").Value.Evaluate());

        Assert.Contains("Expected an integer value", exception.Message);
    }

    [Fact]
    public void EvaluatesConcatenation()
    {
        const string source = "def Example { string Value = \"hello\" # \", \" # \"world\"; };";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("hello, world", Assert.IsType<StringValue>(record.GetField("Value")).Value);
    }

    [Fact]
    public void EvaluatesAdjacentStringLiteralConcatenation()
    {
        const string source = "def Example { string Value = \"hello\" \", \" \"world\"; };";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("hello, world", Assert.IsType<StringValue>(record.GetField("Value")).Value);
    }

    [Fact]
    public void EvaluatesListConcatenationWithHash()
    {
        // TableGen's # operator concatenates two lists when both operands are list values.
        // This is the mechanism used by ops like ModuleOp: [A, B] # SomeTraitList.traits
        const string source =
            "def Example {\n" +
            "  list<int> A = [1, 2];\n" +
            "  list<int> B = [3, 4];\n" +
            "  list<int> C = A # B;\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);
        var c = Assert.IsType<ListValue>(record.GetField("C"));

        Assert.Equal([1, 2, 3, 4], c.Items.Cast<IntegerValue>().Select(static item => item.Value).ToArray());
    }

    [Fact]
    public void EvaluatesListConcatenationViaFieldAccessWithHash()
    {
        // Accessing a record's field and concatenating the resulting list via # must work.
        // This mirrors `[MyTrait] # TraitListRecord.traits` used in ODS op definitions.
        const string source =
            "class Holder<list<int> inner> { list<int> items = inner; };\n" +
            "def H : Holder<[3, 4]>;\n" +
            "def Example {\n" +
            "  list<int> Result = [1, 2] # H.items;\n" +
            "};";

        var evaluated = Document.Parse(source).Value.Evaluate();
        var record = evaluated.Records.Single(static r => r.Name == "Example");
        var result = Assert.IsType<ListValue>(record.GetField("Result"));

        Assert.Equal([1, 2, 3, 4], result.Items.Cast<IntegerValue>().Select(static item => item.Value).ToArray());
    }

    [Fact]
    public void EvaluatesBangArithmetic()
    {
        const string source =
            "def Example {\n" +
            "  int Sum = !add(3, 4);\n" +
            "  int Diff = !sub(10, 3);\n" +
            "  int Product = !mul(6, 7);\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(7, Assert.IsType<IntegerValue>(record.GetField("Sum")).Value);
        Assert.Equal(7, Assert.IsType<IntegerValue>(record.GetField("Diff")).Value);
        Assert.Equal(42, Assert.IsType<IntegerValue>(record.GetField("Product")).Value);
    }

    [Fact]
    public void EvaluatesBangComparisons()
    {
        const string source =
            "def Example {\n" +
            "  int GT = !gt(5, 3);\n" +
            "  int GE = !ge(3, 3);\n" +
            "  int LT = !lt(2, 5);\n" +
            "  int LE = !le(3, 2);\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(1, Assert.IsType<IntegerValue>(record.GetField("GT")).Value);
        Assert.Equal(1, Assert.IsType<IntegerValue>(record.GetField("GE")).Value);
        Assert.Equal(1, Assert.IsType<IntegerValue>(record.GetField("LT")).Value);
        Assert.Equal(0, Assert.IsType<IntegerValue>(record.GetField("LE")).Value);
    }

    [Fact]
    public void EvaluatesBangIf()
    {
        const string source =
            "def Example {\n" +
            "  string A = !if(!gt(5, 3), \"yes\", \"no\");\n" +
            "  string B = !if(!gt(1, 9), \"yes\", \"no\");\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("yes", Assert.IsType<StringValue>(record.GetField("A")).Value);
        Assert.Equal("no", Assert.IsType<StringValue>(record.GetField("B")).Value);
    }

    [Fact]
    public void EvaluatesBangCondLocalDefvarsAndAsserts()
    {
        const string source =
            "def Example {\n" +
            "  defvar x = 1;\n" +
            "  assert !gt(x, 0), \"x must be positive\";\n" +
            "  string Value = !cond(!eq(x, 0): \"zero\", true: \"positive\",);\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("positive", Assert.IsType<StringValue>(record.GetField("Value")).Value);
    }

    [Fact]
    public void EvaluatesBangSubstHeadAndTail()
    {
        const string source =
            "def Example {\n" +
            "  string Replaced = !subst(\"x\", \"y\", \"xoxo\");\n" +
            "  int First = !head([4, 5, 6]);\n" +
            "  list<int> Rest = !tail([4, 5, 6]);\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("yoyo", Assert.IsType<StringValue>(record.GetField("Replaced")).Value);
        Assert.Equal(4, Assert.IsType<IntegerValue>(record.GetField("First")).Value);
        Assert.Equal([5, 6], Assert.IsType<ListValue>(record.GetField("Rest")).Items.Cast<IntegerValue>().Select(static item => item.Value).ToArray());
    }

    [Fact]
    public void ReportsFailedBodyAssertions()
    {
        const string source =
            "def Example {\n" +
            "  assert 0, \"boom\";\n" +
            "};";

        var exception = Assert.Throws<InvalidOperationException>(() => Document.Parse(source).Value.Evaluate());

        Assert.Contains("boom", exception.Message);
    }

    [Fact]
    public void EvaluatesBangIfLazily()
    {
        // !find returns -1 when the substring is absent; the then-branch is only evaluated
        // when the condition is true, so when idx == -1 the !substr in the then-branch
        // must not be reached.
        const string source =
            "def Example {\n" +
            "  int idx = !find(\"abc\", \"_\");\n" +
            "  string V = !if(!ge(idx, 0), !substr(\"abc\", idx), \"safe\");\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(-1, Assert.IsType<IntegerValue>(record.GetField("idx")).Value);
        Assert.Equal("safe", Assert.IsType<StringValue>(record.GetField("V")).Value);
    }

    [Fact]
    public void EvaluatesBangStringOps()
    {
        const string source =
            "def Example {\n" +
            "  int Sz = !size(\"hello\");\n" +
            "  string Up = !toupper(\"hi\");\n" +
            "  string Lo = !tolower(\"WORLD\");\n" +
            "  string Sub2 = !substr(\"hello\", 1);\n" +
            "  string Sub3 = !substr(\"hello\", 1, 3);\n" +
            "  int Idx = !find(\"hello\", \"ll\");\n" +
            "  int IdxMiss = !find(\"hello\", \"xyz\");\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(5, Assert.IsType<IntegerValue>(record.GetField("Sz")).Value);
        Assert.Equal("HI", Assert.IsType<StringValue>(record.GetField("Up")).Value);
        Assert.Equal("world", Assert.IsType<StringValue>(record.GetField("Lo")).Value);
        Assert.Equal("ello", Assert.IsType<StringValue>(record.GetField("Sub2")).Value);
        Assert.Equal("ell", Assert.IsType<StringValue>(record.GetField("Sub3")).Value);
        Assert.Equal(2, Assert.IsType<IntegerValue>(record.GetField("Idx")).Value);
        Assert.Equal(-1, Assert.IsType<IntegerValue>(record.GetField("IdxMiss")).Value);
    }

    [Fact]
    public void EvaluatesBangRange()
    {
        const string source = "def Example { list<int> R = !range(2, 5); };";

        var record = TestHelpers.EvaluateSingleRecord(source);

        var list = Assert.IsType<ListValue>(record.GetField("R"));
        Assert.Equal([2, 3, 4], list.Items.Cast<IntegerValue>().Select(static v => v.Value).ToArray());
    }

    [Fact]
    public void EvaluatesBangFoldlStringConcat()
    {
        const string source =
            "def Example {\n" +
            "  string V = !foldl(\"\", [\"a\", \"b\", \"c\"], acc, cur, acc # cur);\n" +
            "};";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("abc", Assert.IsType<StringValue>(record.GetField("V")).Value);
    }

    [Fact]
    public void EvaluatesClassInstantiation()
    {
        const string source =
            "class StrDouble<string s> { string result = s # s; };\n" +
            "def Example { string V = StrDouble<\"hi\">.result; };";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("hihi", Assert.IsType<StringValue>(record.GetField("V")).Value);
    }

    [Fact]
    public void EvaluatesDeprecatedClass()
    {
        const string source =
            "class Deprecated<string reason> { string odsDeprecated = reason; };\n" +
            "def OldThing : Deprecated<\"use NewThing instead\">;";

        var record = TestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("use NewThing instead", Assert.IsType<StringValue>(record.GetField("odsDeprecated")).Value);
    }

    [Fact]
    public void EvaluatesFirstCharToUpper()
    {
        const string utilsSrc =
            "class firstCharToUpper<string str>\n" +
            "{\n" +
            "  string ret = !if(!gt(!size(str), 0),\n" +
            "    !toupper(!substr(str, 0, 1)) # !substr(str, 1),\n" +
            "    \"\");\n" +
            "};\n";

        var source =
            utilsSrc +
            "def TestNormal : firstCharToUpper<\"hello\">;\n" +
            "def TestEmpty  : firstCharToUpper<\"\">;\n" +
            "def TestUpper  : firstCharToUpper<\"World\">;";

        var doc = Document.Parse(source).Value.Evaluate();
        var normal = doc.Records.Single(static r => r.Name == "TestNormal");
        var empty = doc.Records.Single(static r => r.Name == "TestEmpty");
        var upper = doc.Records.Single(static r => r.Name == "TestUpper");

        Assert.Equal("Hello", Assert.IsType<StringValue>(normal.GetField("ret")).Value);
        Assert.Equal("", Assert.IsType<StringValue>(empty.GetField("ret")).Value);
        Assert.Equal("World", Assert.IsType<StringValue>(upper.GetField("ret")).Value);
    }

    [Fact]
    public void EvaluatesSnakeCaseToCamelCase()
    {
        // Full implementation of snakeCaseToCamelCase from MLIR's Utils.td
        const string utilsSrc =
            "class firstCharToUpper<string str>\n" +
            "{\n" +
            "  string ret = !if(!gt(!size(str), 0),\n" +
            "    !toupper(!substr(str, 0, 1)) # !substr(str, 1),\n" +
            "    \"\");\n" +
            "};\n" +
            "\n" +
            "class _snakeCaseHelper<string str> {\n" +
            "  int idx = !find(str, \"_\");\n" +
            "  string ret = !if(!ge(idx, 0),\n" +
            "    !substr(str, 0, idx) # firstCharToUpper<!substr(str, !add(idx, 1))>.ret,\n" +
            "    str);\n" +
            "};\n" +
            "\n" +
            "class snakeCaseToCamelCase<string str>\n" +
            "{\n" +
            "  string ret = !foldl(firstCharToUpper<str>.ret,\n" +
            "    !range(0, !size(str)), acc, idx, _snakeCaseHelper<acc>.ret);\n" +
            "};\n";

        var source =
            utilsSrc +
            "def TestSimple  : snakeCaseToCamelCase<\"foo\">;\n" +
            "def TestOnce    : snakeCaseToCamelCase<\"snake_case\">;\n" +
            "def TestTwice   : snakeCaseToCamelCase<\"snake_case_example\">;";

        var doc = Document.Parse(source).Value.Evaluate();
        var simple = doc.Records.Single(static r => r.Name == "TestSimple");
        var once = doc.Records.Single(static r => r.Name == "TestOnce");
        var twice = doc.Records.Single(static r => r.Name == "TestTwice");

        Assert.Equal("Foo", Assert.IsType<StringValue>(simple.GetField("ret")).Value);
        Assert.Equal("SnakeCase", Assert.IsType<StringValue>(once.GetField("ret")).Value);
        Assert.Equal("SnakeCaseExample", Assert.IsType<StringValue>(twice.GetField("ret")).Value);
    }

    [Fact]
    public void EvaluatesFullUtilsTd()
    {
        const string source = @"
#ifndef UTILS_TD
#define UTILS_TD

class Deprecated<string reason> {
  string odsDeprecated = reason;
}

class CppDeprecated<string reason> {
  string odsCppDeprecated = reason;
}

class StrFunc<string r> {
  string result = r;
}

def ins;
def outs;

class CArg<string ty, string value = """"> {
  string type = ty;
  string defaultValue = value;
}

class firstCharToUpper<string str>
{
  string ret = !if(!gt(!size(str), 0),
    !toupper(!substr(str, 0, 1)) # !substr(str, 1),
    """");
}

class _snakeCaseHelper<string str> {
  int idx = !find(str, ""_"");
  string ret = !if(!ge(idx, 0),
    !substr(str, 0, idx) # firstCharToUpper<!substr(str, !add(idx, 1))>.ret,
    str);
}

class snakeCaseToCamelCase<string str>
{
  string ret = !foldl(firstCharToUpper<str>.ret,
    !range(0, !size(str)), acc, idx, _snakeCaseHelper<acc>.ret);
}

#endif // UTILS_TD

def TestDep     : Deprecated<""use something else"">;
def TestCppDep  : CppDeprecated<""use CppThing"">;
def TestArg     : CArg<""int"", ""42"">;
def TestArgDef  : CArg<""void *"">;
def TestCamel   : snakeCaseToCamelCase<""get_op_name"">;
";

        var doc = Document.Parse(source).Value.Evaluate();

        var dep = doc.Records.Single(static r => r.Name == "TestDep");
        Assert.Equal("use something else", Assert.IsType<StringValue>(dep.GetField("odsDeprecated")).Value);

        var cppDep = doc.Records.Single(static r => r.Name == "TestCppDep");
        Assert.Equal("use CppThing", Assert.IsType<StringValue>(cppDep.GetField("odsCppDeprecated")).Value);

        var arg = doc.Records.Single(static r => r.Name == "TestArg");
        Assert.Equal("int", Assert.IsType<StringValue>(arg.GetField("type")).Value);
        Assert.Equal("42", Assert.IsType<StringValue>(arg.GetField("defaultValue")).Value);

        var argDef = doc.Records.Single(static r => r.Name == "TestArgDef");
        Assert.Equal("void *", Assert.IsType<StringValue>(argDef.GetField("type")).Value);
        Assert.Equal("", Assert.IsType<StringValue>(argDef.GetField("defaultValue")).Value);

        var camel = doc.Records.Single(static r => r.Name == "TestCamel");
        Assert.Equal("GetOpName", Assert.IsType<StringValue>(camel.GetField("ret")).Value);
    }
}
