namespace TableGen.Tests;

using System.Linq;
using TableGen.Evaluation;
using TableGen.Syntax;
using TableGen.Text;
using Xunit;

public sealed class TableGenParserTests
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

        var document = TableGenDocument.Parse(source);

        Assert.Equal(3, document.Syntax.Declarations.Count);
        var @class = Assert.IsType<TableGenClassSyntax>(document.Syntax.Declarations[0]);
        Assert.Equal("Base", @class.Name);
        Assert.Equal(2, @class.TemplateParameters.Count);
        Assert.Equal("width", @class.TemplateParameters[0].Name);
        Assert.Equal("name", @class.TemplateParameters[1].Name);

        var def = Assert.IsType<TableGenDefSyntax>(document.Syntax.Declarations[2]);
        Assert.Equal("Example", def.Name);
        Assert.Single(def.Bases);
        Assert.Equal("Derived", def.Bases[0].Name);
    }

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

        var record = TableGenDocument.Parse(source).Evaluate().Records.Single();

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

        var record = TableGenDocument.Parse(source).Evaluate().Records.Single();
        var values = Assert.IsType<ListValue>(record.GetField("Values"));

        Assert.Equal(new[] { 1, 2, 3 }, values.Items.Cast<IntegerValue>().Select(static item => item.Value).ToArray());
        Assert.Equal("wrapped", Assert.IsType<StringValue>(record.GetField("Tag")).Value);
    }

    [Fact]
    public void IgnoresLineAndBlockComments()
    {
        const string source =
            "// comment\n" +
            "class Base<int width> { /* body */ int Width = width; }; \n" +
            "def Example : Base<4>;";

        var record = TableGenDocument.Parse(source).Evaluate().Records.Single();

        Assert.Equal(4, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
    }

    [Fact]
    public void ReportsUnterminatedStrings()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("def Bad { string Name = \"oops; };"));

        Assert.Contains("Unterminated string literal", exception.Message);
    }

    [Fact]
    public void ReportsMissingTemplateArgumentsWhenNoDefaultExists()
    {
        const string source =
            "class Base<int width> { int Width = width; };\n" +
            "def Example : Base<>;";

        var document = TableGenDocument.Parse(source);
        var exception = Assert.Throws<System.InvalidOperationException>(() => document.Evaluate());

        Assert.Contains("Missing value for template parameter 'width'", exception.Message);
    }
}
