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
        Assert.Equal(["Wrapper", "Numbers"], record.BaseClasses);
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

        var document = Document.Parse(source).Evaluate();

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
            "class MiniArith_Op<string mnemonic, list<Trait> traits = []> : Op<MiniArith_Dialect, mnemonic, traits>;\n" +
            "def MiniArith_Dialect : Dialect {\n" +
            "  let name = \"miniarith\";\n" +
            "};\n" +
            "def MiniArith_AddIOp : MiniArith_Op<\"addi\", [Pure, Commutative]> {\n" +
            "  let arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "};";

        var record = Document.Parse(source).Evaluate().Records.Single(static record => record.Name == "MiniArith_AddIOp");

        Assert.Contains("MiniArith_Op", record.BaseClasses);
        Assert.Contains("Op", record.BaseClasses);
        Assert.Equal("addi", Assert.IsType<StringValue>(record.GetField("mnemonic")).Value);
        Assert.Equal("MiniArith_Dialect", Assert.IsType<RecordReferenceValue>(record.GetField("dialect")).RecordName);
        var traits = Assert.IsType<ListValue>(record.GetField("traits"));
        Assert.Equal(["Pure", "Commutative"], traits.Items.Cast<SymbolReferenceValue>().Select(static trait => trait.SymbolName).ToArray());
        var arguments = Assert.IsType<DagValue>(record.GetField("arguments"));
        Assert.Equal("ins", arguments.OperatorName);
        Assert.Equal("lhs", arguments.Arguments[0].Name);
        Assert.Equal("rhs", arguments.Arguments[1].Name);
    }

    [Fact]
    public void ReportsMissingTemplateArgumentsWhenNoDefaultExists()
    {
        const string source =
            "class Base<int width> { int Width = width; };\n" +
            "def Example : Base<>;";

        var document = Document.Parse(source);
        var exception = Assert.Throws<InvalidOperationException>(() => document.Evaluate());

        Assert.Contains("Missing value for template parameter 'width'", exception.Message);
    }

    [Fact]
    public void ReportsUnknownBaseClasses()
    {
        var exception = Assert.Throws<KeyNotFoundException>(() => Document.Parse("def Example : MissingBase;").Evaluate());

        Assert.Contains("Unknown TableGen class 'MissingBase'.", exception.Message);
    }

    [Fact]
    public void ReportsTypeMismatchesForUnknownIdentifiersInTypedFields()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Document.Parse("def Example { int Width = missing; };").Evaluate());

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

        var doc = Document.Parse(source).Evaluate();
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

        var doc = Document.Parse(source).Evaluate();
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

        var doc = Document.Parse(source).Evaluate();

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
