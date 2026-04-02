namespace TableGen.Tests;

using TableGen.Syntax;
using TableGen.Text;
using Xunit;

public sealed class ParsingTests
{
    [Fact]
    public void ParsesClassesAndDefsWithInheritance()
    {
        const string source =
            "class Base<int width, string name = \"anon\"> {\n" +
            "  int Width = width;\n" +
            "  string Name = name;\n" +
            "};\n" +
            "class Derived<string suffix> : Base<8, suffix> {\n" +
            "  bit Enabled = 1;\n" +
            "};\n" +
            "def Example : Derived<\"foo\">;";

        var document = Document.Parse(source);

        Assert.Equal(3, document.Syntax.Declarations.Count);
        var @class = Assert.IsType<ClassSyntax>(document.Syntax.Declarations[0]);
        Assert.Equal("Base", @class.Name);
        Assert.Equal(2, @class.TemplateParameters.Count);
        Assert.Equal("width", @class.TemplateParameters[0].Name);
        Assert.Equal("name", @class.TemplateParameters[1].Name);

        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[2]);
        Assert.Equal("Example", def.Name);
        Assert.Single(def.Bases);
        Assert.Equal("Derived", def.Bases[0].Name);
    }

    [Fact]
    public void ParsesNestedGenericTypeNames()
    {
        const string source =
            "class Holder<list<list<int>> values> {\n" +
            "  list<list<int>> Values = values;\n" +
            "};\n" +
            "def Example : Holder<[[1], [2, 3]]>;";

        var document = Document.Parse(source);
        var @class = Assert.IsType<ClassSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(@class.BodyItems[0]);

        Assert.Equal("list<list<int>>", @class.TemplateParameters[0].TypeName);
        Assert.Equal("list<list<int>>", field.TypeName);
    }

    [Fact]
    public void ParsesEmptyTemplateArgumentLists()
    {
        const string source =
            "class Base<> {\n" +
            "  int Width = 4;\n" +
            "};\n" +
            "def Example : Base<>;";

        var document = Document.Parse(source);
        var @class = Assert.IsType<ClassSyntax>(document.Syntax.Declarations[0]);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[1]);

        Assert.Empty(@class.TemplateParameters);
        Assert.Single(def.Bases);
        Assert.Empty(def.Bases[0].Arguments);
    }

    [Fact]
    public void ParsesCodeBlockStringLiterals()
    {
        const string source =
            "def Example {\n" +
            "  string Description = [{Line one.\n" +
            "Line two.\n" +
            "}];\n" +
            "};";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var value = Assert.IsType<StringSyntax>(field.Initializer);

        Assert.Contains("Line one.", value.Value);
        Assert.Contains("Line two.", value.Value);
    }

    [Fact]
    public void ParsesDagExpressions()
    {
        const string source =
            "def Example {\n" +
            "  dag Arguments = (ins I32:$lhs, I32:$rhs);\n" +
            "};";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var dag = Assert.IsType<DagSyntax>(field.Initializer);

        Assert.Equal("ins", dag.OperatorName);
        Assert.Equal(2, dag.Arguments.Count);
        Assert.Equal("lhs", dag.Arguments[0].Name);
        Assert.Equal("rhs", dag.Arguments[1].Name);
    }

    [Fact]
    public void ReportsUnexpectedTopLevelTokens()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("int Width = 1;"));

        Assert.Contains("Expected 'class', 'def', or 'defvar'.", exception.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ParsesConcatenationExpression()
    {
        const string source = "def Example { string Value = \"hello\" # \" \" # \"world\"; };";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var outer = Assert.IsType<ConcatSyntax>(field.Initializer);
        var inner = Assert.IsType<ConcatSyntax>(outer.Left);

        Assert.IsType<StringSyntax>(inner.Left);
        Assert.IsType<StringSyntax>(inner.Right);
        Assert.IsType<StringSyntax>(outer.Right);
    }

    [Fact]
    public void ParsesBangCallExpressions()
    {
        const string source =
            "def Example {\n" +
            "  int N = !size(\"hello\");\n" +
            "  string U = !toupper(\"hi\");\n" +
            "  int S = !add(2, 3);\n" +
            "};";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);

        var sizeField = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var size = Assert.IsType<BangCallSyntax>(sizeField.Initializer);
        Assert.Equal("size", size.OperatorName);
        Assert.Single(size.Arguments);

        var upperField = Assert.IsType<FieldSyntax>(def.BodyItems[1]);
        var upper = Assert.IsType<BangCallSyntax>(upperField.Initializer);
        Assert.Equal("toupper", upper.OperatorName);

        var addField = Assert.IsType<FieldSyntax>(def.BodyItems[2]);
        var add = Assert.IsType<BangCallSyntax>(addField.Initializer);
        Assert.Equal("add", add.OperatorName);
        Assert.Equal(2, add.Arguments.Count);
    }

    [Fact]
    public void ParsesBangIfExpression()
    {
        const string source = "def Example { string V = !if(!gt(1, 0), \"yes\", \"no\"); };";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var ifExpr = Assert.IsType<BangCallSyntax>(field.Initializer);

        Assert.Equal("if", ifExpr.OperatorName);
        Assert.Equal(3, ifExpr.Arguments.Count);
        var cond = Assert.IsType<BangCallSyntax>(ifExpr.Arguments[0]);
        Assert.Equal("gt", cond.OperatorName);
    }

    [Fact]
    public void ParsesFoldlExpression()
    {
        const string source = "def Example { string V = !foldl(\"\", [\"a\", \"b\"], acc, cur, acc # cur); };";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var foldl = Assert.IsType<FoldlSyntax>(field.Initializer);

        Assert.Equal("acc", foldl.AccVar);
        Assert.Equal("cur", foldl.CurVar);
        Assert.IsType<StringSyntax>(foldl.Init);
        Assert.IsType<ListSyntax>(foldl.List);
        Assert.IsType<ConcatSyntax>(foldl.Body);
    }

    [Fact]
    public void ParsesClassInstantiationExpression()
    {
        const string source =
            "class StrUpper<string s> { string result = !toupper(s); };\n" +
            "def Example { string V = StrUpper<\"hello\">.result; };";

        var document = Document.Parse(source);
        var def = Assert.IsType<DefSyntax>(document.Syntax.Declarations[1]);
        var field = Assert.IsType<FieldSyntax>(def.BodyItems[0]);
        var access = Assert.IsType<FieldAccessSyntax>(field.Initializer);
        var inst = Assert.IsType<AnonymousClassInstantiationSyntax>(access.Object);

        Assert.Equal("StrUpper", inst.ClassName);
        Assert.Single(inst.Arguments);
        Assert.Equal("result", access.FieldName);
    }

    [Fact]
    public void ReportsMissingTemplateParameterNames()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("class Base<int>;"));

        Assert.Contains("Expected a template parameter name.", exception.Message);
    }

    [Fact]
    public void ReportsMissingArgumentListTerminators()
    {
        var exception = Assert.Throws<ParseException>(() => Document.Parse("def Example : Base<1;"));

        Assert.Contains("Expected '>' to close the argument list.", exception.Message);
    }

    [Fact]
    public void ReportsMissingListTerminators()
    {
        const string source = "def Example { list<int> Values = [1, 2; };";

        var exception = Assert.Throws<ParseException>(() => Document.Parse(source));

        Assert.Contains("Expected ']' to close the list literal.", exception.Message);
    }

    [Fact]
    public void ReportsUnexpectedEndOfFileWhileParsingTypeArgumentLists()
    {
        const string source = "class Base { list<int Value; };";

        var exception = Assert.Throws<ParseException>(() => Document.Parse(source));

        Assert.Contains("Unexpected end of file while parsing a type argument list.", exception.Message);
    }
}
