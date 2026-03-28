namespace TableGen.Tests;

using TableGen.Syntax;
using TableGen.Text;
using Xunit;

public sealed class TableGenParsingTests
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
    public void ParsesNestedGenericTypeNames()
    {
        const string source =
            "class Holder<list<list<int>> values> {\n" +
            "  list<list<int>> Values = values;\n" +
            "};\n" +
            "def Example : Holder<[[1], [2, 3]]>;";

        var document = TableGenDocument.Parse(source);
        var @class = Assert.IsType<TableGenClassSyntax>(document.Syntax.Declarations[0]);
        var field = Assert.IsType<TableGenFieldSyntax>(@class.BodyItems[0]);

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

        var document = TableGenDocument.Parse(source);
        var @class = Assert.IsType<TableGenClassSyntax>(document.Syntax.Declarations[0]);
        var def = Assert.IsType<TableGenDefSyntax>(document.Syntax.Declarations[1]);

        Assert.Empty(@class.TemplateParameters);
        Assert.Single(def.Bases);
        Assert.Empty(def.Bases[0].Arguments);
    }

    [Fact]
    public void ReportsUnexpectedTopLevelTokens()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("int Width = 1;"));

        Assert.Contains("Expected 'class' or 'def'.", exception.Message);
        Assert.Equal(1, exception.Diagnostic.Line);
        Assert.Equal(1, exception.Diagnostic.Column);
    }

    [Fact]
    public void ReportsMissingTemplateParameterNames()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("class Base<int>;"));

        Assert.Contains("Expected a template parameter name.", exception.Message);
    }

    [Fact]
    public void ReportsMissingArgumentListTerminators()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("def Example : Base<1;"));

        Assert.Contains("Expected '>' to close the argument list.", exception.Message);
    }

    [Fact]
    public void ReportsMissingListTerminators()
    {
        const string source = "def Example { list<int> Values = [1, 2; };";

        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse(source));

        Assert.Contains("Expected ']' to close the list literal.", exception.Message);
    }

    [Fact]
    public void ReportsUnexpectedEndOfFileWhileParsingTypeArgumentLists()
    {
        const string source = "class Base { list<int Value; };";

        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse(source));

        Assert.Contains("Unexpected end of file while parsing a type argument list.", exception.Message);
    }
}
