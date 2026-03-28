namespace TableGen.Tests;

using TableGen.Evaluation;
using TableGen.Text;
using Xunit;

public sealed class TableGenLexingTests
{
    [Fact]
    public void IgnoresLineAndBlockComments()
    {
        const string source =
            "// comment\n" +
            "class Base<int width> { /* body */ int Width = width; }; \n" +
            "def Example : Base<4>;";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);

        Assert.Equal(4, Assert.IsType<IntegerValue>(record.GetField("Width")).Value);
    }

    [Fact]
    public void ParsesStringLiteralEscapes()
    {
        const string source =
            "def Example {\n" +
            "  string Value = \"slash\\\\quote\\\"line\\ncarriage\\r tab\\tend\";\n" +
            "};";

        var record = TableGenTestHelpers.EvaluateSingleRecord(source);

        Assert.Equal("slash\\quote\"line\ncarriage\r tab\tend", Assert.IsType<StringValue>(record.GetField("Value")).Value);
    }

    [Fact]
    public void ReportsUnterminatedStrings()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("def Bad { string Name = \"oops; };"));

        Assert.Contains("Unterminated string literal", exception.Message);
    }

    [Fact]
    public void ReportsUnterminatedBlockComments()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("/* unterminated"));

        Assert.Contains("Unterminated block comment.", exception.Message);
    }

    [Fact]
    public void ReportsUnexpectedCharacters()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("@"));

        Assert.Contains("Unexpected character '@'.", exception.Message);
    }

    [Fact]
    public void ReportsDanglingSlashThatIsNotAComment()
    {
        var exception = Assert.Throws<TableGenParseException>(() => TableGenDocument.Parse("/"));

        Assert.Contains("Unexpected character '/'.", exception.Message);
    }
}
